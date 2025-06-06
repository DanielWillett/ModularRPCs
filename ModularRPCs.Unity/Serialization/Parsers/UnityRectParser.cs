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
public class UnityRectParser : BinaryTypeParser<Rect>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Rect value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityRectParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes,      value.x);
            Unsafe.WriteUnaligned(bytes +  4, value.y);
            Unsafe.WriteUnaligned(bytes +  8, value.width);
            Unsafe.WriteUnaligned(bytes + 12, value.height);
        }
        else
        {
            float x = value.x, y = value.y, w = value.width, h = value.height;
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref x)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref y)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref w)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref h)));
        }

        return 16;
    }
    public override int WriteObject(Rect value, Stream stream)
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
            Unsafe.WriteUnaligned(ref span[ 0], value.x);
            Unsafe.WriteUnaligned(ref span[ 4], value.y);
            Unsafe.WriteUnaligned(ref span[ 8], value.width);
            Unsafe.WriteUnaligned(ref span[12], value.height);
        }
        else
        {
            float x = value.x, y = value.y, w = value.width, h = value.height;
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref x)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref y)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref w)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref h)));
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
    public override unsafe Rect ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityRectParser))) { ErrorCode = 1 };

        Rect v4 = default;
        if (BitConverter.IsLittleEndian)
        {
            v4.x = Unsafe.ReadUnaligned<float>(bytes);
            v4.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            v4.width = Unsafe.ReadUnaligned<float>(bytes + 8);
            v4.height = Unsafe.ReadUnaligned<float>(bytes + 12);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v4.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v4.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            v4.width = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            v4.height = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 16;
        return v4;
    }
    public override Rect ReadObject(Stream stream, out int bytesRead)
    {
        Rect v4 = default;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityRectParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v4.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            v4.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            v4.width = Unsafe.ReadUnaligned<float>(ref span[8]);
            v4.height = Unsafe.ReadUnaligned<float>(ref span[12]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v4.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v4.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            v4.width = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            v4.height = Unsafe.As<int, float>(ref read);
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
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Rect>
    {
        protected override Rect FlipBits(Rect toFlip)
        {
            float x = toFlip.x, y = toFlip.y, w = toFlip.width, h = toFlip.height;
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref y));
            toFlip.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref w));
            toFlip.width = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref h));
            toFlip.height = Unsafe.As<int, float>(ref read);
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
        private static void WriteToBufferIntl(byte* ptr, Rect value)
        {
            *(float*)ptr = value.x;
            *(float*)(ptr + 4) = value.y;
            *(float*)(ptr + 8) = value.width;
            *(float*)(ptr + 12) = value.height;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Rect value)
        {
            Unsafe.WriteUnaligned(ptr, value.x);
            Unsafe.WriteUnaligned(ptr + 4, value.y);
            Unsafe.WriteUnaligned(ptr + 8, value.width);
            Unsafe.WriteUnaligned(ptr + 12, value.height);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Rect value)
        {
            float x = value.x, y = value.y, w = value.width, h = value.height;
            MemoryMarshal.Write(span, ref x);
            MemoryMarshal.Write(span.Slice(4), ref y);
            MemoryMarshal.Write(span.Slice(8), ref w);
            MemoryMarshal.Write(span.Slice(12), ref h);
        }
        private static Rect ReadFromBufferIntl(byte* ptr)
        {
            Rect v4 = default;
            v4.x = *(float*)ptr;
            v4.y = *(float*)(ptr + 4);
            v4.width = *(float*)(ptr + 8);
            v4.height = *(float*)(ptr + 12);
            return v4;
        }
        private static Rect ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Rect v4 = default;
            v4.x = Unsafe.ReadUnaligned<float>(ptr);
            v4.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            v4.width = Unsafe.ReadUnaligned<float>(ptr + 8);
            v4.height = Unsafe.ReadUnaligned<float>(ptr + 12);
            return v4;
        }
        private static Rect ReadFromBufferSpanIntl(Span<byte> span)
        {
            Rect v4 = default;
            v4.x = MemoryMarshal.Read<float>(span);
            v4.y = MemoryMarshal.Read<float>(span.Slice(4));
            v4.width = MemoryMarshal.Read<float>(span.Slice(8));
            v4.height = MemoryMarshal.Read<float>(span.Slice(12));
            return v4;
        }
    }
}