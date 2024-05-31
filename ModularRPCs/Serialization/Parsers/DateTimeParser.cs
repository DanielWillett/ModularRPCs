using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class DateTimeParser : BinaryTypeParser<DateTime>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(DateTime value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(DateTimeParser))) { ErrorCode = 1 };

        long z64 = ToInt64(value);

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, z64);
        }
        else
        {
            bytes[7] = unchecked((byte)z64);
            bytes[6] = unchecked((byte)(z64 >>> 8));
            bytes[5] = unchecked((byte)(z64 >>> 16));
            bytes[4] = unchecked((byte)(z64 >>> 24));
            bytes[3] = unchecked((byte)(z64 >>> 32));
            bytes[2] = unchecked((byte)(z64 >>> 40));
            bytes[1] = unchecked((byte)(z64 >>> 48));
            *bytes   = unchecked((byte)(z64 >>> 56));
        }
        
        return 8;
    }
    public override int WriteObject(DateTime value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
#else
        Span<byte> span = stackalloc byte[8];
#endif
        long z64 = ToInt64(value);
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], z64);
        }
        else
        {
            span[7] = unchecked((byte)z64);
            span[6] = unchecked((byte)(z64 >>> 8));
            span[5] = unchecked((byte)(z64 >>> 16));
            span[4] = unchecked((byte)(z64 >>> 24));
            span[3] = unchecked((byte)(z64 >>> 32));
            span[2] = unchecked((byte)(z64 >>> 40));
            span[1] = unchecked((byte)(z64 >>> 48));
            span[0] = unchecked((byte)(z64 >>> 56));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 8);
#else
        stream.Write(span);
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        return 8;
    }
    public override unsafe DateTime ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(DateTimeParser))) { ErrorCode = 1 };

        long value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<long>(bytes)
            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);

        bytesRead = 8;
        return FromInt64(value);
    }
    private static DateTime FromInt64(long l)
    {
        DateTimeKind kind = (DateTimeKind)((l >> 62) & 0b11);
        l &= ~(0b11L << 62);
        return new DateTime(l, kind);
    }
    private static long ToInt64(DateTime dateTime)
    {
        return (long)dateTime.Kind << 62 | dateTime.Ticks;
    }
    public override DateTime ReadObject(Stream stream, out int bytesRead)
    {
        long value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
            int ct = stream.Read(span, 0, 8);
#else
        Span<byte> span = stackalloc byte[8];
        int ct = stream.Read(span);
#endif

        if (ct != 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(DateTimeParser))) { ErrorCode = 2 };
        
        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<long>(ref span[0])
            : ((long)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) | ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 8;
        return FromInt64(value);
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<DateTime>
    {
        public Many(SerializationConfiguration config) : base(config, sizeof(long), sizeof(long), !BitConverter.IsLittleEndian, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, DateTime dateTime)
        {
            *(long*)ptr = ToInt64(dateTime);
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, DateTime dateTime)
        {
            Unsafe.WriteUnaligned(ptr, ToInt64(dateTime));
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, DateTime dateTime)
        {
            long int64 = ToInt64(dateTime);
            MemoryMarshal.Write(span, ref int64);
        }
        private static DateTime ReadFromBufferIntl(byte* ptr)
        {
            return FromInt64(*(long*)ptr);
        }
        private static DateTime ReadFromBufferUnalignedIntl(byte* ptr)
        {
            return FromInt64(Unsafe.ReadUnaligned<long>(ptr));
        }
        private static DateTime ReadFromBufferSpanIntl(Span<byte> span)
        {
            return FromInt64(MemoryMarshal.Read<long>(span));
        }
    }
}