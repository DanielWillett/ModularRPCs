using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UInt64Parser : BinaryTypeParser<ulong>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(ulong value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UInt64Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            bytes[7] = unchecked((byte)value);
            bytes[6] = unchecked((byte)(value >>> 8));
            bytes[5] = unchecked((byte)(value >>> 16));
            bytes[4] = unchecked((byte)(value >>> 24));
            bytes[3] = unchecked((byte)(value >>> 32));
            bytes[2] = unchecked((byte)(value >>> 40));
            bytes[1] = unchecked((byte)(value >>> 48));
            *bytes   = unchecked((byte)(value >>> 56));
        }
        
        return 8;
    }
    public override int WriteObject(ulong value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
#else
        Span<byte> span = stackalloc byte[8];
#endif
            
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value);
        }
        else
        {
            span[7] = unchecked((byte)value);
            span[6] = unchecked((byte)(value >>> 8));
            span[5] = unchecked((byte)(value >>> 16));
            span[4] = unchecked((byte)(value >>> 24));
            span[3] = unchecked((byte)(value >>> 32));
            span[2] = unchecked((byte)(value >>> 40));
            span[1] = unchecked((byte)(value >>> 48));
            span[0] = unchecked((byte)(value >>> 56));
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
    public override unsafe ulong ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UInt64Parser))) { ErrorCode = 1 };

        ulong value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(bytes)
            : ((ulong)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);

        bytesRead = 8;
        return value;
    }
    public override ulong ReadObject(Stream stream, out int bytesRead)
    {
        ulong value;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UInt64Parser))) { ErrorCode = 2 };
        
        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ref span[0])
            : ((ulong)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) | ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 8;
        return value;
    }
}