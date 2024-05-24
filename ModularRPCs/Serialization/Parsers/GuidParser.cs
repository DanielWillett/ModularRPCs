using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class GuidParser : BinaryTypeParser<Guid>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Guid value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(GuidParser))) { ErrorCode = 1 };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        value.TryWriteBytes(new Span<byte>(bytes, 16));
#else
        byte[] data = value.ToByteArray();
        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(bytes), ref data[0], 16u);
#endif

        return 16;
    }
    public override int WriteObject(Guid value, Stream stream)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        stream.Write(bytes);
#else
        byte[] data = value.ToByteArray();
        stream.Write(data, 0, 16);
#endif

        return 16;
    }

    public override unsafe Guid ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(GuidParser))) { ErrorCode = 1 };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Guid guid = new Guid(new ReadOnlySpan<byte>(bytes, 16));
#else
        Guid guid;
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            Unsafe.CopyBlockUnaligned(ref span[0], ref Unsafe.AsRef<byte>(bytes), 16u);
            guid = new Guid(span);
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        bytesRead = 16;
        return guid;
    }

    public override Guid ReadObject(Stream stream, out int bytesRead)
    {
        Guid guid;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
#else
        Span<byte> span = stackalloc byte[16];
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        int ct = stream.Read(span, 0, 16);
#else
        int ct = stream.Read(span);
#endif
        if (ct != 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(GuidParser))) { ErrorCode = 2 };

        bytesRead = 16;
        guid = new Guid(span);
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        return guid;
    }
}
