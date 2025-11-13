namespace HexaEngine.Sappho
{
    public static class EnumHelper<T> where T : Enum
    {
        public static readonly Type BaseType = Enum.GetUnderlyingType(typeof(T));
        public static readonly EnumBaseType NumericType;

        static EnumHelper()
        {
            if (BaseType == typeof(byte))
            {
                NumericType = EnumBaseType.Byte;
            }
            else if (BaseType == typeof(sbyte))
            {
                NumericType = EnumBaseType.SByte;
            }
            else if (BaseType == typeof(ushort))
            {
                NumericType = EnumBaseType.UShort;
            }
            else if (BaseType == typeof(short))
            {
                NumericType = EnumBaseType.Short;
            }
            else if (BaseType == typeof(uint))
            {
                NumericType = EnumBaseType.UInt;
            }
            else if (BaseType == typeof(int))
            {
                NumericType = EnumBaseType.Int;
            }
            else if (BaseType == typeof(ulong))
            {
                NumericType = EnumBaseType.ULong;
            }
            else if (BaseType == typeof(long))
            {
                NumericType = EnumBaseType.Long;
            }
            else
            {
                throw new NotSupportedException($"Enum base type {BaseType} is not supported.");
            }
        }
    }

    public enum EnumBaseType
    {
        Unsupported,
        Byte,
        SByte,
        UShort,
        Short,
        UInt,
        Int,
        ULong,
        Long,
    }

    public static class EnumExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static EnumBaseType GetEnumBaseType<T>(this T value) where T : Enum
        {
            return EnumHelper<T>.NumericType;
        }
    }
}