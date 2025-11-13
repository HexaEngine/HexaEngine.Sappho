namespace HexaEngine.Sappho
{
    using System.Runtime.CompilerServices;

    public struct SapphoTypeId : IEquatable<SapphoTypeId>
    {
        public const ulong TypeIdMask = (1ul << 56) - 1;
        public const int FlagShift = 56;
        public const ulong FlagMask = (1ul << 8) - 1;

        private ulong raw;

        public static readonly SapphoTypeId NullType = (ulong)SapphoTypeIdFlags.IsReference;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong MakeId(ulong typeId, SapphoTypeIdFlags flags)
        {
            return (typeId & TypeIdMask) | ((ulong)flags << FlagShift);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is SapphoTypeId id && Equals(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(SapphoTypeId other)
        {
            return raw == other.raw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            return raw.GetHashCode();
        }

        public SapphoTypeId(ulong typeId, SapphoTypeIdFlags flags)
        {
            raw = MakeId(typeId, flags);
        }

        public SapphoTypeId(ulong raw)
        {
            this.raw = raw;
        }

        public ulong TypeId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => raw & TypeIdMask;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => raw = MakeId(value, Flags);
        }

        public SapphoTypeIdFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (SapphoTypeIdFlags)((raw >> FlagShift) & FlagMask);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => raw = MakeId(TypeId, value);
        }

        public readonly bool HasFlag(SapphoTypeIdFlags flag)
        {
            return (Flags & flag) != 0;
        }

        public readonly bool HasFlags(SapphoTypeIdFlags flags)
        {
            return (Flags & flags) == flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(SapphoTypeId typeId) => typeId.raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SapphoTypeId(ulong raw) => new(raw);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SapphoTypeId left, SapphoTypeId right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(SapphoTypeId left, SapphoTypeId right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return $"TypeId: {TypeId}, Flags: {Flags}";
        }
    }
}