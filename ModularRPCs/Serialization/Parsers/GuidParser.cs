using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        Guid guid = ReadGuidFromBytes(bytes);
#endif
        bytesRead = 16;
        return guid;
    }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
    private static unsafe Guid ReadGuidFromBytes(byte* bytes)
    {
        // rent until we get an array with the exact length
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            if (span.Length == 16)
            {
                Unsafe.CopyBlockUnaligned(ref span[0], ref Unsafe.AsRef<byte>(bytes), 16u);
                return new Guid(span);
            }
            else
            {
                return ReadGuidFromBytes(bytes);
            }
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
    }
#endif
    public override Guid ReadObject(Stream stream, out int bytesRead)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        Guid guid = ReadGuidFromStream(stream);
#else
        Span<byte> span = stackalloc byte[16];
        int ct = stream.Read(span);
        if (ct != 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(GuidParser))) { ErrorCode = 2 };

        Guid guid = new Guid(span);
#endif

        bytesRead = 16;
        return guid;
    }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
    private static Guid ReadGuidFromStream(Stream stream)
    {
        // rent until we get an array with the exact length
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            if (span.Length == 16)
            {
                int ct = stream.Read(span, 0, 16);
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(GuidParser))) { ErrorCode = 2 };

                return new Guid(span);
            }
            else
            {
                return ReadGuidFromStream(stream);
            }
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
    }
#endif

    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Guid>
    {
        public Many(SerializationConfiguration config) : base(config, 16,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            1
#else
            16
#endif
            , false, &WriteToBufferIntl,

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            &WriteToBufferIntl,
#else
            &WriteToBufferUnalignedIntl,
#endif
            &WriteToBufferSpanIntl, &ReadFromBufferIntl,

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            &ReadFromBufferIntl,
#else
            &ReadFromBufferUnalignedIntl,
#endif
            &ReadFromBufferSpanIntl) { }

        private static void WriteToBufferIntl(byte* ptr, Guid guid)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            guid.TryWriteBytes(new Span<byte>(ptr, 16));
#else
            if (BitConverter.IsLittleEndian)
            {
                *(Guid*)ptr = guid;
                return;
            }
            byte[] bytes = guid.ToByteArray();
            bytes.AsSpan(0, 16).CopyTo(new Span<byte>(ptr, 16));
#endif
        }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private static void WriteToBufferUnalignedIntl(byte* ptr, Guid guid)
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ptr, guid);
                return;
            }
            byte[] bytes = guid.ToByteArray();
            bytes.AsSpan(0, 16).CopyTo(new Span<byte>(ptr, 16));
        }
#endif
        private static void WriteToBufferSpanIntl(Span<byte> span, Guid guid)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            guid.TryWriteBytes(span);
#else
            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.Write(span, ref guid);
                return;
            }
            byte[] bytes = guid.ToByteArray();
            bytes.AsSpan(0, 16).CopyTo(span);
#endif
        }
        private static Guid ReadFromBufferIntl(byte* ptr)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            return new Guid(new ReadOnlySpan<byte>(ptr, 16));
#else
            if (BitConverter.IsLittleEndian)
                return *(Guid*)ptr;

            byte[] bytes = DefaultSerializer.ArrayPool.Rent(16);
            if (bytes.Length != 16)
            {
                DefaultSerializer.ArrayPool.Return(bytes);
                bytes = new byte[16];
                new Span<byte>(ptr, 16).CopyTo(bytes);
                return new Guid(bytes);
            }

            try
            {
                new Span<byte>(ptr, 16).CopyTo(bytes);
                return new Guid(bytes);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(bytes);
            }
#endif
        }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private static Guid ReadFromBufferUnalignedIntl(byte* ptr)
        {
            if (BitConverter.IsLittleEndian)
                return Unsafe.ReadUnaligned<Guid>(ptr);

            byte[] bytes = DefaultSerializer.ArrayPool.Rent(16);
            if (bytes.Length != 16)
            {
                DefaultSerializer.ArrayPool.Return(bytes);
                bytes = new byte[16];
                new Span<byte>(ptr, 16).CopyTo(bytes);
                return new Guid(bytes);
            }

            try
            {
                new Span<byte>(ptr, 16).CopyTo(bytes);
                return new Guid(bytes);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(bytes);
            }
        }
#endif
        private static Guid ReadFromBufferSpanIntl(Span<byte> span)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            return new Guid(span[..16]);
#else
            if (BitConverter.IsLittleEndian)
                return MemoryMarshal.Read<Guid>(span);

            byte[] bytes = DefaultSerializer.ArrayPool.Rent(16);
            if (bytes.Length != 16)
            {
                DefaultSerializer.ArrayPool.Return(bytes);
                bytes = new byte[16];
                span.Slice(0, 16).CopyTo(bytes);
                return new Guid(bytes);
            }

            try
            {
                span.Slice(0, 16).CopyTo(bytes);
                return new Guid(bytes);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(bytes);
            }
#endif
        }
    }
}
