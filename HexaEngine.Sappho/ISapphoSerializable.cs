namespace HexaEngine.Sappho
{
    public interface ISapphoSerializable
    {
        SapphoTypeId TypeId { get; }

        void Serialize(ref SapphoWriter writer);
    }

    public interface ISapphoSerializable<T> : ISapphoSerializable where T : ISapphoSerializable<T>
    {
        static abstract T Deserialize(ref SapphoReader reader);
    }

    public interface ISapphoSerializer<T>
    {
        static abstract void Serialize(ref SapphoWriter writer, in T val);

        static abstract T Deserialize(ref SapphoReader reader);
    }

    public interface ISapphoInstanceId
    {
        [SapphoIgnore]
        public Guid InstanceId { get; }
    }
}