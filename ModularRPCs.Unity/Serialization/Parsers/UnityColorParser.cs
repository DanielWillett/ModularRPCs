extern alias Unity;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity::UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityColorParser : BinaryTypeParser<Color>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Color value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityColorParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value.a);
            Unsafe.WriteUnaligned(bytes + 4, value.r);
            Unsafe.WriteUnaligned(bytes + 8, value.g);
            Unsafe.WriteUnaligned(bytes + 12, value.b);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.a)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.r)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.g)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.b)));
        }

        return 16;
    }
    public override int WriteObject(Color value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
#else
        Span<byte> span = stackalloc byte[16];
#endif
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value.a);
            Unsafe.WriteUnaligned(ref span[4], value.r);
            Unsafe.WriteUnaligned(ref span[8], value.g);
            Unsafe.WriteUnaligned(ref span[12], value.b);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.a)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.r)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.g)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.b)));
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
    public override unsafe Color ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityColorParser))) { ErrorCode = 1 };

        Color v4 = default;
        if (BitConverter.IsLittleEndian)
        {
            v4.a = Unsafe.ReadUnaligned<float>(bytes);
            v4.r = Unsafe.ReadUnaligned<float>(bytes + 4);
            v4.g = Unsafe.ReadUnaligned<float>(bytes + 8);
            v4.b = Unsafe.ReadUnaligned<float>(bytes + 12);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v4.a = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v4.r = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            v4.g = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            v4.b = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 16;
        return v4;
    }
    public override Color ReadObject(Stream stream, out int bytesRead)
    {
        Color v4 = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            int ct = stream.Read(span, 0, 16);
#else
        Span<byte> span = stackalloc byte[16];
        int ct = stream.Read(span);
#endif
            
        bytesRead = ct;
        if (ct != 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityColorParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v4.a = Unsafe.ReadUnaligned<float>(ref span[0]);
            v4.r = Unsafe.ReadUnaligned<float>(ref span[4]);
            v4.g = Unsafe.ReadUnaligned<float>(ref span[8]);
            v4.b = Unsafe.ReadUnaligned<float>(ref span[12]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v4.a = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v4.r = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            v4.g = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            v4.b = Unsafe.As<int, float>(ref read);
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return v4;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Color>
    {
        protected override Color FlipBits(Color toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.a));
            toFlip.a = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.r));
            toFlip.r = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.g));
            toFlip.g = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.b));
            toFlip.b = Unsafe.As<int, float>(ref read);
            return toFlip;
        }

        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            bytes += hdrSize;
            byte* end = bytes + size;
            const int elementSize = 16;
            while (bytes < end)
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
                Unsafe.WriteUnaligned(bytes, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
                Unsafe.WriteUnaligned(bytes + 4, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
                Unsafe.WriteUnaligned(bytes + 8, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
                Unsafe.WriteUnaligned(bytes + 12, read);

                bytes += elementSize;
            }
        }

        protected override void FlipBits(byte[] bytes, int index, int size)
        {
            const int elementSize = 16;
            for (; index < size; index += elementSize)
            {
                ref byte pos = ref bytes[index];
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));
            }
        }
        public Many(SerializationConfiguration config) : base(config, 16, sizeof(float), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Color value)
        {
            *(float*)ptr = value.a;
            *(float*)(ptr + 4) = value.r;
            *(float*)(ptr + 8) = value.g;
            *(float*)(ptr + 12) = value.b;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Color value)
        {
            Unsafe.WriteUnaligned(ptr, value.a);
            Unsafe.WriteUnaligned(ptr + 4, value.r);
            Unsafe.WriteUnaligned(ptr + 8, value.g);
            Unsafe.WriteUnaligned(ptr + 12, value.b);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Color value)
        {
            MemoryMarshal.Write(span, ref value.a);
            MemoryMarshal.Write(span.Slice(4), ref value.r);
            MemoryMarshal.Write(span.Slice(8), ref value.g);
            MemoryMarshal.Write(span.Slice(12), ref value.b);
        }
        private static Color ReadFromBufferIntl(byte* ptr)
        {
            Color v4 = default;
            v4.a = *(float*)ptr;
            v4.r = *(float*)(ptr + 4);
            v4.g = *(float*)(ptr + 8);
            v4.b = *(float*)(ptr + 12);
            return v4;
        }
        private static Color ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Color v4 = default;
            v4.a = Unsafe.ReadUnaligned<float>(ptr);
            v4.r = Unsafe.ReadUnaligned<float>(ptr + 4);
            v4.g = Unsafe.ReadUnaligned<float>(ptr + 8);
            v4.b = Unsafe.ReadUnaligned<float>(ptr + 12);
            return v4;
        }
        private static Color ReadFromBufferSpanIntl(Span<byte> span)
        {
            Color v4 = default;
            v4.a = MemoryMarshal.Read<float>(span);
            v4.r = MemoryMarshal.Read<float>(span.Slice(4));
            v4.g = MemoryMarshal.Read<float>(span.Slice(8));
            v4.b = MemoryMarshal.Read<float>(span.Slice(12));
            return v4;
        }
    }
}