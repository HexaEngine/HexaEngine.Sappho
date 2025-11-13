namespace HexaEngine.Sappho
{
    using Hexa.NET.Utilities;
    using System.Runtime.CompilerServices;
    using System.Text;

    public unsafe struct SapphoWriter : IDisposable
    {
        private byte* buffer;
        private uint count;
        private uint capacity;
        private readonly SapphoSerializationContext context;

        public SapphoWriter(SapphoSerializationContext context)
        {
            this.context = context;
        }

        public SapphoWriter(SapphoSerializationContext context, uint initialCapacity)
        {
            this.context = context;
            Capacity = initialCapacity;
        }

        public void Reset()
        {
            count = 0;
        }

        public readonly byte* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => buffer;
        }

        public readonly uint Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count;
        }

        public uint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var newBuffer = Utils.AllocT<byte>(value);
                if (buffer != null)
                {
                    Utils.MemcpyT(buffer, newBuffer, count);
                    Utils.Free(buffer);
                }
                buffer = newBuffer;
                capacity = value;
            }
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                Utils.Free(buffer);
                buffer = null;
            }
            count = 0;
            capacity = 0;
        }

        private void EnsureCapacity(uint newCapacity)
        {
            if (newCapacity > capacity)
            {
                Capacity = Math.Max(newCapacity, capacity * 2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte* data, uint count)
        {
            uint newCount = this.count + count;
            EnsureCapacity(newCount);
            Utils.MemcpyT(data, buffer + this.count, count);
            this.count = newCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEndianAware(byte* data, uint count)
        {
            if (BitConverter.IsLittleEndian)
            {
                Write(data, count);
            }
            else
            {
                byte* buffer = stackalloc byte[(int)count];
                for (uint i = 0; i < count; i++)
                {
                    buffer[i] = data[count - i - 1];
                }
                Write(buffer, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePrim<T>(T value) where T : unmanaged
        {
            WriteEndianAware((byte*)&value, (uint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArray(ReadOnlySpan<byte> span)
        {
            WritePrim((uint)span.Length);
            fixed (byte* ptr = span)
            {
                Write(ptr, (uint)span.Length);
            }
        }

        public void WriteStr(string str)
        {
            WriteStr(str.AsSpan());
        }

        public void WriteStrNullable(string? str)
        {
            if (str == null)
            {
                WritePrim((uint)0u);
                return;
            }
            WriteStr(str.AsSpan());
        }

        public void WriteStr(ReadOnlySpan<char> span)
        {
            var byteCount = Encoding.UTF8.GetByteCount(span);
            byte* buffer;
            if (byteCount > Utils.StackAllocLimit)
            {
                buffer = Utils.AllocT<byte>((uint)byteCount);
            }
            else
            {
                byte* stackBuffer = stackalloc byte[byteCount];
                buffer = stackBuffer;
            }

            WritePrim((uint)byteCount);
            if (byteCount == 0)
            {
                return;
            }

            fixed (char* ptr = span)
            {
                Encoding.UTF8.GetBytes(ptr, span.Length, buffer, byteCount);
            }
            Write(buffer, (uint)byteCount);

            if (byteCount > Utils.StackAllocLimit)
            {
                Utils.Free(buffer);
            }
        }

        public void WriteEnum<T>(T value) where T : unmanaged, Enum
        {
            var baseType = value.GetEnumBaseType();
            switch (baseType)
            {
                case EnumBaseType.Byte:
                    WritePrim(*(byte*)&value);
                    break;

                case EnumBaseType.SByte:
                    WritePrim(*(sbyte*)&value);
                    break;

                case EnumBaseType.UShort:
                    WritePrim(*(ushort*)&value);
                    break;

                case EnumBaseType.Short:
                    WritePrim(*(short*)&value);
                    break;

                case EnumBaseType.UInt:
                    WritePrim(*(uint*)&value);
                    break;

                case EnumBaseType.Int:
                    WritePrim(*(int*)&value);
                    break;

                case EnumBaseType.ULong:
                    WritePrim(*(ulong*)&value);
                    break;

                case EnumBaseType.Long:
                    WritePrim(*(long*)&value);
                    break;

                default:
                    throw new NotSupportedException($"Enum base type '{baseType}' is not supported.");
            }
        }

        public void WriteObject<T>(T? obj) where T : class, ISapphoSerializable<T>
        {
            if (obj == null)
            {
                WritePrim(SapphoTypeId.NullType);
                return;
            }

            Guid? instanceId = default;
            if (obj is ISapphoInstanceId instanceIdGetter)
            {
                instanceId = instanceIdGetter.InstanceId;
            }

            var refId = context.GetOrAddReferenceId(obj, instanceId, out bool exists);
            SapphoTypeIdFlags flags = exists ? SapphoTypeIdFlags.IsReference : SapphoTypeIdFlags.None;
            SapphoTypeId typeId = new(obj.TypeId, flags);
            WritePrim(typeId);
            WritePrim(refId);
            if (!exists)
            {
                obj.Serialize(ref this);
            }
        }

        public void WriteValue<T>(in T obj) where T : ISapphoSerializable<T>
        {
            obj.Serialize(ref this);
        }

        public void WriteValue<T, TSerializer>(in T val) where TSerializer : ISapphoSerializer<T>
        {
            TSerializer.Serialize(ref this, val);
        }
    }
}