namespace HexaEngine.Sappho
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SapphoMemberAttribute : Attribute
    {
        public string Name { get; }

        public SapphoMemberAttribute(string name)
        {
            Name = name;
        }
    }
}