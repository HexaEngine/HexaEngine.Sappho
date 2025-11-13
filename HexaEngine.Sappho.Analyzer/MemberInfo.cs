namespace HexaEngine.Sappho.Analyzer
{
    using Microsoft.CodeAnalysis;

    public struct MemberInfo
    {
        public string Name;
        public string TypeName;
        public TypeInfo Type;
        public MemberKind Kind;
        public MemberFlags Flags;

        public MemberInfo(IPropertySymbol prop, MemberFlags flags)
        {
            Name = prop.Name;
            TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            Type = new(prop.Type);
            Kind = MemberKind.Property;
            Flags = flags;
        }

        public MemberInfo(IFieldSymbol field, MemberFlags flags)
        {
            Name = field.Name;
            TypeName = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            Type = new(field.Type);
            Kind = MemberKind.Field;
            Flags = flags;
        }

        public static MemberFlags Classify(ISymbol symbol)
        {
            MemberFlags flags = MemberFlags.None;
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.IsAttribute<SapphoIgnoreAttribute>())
                {
                    flags |= MemberFlags.Ignored;
                }
                else if (attr.IsAttribute<SapphoPolymorphicAttribute>())
                {
                    flags |= MemberFlags.Polymorphic;
                }
            }
            return flags;
        }

        public readonly ITypeSymbol TypeSymbol => Type.Symbol;

        public readonly bool HasFlag(MemberFlags flag) => (Flags & flag) != 0;
    }
}