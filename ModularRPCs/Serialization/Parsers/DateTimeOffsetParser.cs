using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class DateTimeOffsetParser : BinaryTypeParser<DateTimeOffset>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 10;
    public override unsafe int WriteObject(DateTimeOffset value, byte* bytes, uint maxSize)
    {
        if (maxSize < 10)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(DateTimeOffsetParser))) { ErrorCode = 1 };

        ToComponents(ref value, out long ticks, out short offset);
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, ticks);
            Unsafe.WriteUnaligned(bytes + 8, offset);
        }
        else
        {
            bytes[7] = unchecked((byte)ticks);
            bytes[6] = unchecked((byte)(ticks >>> 8));
            bytes[5] = unchecked((byte)(ticks >>> 16));
            bytes[4] = unchecked((byte)(ticks >>> 24));
            bytes[3] = unchecked((byte)(ticks >>> 32));
            bytes[2] = unchecked((byte)(ticks >>> 40));
            bytes[1] = unchecked((byte)(ticks >>> 48));
            *bytes   = unchecked((byte)(ticks >>> 56));

            bytes[9] = unchecked((byte)offset);
            bytes[8] = unchecked((byte)(offset >>> 8));
        }
        
        return 10;
    }
    public override int WriteObject(DateTimeOffset value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(10);
        try
        {
#else
        Span<byte> span = stackalloc byte[10];
#endif
        
        ToComponents(ref value, out long ticks, out short offset);
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], ticks);
            Unsafe.WriteUnaligned(ref span[8], offset);
        }
        else
        {
            span[7] = unchecked((byte)ticks);
            span[6] = unchecked((byte)(ticks >>> 8));
            span[5] = unchecked((byte)(ticks >>> 16));
            span[4] = unchecked((byte)(ticks >>> 24));
            span[3] = unchecked((byte)(ticks >>> 32));
            span[2] = unchecked((byte)(ticks >>> 40));
            span[1] = unchecked((byte)(ticks >>> 48));
            span[0] = unchecked((byte)(ticks >>> 56));

            span[9] = unchecked((byte)offset);
            span[8] = unchecked((byte)(offset >>> 8));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 10);
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
        return 10;
    }
    public override unsafe DateTimeOffset ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 10)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(DateTimeOffsetParser))) { ErrorCode = 1 };

        long ticks = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<long>(bytes)
            : ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);

        short offset = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<short>(bytes + 8)
            : unchecked( (short)(bytes[8] << 8 | bytes[9]) );

        bytesRead = 10;
        return FromComponents(ticks, offset);
    }
    private static DateTimeOffset FromComponents(long ticks, short offset)
    {
        return new DateTimeOffset(new DateTime(ticks), TimeSpan.FromMinutes(offset));
    }
    private static void ToComponents(ref DateTimeOffset dateTime, out long ticks, out short offset)
    {
        ticks = dateTime.Ticks;
        offset = (short)Math.Round(dateTime.Offset.TotalMinutes);
    }
    public override DateTimeOffset ReadObject(Stream stream, out int bytesRead)
    {
        long ticks;
        short offset;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(10);
        try
        {
            int ct = stream.Read(span, 0, 10);
#else
        Span<byte> span = stackalloc byte[10];
        int ct = stream.Read(span);
#endif

        if (ct != 10)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(DateTimeOffsetParser))) { ErrorCode = 2 };

        ticks = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<long>(ref span[0])
            : ((long)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) | ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);

        offset = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<short>(ref span[8])
            : unchecked((short)(span[8] << 8 | span[9]));

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 10;
        return FromComponents(ticks, offset);
    }
}