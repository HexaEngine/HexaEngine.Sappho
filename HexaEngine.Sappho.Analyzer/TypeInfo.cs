namespace HexaEngine.Sappho.Analyzer
{
    using Microsoft.CodeAnalysis;

    public struct TypeInfo
    {
        public ITypeSymbol Symbol;
        public TypeKind Kind;
        public bool IsNullable;

        public TypeInfo(ITypeSymbol symbol)
        {
            Symbol = symbol;
            if (symbol is INamedTypeSymbol { IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
            {
                IsNullable = true;
                symbol = nullableType.TypeArguments[0];
            }
            else if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                IsNullable = true;
            }

            if (symbol.SpecialType != SpecialType.None)
            {
                if (symbol.SpecialType == SpecialType.System_String)
                {
                    Kind = TypeKind.String;
                    return;
                }

                Kind = TypeKind.Primitive;
            }
            else if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
            {
                Kind = TypeKind.Array;
            }
            else if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
            {
                Kind = TypeKind.Enum;
            }
            else if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class)
            {
                Kind = TypeKind.Class;
            }
            else if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct)
            {
                Kind = TypeKind.Struct;
            }
            else
            {
                Kind = TypeKind.Unknown;
            }
        }

        public readonly string Name => Symbol.Name;

        public readonly string FullName => Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}