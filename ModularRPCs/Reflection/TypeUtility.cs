using System;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class TypeUtility
{
    public const TypeCode MaxUsedTypeCode = (TypeCode)19;

    public const TypeCode TypeCodeTimeSpan = (TypeCode)17;
    public const TypeCode TypeCodeGuid = (TypeCode)19;
    public static Type GetType(TypeCode tc)
    {
        return tc switch
        {
            TypeCode.Empty => throw new ArgumentOutOfRangeException(nameof(tc)),
            TypeCode.Object => typeof(object),
            TypeCode.DBNull => typeof(DBNull),
            TypeCode.Boolean => typeof(bool),
            TypeCode.Char => typeof(char),
            TypeCode.SByte => typeof(sbyte),
            TypeCode.Byte => typeof(byte),
            TypeCode.Int16 => typeof(short),
            TypeCode.UInt16 => typeof(ushort),
            TypeCode.Int32 => typeof(int),
            TypeCode.UInt32 => typeof(uint),
            TypeCode.Int64 => typeof(long),
            TypeCode.UInt64 => typeof(ulong),
            TypeCode.Single => typeof(float),
            TypeCode.Double => typeof(double),
            TypeCode.Decimal => typeof(decimal),
            TypeCode.DateTime => typeof(DateTime),
            TypeCodeTimeSpan => typeof(TimeSpan),
            TypeCode.String => typeof(string),
            TypeCodeGuid => typeof(Guid),
            _ => throw new ArgumentOutOfRangeException(nameof(tc))
        };
    }
    public static int GetTypeCodeSize(TypeCode tc)
    {
        return tc switch
        {
            TypeCode.Boolean or TypeCode.SByte or TypeCode.Byte => 1,
            TypeCode.Char => sizeof(char),
            TypeCode.Int16 => sizeof(short),
            TypeCode.UInt16 => sizeof(ushort),
            TypeCode.Int32 => sizeof(int),
            TypeCode.UInt32 => sizeof(uint),
            TypeCode.Int64 => sizeof(long),
            TypeCode.UInt64 => sizeof(ulong),
            TypeCode.Single => sizeof(float),
            TypeCode.Double => sizeof(double),
            TypeCode.Decimal => sizeof(int) * 4,
            TypeCode.DateTime => sizeof(long),
            TypeCodeTimeSpan => sizeof(long),
            TypeCode.String => 1,
            TypeCodeGuid => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(tc), tc, null)
        };
    }
}
