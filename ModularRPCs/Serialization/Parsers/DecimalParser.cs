using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class DecimalParser : BinaryTypeParser<decimal>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(decimal value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(DecimalParser))) { ErrorCode = 1 };

#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
#else
        int[] bits = decimal.GetBits(value);
#endif

        for (int i = 0; i < 4; ++i)
        {
            int bit = bits[i];
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(bytes, bit);
            }
            else
            {
                bytes[3] = unchecked((byte)bit);
                bytes[2] = unchecked((byte)(bit >>> 8));
                bytes[1] = unchecked((byte)(bit >>> 16));
                *bytes = unchecked((byte)(bit >>> 24));
            }

            bytes += 4;
        }

        return 16;
    }

    public override int WriteObject(decimal value, Stream stream)
    {
#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
#else
        int[] bits = decimal.GetBits(value);
#endif
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
#else
        Span<byte> span = stackalloc byte[16];
#endif
        for (int i = 0; i < 4; ++i)
        {
            int bit = bits[i];
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ref span[i * 4], bit);
            }
            else
            {
                span[i * 4 + 3] = unchecked((byte)bit);
                span[i * 4 + 2] = unchecked((byte)(bit >>> 8));
                span[i * 4 + 1] = unchecked((byte)(bit >>> 16));
                span[i * 4]     = unchecked((byte)(bit >>> 24));
            }
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 16);
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
        return 16;
    }

    public override unsafe decimal ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOutIBinaryTypeParser, nameof(DecimalParser))) { ErrorCode = 1 };

#if NET5_0_OR_GREATER
        int* bits = stackalloc int[4];
#else
        int[] bits = new int[4];
#endif
        if (BitConverter.IsLittleEndian)
        {
#if NET5_0_OR_GREATER
            Unsafe.CopyBlockUnaligned(bits, bytes, 16u);
#else
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref bits[0]), ref Unsafe.AsRef<byte>(bytes), 16u);
#endif
        }
        else
        {
            for (int i = 0; i < 4; ++i)
                bits[i] = bytes[i * 4] << 24 | bytes[i * 4 + 1] << 16 | bytes[i * 4 + 2] << 8 | bytes[i * 4 + 3];
        }

        bytesRead = 16;
#if NET5_0_OR_GREATER
        return new decimal(new ReadOnlySpan<int>(bits, 4));
#else
        return new decimal(bits);
#endif
    }

    public override decimal ReadObject(Stream stream, out int bytesRead)
    {
        decimal value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            int ct = stream.Read(span, 0, 16);
#else
        Span<byte> span = stackalloc byte[16];
        int ct = stream.Read(span);
#endif

        if (ct != 16)
            throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOutIBinaryTypeParser, nameof(Int32Parser))) { ErrorCode = 2 };

#if NET5_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
#else
        int[] bits = new int[4];
#endif
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref bits[0]), ref span[0], 16u);
        }
        else
        {
            for (int i = 0; i < 4; ++i)
                bits[i] = span[i * 4] << 24 | span[i * 4 + 1] << 16 | span[i * 4 + 2] << 8 | span[i * 4 + 3];
        }

        value = new decimal(bits);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 16;
        return value;
    }
}
