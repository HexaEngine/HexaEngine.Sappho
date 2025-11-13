namespace HexaEngine.Sappho.Analyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    [Generator]
    public class SapphoGenerator : IIncrementalGenerator
    {
        private readonly HashSet<ulong> typeIds = [];

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            typeIds.Clear();
            var provider = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTarget, TransformTarget).Where(static m => m is not null);
            context.RegisterSourceOutput(provider, Execute);
        }

        private bool IsSyntaxTarget(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDecl && (typeDecl is ClassDeclarationSyntax or StructDeclarationSyntax);
        }

        private TypeToGenerate? TransformTarget(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);

            if (symbol is not INamedTypeSymbol typeSymbol)
                return null;

            bool hasSapphoAttribute = typeSymbol.GetAttributes().Any(attr => attr.IsAttribute<SapphoObjectAttribute>());

            if (!hasSapphoAttribute)
                return null;

            return new TypeToGenerate(typeSymbol);
        }

        private static IEnumerable<MemberInfo> GetMembers(ITypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                    continue;

                var memberFlags = MemberInfo.Classify(member);
                if (memberFlags.HasFlag(MemberFlags.Ignored))
                    continue;

                if (member is IFieldSymbol field)
                {
                    yield return new(field, memberFlags);
                }
                else if (member is IPropertySymbol property && property.GetMethod is not null && property.SetMethod is not null)
                {
                    yield return new(property, memberFlags);
                }
            }
        }

        private static IEnumerable<MemberInfo> GetAllMembers(ITypeSymbol symbol)
        {
            Stack<(ITypeSymbol, bool)> stack = [];
            stack.Push((symbol, false));
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                var (current, closing) = item;
                if (closing)
                {
                    foreach (var member in GetMembers(current))
                    {
                        yield return member;
                    }
                }
                else
                {
                    stack.Push((current, true));
                    if (current.BaseType != null && current.BaseType.SpecialType != SpecialType.System_Object && current.BaseType.SpecialType != SpecialType.System_ValueType)
                    {
                        stack.Push((current.BaseType, false));
                    }
                }
            }
        }

        private void Execute(SourceProductionContext context, TypeToGenerate? typeToGenerate)
        {
            if (!typeToGenerate.HasValue)
            {
                return;
            }
            var source = GenerateSource(typeToGenerate.Value);
            context.AddSource($"{typeToGenerate.Value.Name}_SapphoSerializable.g.cs", source);
        }

        private ulong GetTypeId(string name)
        {
            var id = MurmurHash3.Hash64(name.AsSpan());
            while (!typeIds.Add(id))
            {
                id++;
            }
            return id;
        }

        private string GenerateSource(TypeToGenerate type)
        {
            StringBuilder sb = new();
            using CodeWriter writer = new(sb, type.Namespace);

            using (writer.PushBlock($"partial {(type.IsClass ? "class" : "struct")} {type.Name} : global::HexaEngine.Sappho.ISapphoSerializable<{type.Name}>"))
            {
                writer.WriteLine($"public const ulong SapphoTypeId = {GetTypeId(type.Name)}UL;");
                writer.WriteLine($"public global::HexaEngine.Sappho.SapphoTypeId TypeId => SapphoTypeId;");
                writer.WriteLine();
                var members = GetAllMembers(type.Symbol).ToArray();
                WriteSerialize(writer, new TypeInfo(type.Symbol), false, members);

                writer.WriteLine();
                WriteDeserialize(writer, new TypeInfo(type.Symbol), false, members);

                HashSet<ITypeSymbol> subTypes = [];

                foreach (var member in members)
                {
                    if (member.Type.Kind == TypeKind.Struct)
                    {
                        var subMembers = GetAllMembers(member.TypeSymbol).ToArray();
                        writer.WriteLine();
                        WriteSerialize(writer, member.Type, true, subMembers);
                        writer.WriteLine();
                        WriteDeserialize(writer, member.Type, true, subMembers);
                    }
                }
            }
            writer.Dispose();

            return sb.ToString();
        }

        private static void WriteDeserialize(CodeWriter writer, TypeInfo symbol, bool makeExternal, MemberInfo[] members)
        {
            string fullName = symbol.FullName;
            using (writer.PushBlock(makeExternal ? $"public static {fullName} Deserialize{symbol.Name}(ref global::HexaEngine.Sappho.SapphoReader reader)" : $"public static {fullName} Deserialize(ref global::HexaEngine.Sappho.SapphoReader reader)"))
            {
                writer.WriteLine($"{fullName} result = new();");
                if (!makeExternal && symbol.Kind == TypeKind.Class)
                {
                    writer.WriteLine($"reader.AddReference(result);");
                }

                foreach (var member in members)
                {
                    GenerateDeserializeCode(writer, member, "result");
                }

                writer.WriteLine("return result;");
            }
        }

        private static void WriteSerialize(CodeWriter writer, TypeInfo symbol, bool makeExternal, MemberInfo[] members)
        {
            using (writer.PushBlock(makeExternal ? $"public static void Serialize(ref global::HexaEngine.Sappho.SapphoWriter writer, in {symbol.FullName} value)" : "public void Serialize(ref global::HexaEngine.Sappho.SapphoWriter writer)"))
            {
                foreach (var member in members)
                {
                    GenerateSerializeCode(writer, member, makeExternal ? "value" : "");
                }
            }
        }

        private static void GenerateSerializeCode(CodeWriter writer, in MemberInfo member, string memberPath = "")
        {
            var memberAccess = string.IsNullOrWhiteSpace(memberPath) ? member.Name : $"{memberPath}.{member.Name}";
            WriteSerialize(writer, member.Type, memberAccess);
        }

        private static void WriteSerialize(CodeWriter writer, in TypeInfo typeInfo, string memberAccess)
        {
            switch (typeInfo.Kind)
            {
                case TypeKind.Primitive:
                    writer.WriteLine($"writer.WritePrim({memberAccess});");
                    break;

                case TypeKind.String:
                    writer.WriteLine($"writer.{(typeInfo.IsNullable ? "WriteStrNullable" : "WriteStr")}({memberAccess});");
                    break;

                case TypeKind.Enum:
                    writer.WriteLine($"writer.WriteEnum({memberAccess});");
                    break;

                case TypeKind.Class:
                    writer.WriteLine($"writer.WriteObject({memberAccess});");
                    break;

                case TypeKind.Struct:
                    writer.WriteLine($"Serialize(ref writer, {memberAccess});");
                    break;

                case TypeKind.Array:
                    TypeInfo elementType = new((typeInfo.Symbol as IArrayTypeSymbol)!.ElementType);
                    var varName = $"{memberAccess.Replace('.', '_')}Len";
                    writer.WriteLine(typeInfo.IsNullable ? $"uint {varName} = (uint)({memberAccess}?.Length ?? 0);" : $"uint {varName} = (uint){memberAccess}.Length;");
                    writer.WriteLine($"writer.WritePrim({varName});");
                    writer.BeginBlock($"for (uint i = 0; i < {varName}; ++i)");
                    WriteSerialize(writer, elementType, $"{memberAccess}[i]");
                    writer.EndBlock();
                    break;

                default:
                    break;
            }
        }

        private static void GenerateDeserializeCode(CodeWriter writer, MemberInfo member, string memberPath)
        {
            var memberAccess = $"{memberPath}.{member.Name}";
            TypeInfo typeInfo = new(member.TypeSymbol);
            WriteDeserialize(writer, typeInfo, memberAccess, member.Flags);
        }

        private static void WriteDeserialize(CodeWriter writer, in TypeInfo type, string memberAccess, MemberFlags flags)
        {
            switch (type.Kind)
            {
                case TypeKind.Primitive:
                    writer.WriteLine($"{memberAccess} = reader.ReadPrim<{type.FullName}>();");
                    break;

                case TypeKind.String:
                    writer.WriteLine($"{memberAccess} = reader.{(type.IsNullable ? "ReadStrNullable" : "ReadStr")}();");
                    break;

                case TypeKind.Enum:
                    writer.WriteLine($"{memberAccess} = reader.ReadEnum<{type.FullName}>();");
                    break;

                case TypeKind.Class:
                    writer.WriteLine($"{memberAccess} = reader.{(flags.HasFlag(MemberFlags.Polymorphic) ? "ReadObjectPolymorphic" : "ReadObject")}<{type.FullName}>();");
                    break;

                case TypeKind.Struct:
                    writer.WriteLine($"{memberAccess} = Deserialize{type.Name}(ref reader);");
                    break;

                case TypeKind.Array:
                    TypeInfo elementType = new((type.Symbol as IArrayTypeSymbol)!.ElementType);
                    var varName = $"{memberAccess.Replace('.', '_')}Len";
                    writer.WriteLine($"uint {varName} = reader.ReadPrim<uint>();");
                    if (type.IsNullable)
                    {
                        writer.BeginBlock($"if ({varName} != 0)");
                    }
                    writer.WriteLine($"{memberAccess} = new {elementType.FullName}[{varName}];");
                    writer.BeginBlock($"for (uint i = 0; i < {varName}; ++i)");
                    WriteDeserialize(writer, elementType, $"{memberAccess}[i]", flags);
                    writer.EndBlock();
                    if (type.IsNullable)
                    {
                        writer.EndBlock();
                    }
                    break;

                default:
                    break;
            }
        }
    }
}