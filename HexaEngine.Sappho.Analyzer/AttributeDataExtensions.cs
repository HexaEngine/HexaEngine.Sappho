namespace HexaEngine.Sappho.Analyzer
{
    using Microsoft.CodeAnalysis;

    public static class AttributeDataExtensions
    {
        public static bool IsAttribute(this AttributeData attributeData, string @namespace, string attributeName)
        {
            return attributeData.AttributeClass?.Name == attributeName && attributeData.AttributeClass.ContainingNamespace.ToDisplayString() == @namespace;
        }

        public static bool IsAttribute<T>(this AttributeData attributeData) where T : Attribute
        {
            var type = typeof(T);
            return attributeData.IsAttribute(type.Namespace, type.Name);
        }
    }
}