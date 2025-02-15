using System;

namespace DanielWillett.ModularRpcs.Data;

#if !NET8_0_OR_GREATER
internal static class LegacyEnumCache<TEnum>
{
    public const TypeCode NativeInt = (TypeCode)13;
    public const TypeCode NativeUInt = (TypeCode)14;

    public static TypeCode UnderlyingType;

    static LegacyEnumCache()
    {
        Type type = typeof(TEnum);
        if (!type.IsEnum)
            throw new InvalidOperationException();

        UnderlyingType = Type.GetTypeCode(type.GetEnumUnderlyingType());

        if (UnderlyingType is NativeInt or NativeUInt)
            UnderlyingType = TypeCode.Empty;
        else if (type == typeof(nint))
            UnderlyingType = NativeInt;
        else if (type == typeof(nuint))
            UnderlyingType = NativeUInt;
    }
}
#endif