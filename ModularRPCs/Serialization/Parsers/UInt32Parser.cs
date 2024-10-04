using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UInt32Parser : BinaryTypeParser<uint>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 4;
    public override unsafe int WriteObject(uint value, byte* bytes, uint maxSize)
    {
        if (maxSize < 4)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UInt32Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            bytes[3] = unchecked((byte)value);
            bytes[2] = unchecked((byte)(value >>> 8));
            bytes[1] = unchecked((byte)(value >>> 16));
            *bytes   = unchecked((byte)(value >>> 24));
        }
        
        return 4;
    }
    public override int WriteObject(uint value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(4);
        try
        {
#else
        Span<byte> span = stackalloc byte[4];
#endif

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value);
        }
        else
        {
            span[3] = unchecked((byte)value);
            span[2] = unchecked((byte)(value >>> 8));
            span[1] = unchecked((byte)(value >>> 16));
            span[0] = unchecked((byte)(value >>> 24));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 4);
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
        return 4;
    }
    public override unsafe uint ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 4)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UInt32Parser))) { ErrorCode = 1 };

        uint value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes)
            : unchecked((uint)(*bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]));

        bytesRead = 4;
        return value;
    }
    public override uint ReadObject(Stream stream, out int bytesRead)
    {
        uint value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(4);
        try
        {
            int ct = stream.Read(span, 0, 4);
#else
        Span<byte> span = stackalloc byte[4];
        int ct = stream.Read(span);
#endif

        bytesRead = ct;
        if (ct != 4)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UInt32Parser))) { ErrorCode = 2 };

        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref span[0])
            : unchecked((uint)(span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3]));
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return value;
    }
    public class Many(SerializationConfiguration config) : UnmanagedValueTypeBinaryArrayTypeParser<uint>(config);
}