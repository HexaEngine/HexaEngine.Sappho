namespace HexaEngine.Sappho
{
    using Hexa.NET.Utilities;
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;

    public unsafe struct SapphoReader
    {
        private byte* buffer;
        private uint position;
        private uint length;
        private readonly SapphoSerializationContext context;

        public SapphoReader(byte* buffer, uint length, SapphoSerializationContext context)
        {
            this.buffer = buffer;
            this.length = length;
            this.position = 0;
            this.context = context;
        }

        public readonly SapphoSerializationContext Context => context;

        public Guid InstanceId { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read(byte* dest, uint count)
        {
            if (position + count > length)
                throw new InvalidOperationException("Attempted to read past end of buffer");

            Utils.MemcpyT(buffer + position, dest, count);
            position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadEndianAware(byte* dest, uint count)
        {
            if (BitConverter.IsLittleEndian)
            {
                Read(dest, count);
            }
            else
            {
                byte* temp = stackalloc byte[(int)count];
                Read(temp, count);
                for (uint i = 0; i < count; i++)
                {
                    dest[i] = temp[count - i - 1];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadPrim<T>() where T : unmanaged
        {
            T value;
            ReadEndianAware((byte*)&value, (uint)sizeof(T));
            return value;
        }

        public byte[] ReadArray()
        {
            uint arrayLength = ReadPrim<uint>();
            byte[] array = new byte[arrayLength];
            fixed (byte* ptr = array)
            {
                Read(ptr, arrayLength);
            }
            return array;
        }

        public string ReadStr()
        {
            uint byteCount = ReadPrim<uint>();
            if (byteCount == 0)
            {
                return string.Empty;
            }

            byte* strBuffer;
            if (byteCount > Utils.StackAllocLimit)
            {
                strBuffer = Utils.AllocT<byte>(byteCount);
            }
            else
            {
                byte* stackBuffer = stackalloc byte[(int)byteCount];
                strBuffer = stackBuffer;
            }

            Read(strBuffer, byteCount);
            string result = Encoding.UTF8.GetString(strBuffer, (int)byteCount);

            if (byteCount > Utils.StackAllocLimit)
            {
                Utils.Free(strBuffer);
            }

            return result;
        }

        public string? ReadStrNullable()
        {
            uint byteCount = ReadPrim<uint>();
            if (byteCount == 0)
            {
                return null;
            }
            byte* strBuffer;
            if (byteCount > Utils.StackAllocLimit)
            {
                strBuffer = Utils.AllocT<byte>(byteCount);
            }
            else
            {
                byte* stackBuffer = stackalloc byte[(int)byteCount];
                strBuffer = stackBuffer;
            }
            Read(strBuffer, byteCount);
            string result = Encoding.UTF8.GetString(strBuffer, (int)byteCount);
            if (byteCount > Utils.StackAllocLimit)
            {
                Utils.Free(strBuffer);
            }
            return result;
        }

        public T ReadEnum<T>() where T : unmanaged, Enum
        {
            T value = default;
            var baseType = value.GetEnumBaseType();

            switch (baseType)
            {
                case EnumBaseType.Byte:
                    {
                        byte b = ReadPrim<byte>();
                        *(byte*)&value = b;
                    }
                    break;

                case EnumBaseType.SByte:
                    {
                        sbyte sb = ReadPrim<sbyte>();
                        *(sbyte*)&value = sb;
                    }
                    break;

                case EnumBaseType.UShort:
                    {
                        ushort us = ReadPrim<ushort>();
                        *(ushort*)&value = us;
                    }
                    break;

                case EnumBaseType.Short:
                    {
                        short s = ReadPrim<short>();
                        *(short*)&value = s;
                    }
                    break;

                case EnumBaseType.UInt:
                    {
                        uint ui = ReadPrim<uint>();
                        *(uint*)&value = ui;
                    }
                    break;

                case EnumBaseType.Int:
                    {
                        int i = ReadPrim<int>();
                        *(int*)&value = i;
                    }
                    break;

                case EnumBaseType.ULong:
                    {
                        ulong ul = ReadPrim<ulong>();
                        *(ulong*)&value = ul;
                    }
                    break;

                case EnumBaseType.Long:
                    {
                        long l = ReadPrim<long>();
                        *(long*)&value = l;
                    }
                    break;

                default:
                    throw new NotSupportedException($"Enum base type '{baseType}' is not supported.");
            }

            return value;
        }

        public T? ReadObject<T>() where T : class, ISapphoSerializable<T>
        {
            var typeId = ReadPrim<SapphoTypeId>();

            if (typeId == SapphoTypeId.NullType)
            {
                return null;
            }

            Guid refId = ReadPrim<Guid>();

            if (typeId.HasFlag(SapphoTypeIdFlags.IsReference))
            {
                return context.GetReference<T>(refId);
            }

            InstanceId = refId;
            var obj = T.Deserialize(ref this);
            return obj;
        }

        public readonly void AddReference(object obj)
        {
            context.AddReference(InstanceId, obj);
        }

        public T? ReadObjectPolymorphic<T>() where T : class, ISapphoSerializable<T>
        {
            var typeId = ReadPrim<SapphoTypeId>();

            if (typeId == SapphoTypeId.NullType)
            {
                return null;
            }

            Guid refId = ReadPrim<Guid>();

            if (typeId.HasFlag(SapphoTypeIdFlags.IsReference))
            {
                return context.GetReference<T>(refId);
            }

            var serializer = context.GetSerializer(typeId);
            var obj = (T)serializer.ReadObject(ref this);
            context.AddReference(refId, obj);
            return obj;
        }

        public T ReadValue<T>() where T : struct, ISapphoSerializable<T>
        {
            return T.Deserialize(ref this);
        }

        public T ReadValue<T, TSerializer>() where TSerializer : ISapphoSerializer<T>
        {
            return TSerializer.Deserialize(ref this);
        }
    }
}