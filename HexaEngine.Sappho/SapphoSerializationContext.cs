namespace HexaEngine.Sappho
{
    public abstract class SapphoObjectSerializer
    {
        public abstract object ReadObject(ref SapphoReader reader);

        public abstract void WriteObject(ref SapphoWriter writer, object obj);
    }

    public class SapphoObjectSerializer<T> : SapphoObjectSerializer where T : ISapphoSerializable<T>
    {
        public static readonly SapphoObjectSerializer<T> Instance = new();

        public override object ReadObject(ref SapphoReader reader)
        {
            return T.Deserialize(ref reader);
        }

        public override void WriteObject(ref SapphoWriter writer, object obj)
        {
            ((T)obj).Serialize(ref writer);
        }
    }

    public class SapphoSerializationContext
    {
        private readonly Dictionary<object, Guid> objectToRefId = [];
        private readonly Dictionary<Guid, object> refIdToObject = [];

        private readonly Dictionary<SapphoTypeId, SapphoObjectSerializer> serializers = [];

        public SapphoObjectSerializer GetSerializer(SapphoTypeId typeId)
        {
            typeId.Flags = 0;
            if (serializers.TryGetValue(typeId, out var serializer))
            {
                return serializer;
            }
            throw new InvalidOperationException($"Serializer for type ID {typeId} not found");
        }

        public void RegisterType(SapphoTypeId typeId, SapphoObjectSerializer serializer)
        {
            typeId.Flags = 0;
            serializers[typeId] = serializer;
        }

        public SapphoObjectSerializer RegisterType<T>(SapphoTypeId typeId) where T : ISapphoSerializable<T>
        {
            var serializer = SapphoObjectSerializer<T>.Instance;
            RegisterType(typeId, serializer);
            return serializer;
        }

        public Guid GetOrAddReferenceId(object obj, Guid? instanceId, out bool exists)
        {
            if (objectToRefId.TryGetValue(obj, out Guid refId))
            {
                exists = true;
                return refId;
            }
            exists = false;
            refId = instanceId ?? Guid.NewGuid();
            objectToRefId[obj] = refId;
            return refId;
        }

        public void AddReference<T>(Guid refId, T obj) where T : class
        {
            refIdToObject[refId] = obj;
            objectToRefId[obj] = refId;
        }

        public T GetReference<T>(Guid refId) where T : class
        {
            if (refIdToObject.TryGetValue(refId, out object? obj))
            {
                return (T)obj;
            }
            throw new InvalidOperationException($"Reference with ID {refId} not found");
        }
    }
}