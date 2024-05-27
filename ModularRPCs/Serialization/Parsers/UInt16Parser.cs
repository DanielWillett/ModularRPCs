using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UInt16Parser : BinaryTypeParser<ushort>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 2;
    public override unsafe int WriteObject(ushort value, byte* bytes, uint maxSize)
    {
        if (maxSize < 2)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UInt16Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            bytes[1] = unchecked((byte)value);
            *bytes   = unchecked((byte)(value >>> 8));
        }
        
        return 2;
    }
    public override int WriteObject(ushort value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(2);
        try
        {
#else
        Span<byte> span = stackalloc byte[2];
#endif

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value);
        }
        else
        {
            span[1] = unchecked((byte)value);
            span[0] = unchecked((byte)(value >>> 8));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 2);
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
        return 2;
    }
    public override unsafe ushort ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 2)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UInt16Parser))) { ErrorCode = 1 };

        int value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(bytes)
            : *bytes << 8 | bytes[1];

        bytesRead = 2;
        return unchecked( (ushort)value );
    }
    public override ushort ReadObject(Stream stream, out int bytesRead)
    {
        int value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(2);
        try
        {
            int ct = stream.Read(span, 0, 2);
#else
        Span<byte> span = stackalloc byte[2];
        int ct = stream.Read(span);
#endif

        if (ct != 2)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UInt16Parser))) { ErrorCode = 2 };

        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(ref span[0])
            : span[0] << 8 | span[1];
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 2;
        return unchecked( (ushort)value );
    }
    public class Many : UnmanagedValueTypeBinaryArrayTypeParser<ushort>;
}