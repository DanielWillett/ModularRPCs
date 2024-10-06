using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class TimeSpanParser : BinaryTypeParser<TimeSpan>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(TimeSpan value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(TimeSpanParser))) { ErrorCode = 1 };

        long ticks = value.Ticks;
        Unsafe.WriteUnaligned(bytes, BitConverter.IsLittleEndian ? ticks : BinaryPrimitives.ReverseEndianness(ticks));

        return 8;
    }
    public override int WriteObject(TimeSpan value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
#else
        Span<byte> span = stackalloc byte[8];
#endif
        long ticks = value.Ticks;
        Unsafe.WriteUnaligned(ref span[0], BitConverter.IsLittleEndian ? ticks : BinaryPrimitives.ReverseEndianness(ticks));

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
    public override unsafe TimeSpan ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(TimeSpanParser))) { ErrorCode = 1 };

        long value = Unsafe.ReadUnaligned<long>(bytes);
        if (!BitConverter.IsLittleEndian)
            value = BinaryPrimitives.ReverseEndianness(value);

        bytesRead = 8;
        return new TimeSpan(value);
    }
    public override TimeSpan ReadObject(Stream stream, out int bytesRead)
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

        bytesRead = ct;
        if (ct != 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(TimeSpanParser))) { ErrorCode = 2 };

        value = Unsafe.ReadUnaligned<long>(ref span[0]);
        if (!BitConverter.IsLittleEndian)
            value = BinaryPrimitives.ReverseEndianness(value);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return new TimeSpan(value);
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<TimeSpan>
    {
        public Many(SerializationConfiguration config) : base(config, sizeof(long), sizeof(long), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }

        private static void WriteToBufferIntl(byte* ptr, TimeSpan timeSpan)
        {
            *(long*)ptr = timeSpan.Ticks;
        }

        private static void WriteToBufferUnalignedIntl(byte* ptr, TimeSpan timeSpan)
        {
            Unsafe.WriteUnaligned(ptr, timeSpan.Ticks);
        }

        private static void WriteToBufferSpanIntl(Span<byte> span, TimeSpan timeSpan)
        {
            long ticks = timeSpan.Ticks;
            MemoryMarshal.Write(span, ref ticks);
        }

        private static TimeSpan ReadFromBufferIntl(byte* ptr)
        {
            return new TimeSpan(*(long*)ptr);
        }

        private static TimeSpan ReadFromBufferUnalignedIntl(byte* ptr)
        {
            return new TimeSpan(Unsafe.ReadUnaligned<long>(ptr));
        }

        private static TimeSpan ReadFromBufferSpanIntl(Span<byte> span)
        {
            return new TimeSpan(MemoryMarshal.Read<long>(span));
        }
    }
}