using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class SingleParser : BinaryTypeParser<float>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 4;
    public override unsafe int WriteObject(float value, byte* bytes, uint maxSize)
    {
        if (maxSize < 4)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(SingleParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            int write = *(int*)&value;
            bytes[3] = unchecked((byte)write);
            bytes[2] = unchecked((byte)(write >>> 8));
            bytes[1] = unchecked((byte)(write >>> 16));
            *bytes   = unchecked((byte)(write >>> 24));
        }
        
        return 4;
    }
    public override unsafe int WriteObject(float value, Stream stream)
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
            int write = *(int*)&value;
            span[3] = unchecked((byte)write);
            span[2] = unchecked((byte)(write >>> 8));
            span[1] = unchecked((byte)(write >>> 16));
            span[0] = unchecked((byte)(write >>> 24));
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
    public override unsafe float ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 4)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOutIBinaryTypeParser, nameof(SingleParser))) { ErrorCode = 1 };

        float value;
        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<float>(bytes);
        }
        else
        {
            int read = *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
            value = *(float*)&read;
        }

        bytesRead = 4;
        return value;
    }
    public override unsafe float ReadObject(Stream stream, out int bytesRead)
    {
        float value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(4);
        try
        {
            int ct = stream.Read(span, 0, 4);
#else
        Span<byte> span = stackalloc byte[4];
        int ct = stream.Read(span);
#endif

        if (ct != 4)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOutIBinaryTypeParser, nameof(SingleParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<float>(ref span[0]);
        }
        else
        {
            int read = span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3];
            value = *(float*)&read;
        }
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 4;
        return value;
    }
}