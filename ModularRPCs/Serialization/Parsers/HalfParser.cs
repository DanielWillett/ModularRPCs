#if NET5_0_OR_GREATER
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class HalfParser : BinaryTypeParser<Half>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 2;
    public override unsafe int WriteObject(Half value, byte* bytes, uint maxSize)
    {
        if (maxSize < 2)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(HalfParser))) { ErrorCode = 1 };

        ushort packed = *(ushort*)&value;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, packed);
        }
        else
        {
            bytes[1] = unchecked((byte)packed);
            *bytes = unchecked((byte)(packed >>> 8));
        }

        return 2;
    }
    public override unsafe int WriteObject(Half value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(2);
        try
        {
#else
        Span<byte> span = stackalloc byte[2];
#endif

        ushort packed = *(ushort*)&value;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], packed);
        }
        else
        {
            span[1] = unchecked((byte)packed);
            span[0] = unchecked((byte)(packed >>> 8));
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
    public override unsafe Half ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 2)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(HalfParser))) { ErrorCode = 1 };

        short value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<short>(bytes)
            : unchecked( (short)(*bytes << 8 | bytes[1]) );

        bytesRead = 2;
        return *(Half*)&value;
    }
    public override unsafe Half ReadObject(Stream stream, out int bytesRead)
    {
        short value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(2);
        try
        {
            int ct = stream.Read(span, 0, 2);
#else
        Span<byte> span = stackalloc byte[2];
        int ct = stream.Read(span);
#endif

        bytesRead = ct;
        if (ct != 2)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(HalfParser))) { ErrorCode = 2 };

        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<short>(ref span[0])
            : unchecked( (short)(span[0] << 8 | span[1]) );
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return *(Half*)&value;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Half>
    {
        public Many(SerializationConfiguration config) : base(config, sizeof(ushort), sizeof(ushort), true,
            &WriteToBufferIntl,
            &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl,
            &ReadFromBufferIntl,
            &ReadFromBufferUnalignedIntl,
            &ReadFromBufferSpanIntl)
        {

        }

        protected override Half FlipBits(Half toFlip)
        {
            ushort flip = *(ushort*)&toFlip;
            flip = BinaryPrimitives.ReverseEndianness(flip);
            return *(Half*)flip;
        }
        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            Unsafe.WriteUnaligned(bytes, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(bytes)));
        }
        protected override void FlipBits(byte[] bytes, int hdrSize, int size)
        {
            Unsafe.WriteUnaligned(ref bytes[0], BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref bytes[0])));
        }
        private static void WriteToBufferIntl(byte* ptr, Half v)
        {
            *(ushort*)ptr = *(ushort*)&v;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Half v)
        {
            Unsafe.WriteUnaligned(ptr, *(ushort*)&v);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Half v)
        {
            ushort v16 = *(ushort*)&v;
            MemoryMarshal.Write(span, ref v16);
        }
        private static Half ReadFromBufferIntl(byte* ptr)
        {
            ushort packed = *(ushort*)ptr;
            return *(Half*)&packed;
        }
        private static Half ReadFromBufferUnalignedIntl(byte* ptr)
        {
            ushort packed = Unsafe.ReadUnaligned<ushort>(ptr);
            return *(Half*)&packed;
        }
        private static Half ReadFromBufferSpanIntl(Span<byte> span)
        {
            ushort packed = MemoryMarshal.Read<ushort>(span);
            return *(Half*)&packed;
        }
    }
}
#endif