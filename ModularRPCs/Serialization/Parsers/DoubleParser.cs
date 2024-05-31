using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class DoubleParser : BinaryTypeParser<double>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(double value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(DoubleParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            long write = *(long*)&value;
            bytes[7] = unchecked((byte)write);
            bytes[6] = unchecked((byte)(write >>> 8));
            bytes[5] = unchecked((byte)(write >>> 16));
            bytes[4] = unchecked((byte)(write >>> 24));
            bytes[3] = unchecked((byte)(write >>> 32));
            bytes[2] = unchecked((byte)(write >>> 40));
            bytes[1] = unchecked((byte)(write >>> 48));
            *bytes   = unchecked((byte)(write >>> 56));
        }
        
        return 8;
    }
    public override unsafe int WriteObject(double value, Stream stream)
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
            long write = *(long*)&value;
            span[7] = unchecked((byte)write);
            span[6] = unchecked((byte)(write >>> 8));
            span[5] = unchecked((byte)(write >>> 16));
            span[4] = unchecked((byte)(write >>> 24));
            span[3] = unchecked((byte)(write >>> 32));
            span[2] = unchecked((byte)(write >>> 40));
            span[1] = unchecked((byte)(write >>> 48));
            span[0] = unchecked((byte)(write >>> 56));
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
    public override unsafe double ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(DoubleParser))) { ErrorCode = 1 };

        double value;
        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<double>(bytes);
        }
        else
        {
            long read = ((long)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
            value = *(double*)&read;
        }

        bytesRead = 8;
        return value;
    }
    public override unsafe double ReadObject(Stream stream, out int bytesRead)
    {
        double value;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(DoubleParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<double>(ref span[0]);
        }
        else
        {
            long read = ((long)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) | ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);
            value = *(double*)&read;
        }
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
    public class Many(SerializationConfiguration config) : UnmanagedValueTypeBinaryArrayTypeParser<double>(config);
}