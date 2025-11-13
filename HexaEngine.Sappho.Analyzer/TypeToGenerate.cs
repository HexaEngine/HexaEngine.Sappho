namespace HexaEngine.Sappho.Analyzer
{
    using Microsoft.CodeAnalysis;
    using System.Collections.Immutable;

    public struct TypeToGenerate
    {
        private readonly ITypeSymbol symbol;
        public string Namespace;

        public TypeToGenerate(ITypeSymbol symbol)
        {
            this.symbol = symbol;
            Namespace = symbol.ContainingNamespace.ToDisplayString();
        }

        public readonly ITypeSymbol Symbol => symbol;

        public readonly string Name => symbol.Name;

        public readonly bool IsClass => symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class;
    }
}