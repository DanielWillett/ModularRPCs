using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityBoundsParser : BinaryTypeParser<Bounds>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 24;

    public override unsafe int WriteObject(Bounds value, byte* bytes, uint maxSize)
    {
        if (maxSize < 24)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 1 };

        Vector3 center = value.center;
        Vector3 extents = value.extents;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, center.x);
            Unsafe.WriteUnaligned(bytes + 4, center.y);
            Unsafe.WriteUnaligned(bytes + 8, center.z);
            Unsafe.WriteUnaligned(bytes + 12, extents.x);
            Unsafe.WriteUnaligned(bytes + 16, extents.y);
            Unsafe.WriteUnaligned(bytes + 20, extents.z);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.x)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.y)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.z)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.x)));
            Unsafe.WriteUnaligned(bytes + 16, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.y)));
            Unsafe.WriteUnaligned(bytes + 20, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.z)));
        }

        return 24;
    }

    public override int WriteObject(Bounds value, Stream stream)
    {
        Vector3 center = value.center;
        Vector3 extents = value.extents;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ref span[0], center.x);
                Unsafe.WriteUnaligned(ref span[4], center.y);
                Unsafe.WriteUnaligned(ref span[8], center.z);
                stream.Write(span, 0, 12);
                Unsafe.WriteUnaligned(ref span[0], extents.x);
                Unsafe.WriteUnaligned(ref span[4], extents.y);
                Unsafe.WriteUnaligned(ref span[8], extents.z);
                stream.Write(span, 0, 12);
            }
            else
            {
                Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.x)));
                Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.y)));
                Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.z)));
                stream.Write(span, 0, 12);
                Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.x)));
                Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.y)));
                Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.z)));
                stream.Write(span, 0, 12);
            }
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#else
        Span<byte> span = stackalloc byte[24];
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], center.x);
            Unsafe.WriteUnaligned(ref span[4], center.y);
            Unsafe.WriteUnaligned(ref span[8], center.z);
            Unsafe.WriteUnaligned(ref span[12], extents.x);
            Unsafe.WriteUnaligned(ref span[16], extents.y);
            Unsafe.WriteUnaligned(ref span[20], extents.z);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.x)));
            Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.y)));
            Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref center.z)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.x)));
            Unsafe.WriteUnaligned(ref span[16], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.y)));
            Unsafe.WriteUnaligned(ref span[20], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref extents.z)));
        }
        stream.Write(span);
#endif
        return 24;
    }
    public override unsafe Bounds ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 24)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 1 };

        Bounds bounds = default;
        Vector3 center = default;
        Vector3 extents = default;
        if (BitConverter.IsLittleEndian)
        {
            center.x = Unsafe.ReadUnaligned<float>(bytes);
            center.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            center.z = Unsafe.ReadUnaligned<float>(bytes + 8);
            extents.x = Unsafe.ReadUnaligned<float>(bytes + 12);
            extents.y = Unsafe.ReadUnaligned<float>(bytes + 16);
            extents.z = Unsafe.ReadUnaligned<float>(bytes + 20);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            center.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            center.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            center.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            extents.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 16));
            extents.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 20));
            extents.z = Unsafe.As<int, float>(ref read);
        }

        bounds.center = center;
        bounds.extents = extents;

        bytesRead = 24;
        return bounds;
    }
    public override Bounds ReadObject(Stream stream, out int bytesRead)
    {
        Bounds bounds = default;
        Vector3 center = default;
        Vector3 extents = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        bytesRead = 0;
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
            int ct = stream.Read(span, 0, 12);
            bytesRead += ct;
            if (ct != 12)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 2 };

            if (BitConverter.IsLittleEndian)
            {
                center.x = Unsafe.ReadUnaligned<float>(ref span[0]);
                center.y = Unsafe.ReadUnaligned<float>(ref span[4]);
                center.z = Unsafe.ReadUnaligned<float>(ref span[8]);

                ct = stream.Read(span, 0, 12);
                bytesRead += ct;
                if (ct != 12)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 2 };

                extents.x = Unsafe.ReadUnaligned<float>(ref span[0]);
                extents.y = Unsafe.ReadUnaligned<float>(ref span[4]);
                extents.z = Unsafe.ReadUnaligned<float>(ref span[8]);
            }
            else
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                center.x = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                center.y = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                center.z = Unsafe.As<int, float>(ref read);

                ct = stream.Read(span, 0, 12);
                bytesRead += ct;
                if (ct != 12)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 2 };

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                extents.x = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                extents.y = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                extents.z = Unsafe.As<int, float>(ref read);
            }
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#else
        Span<byte> span = stackalloc byte[24];
        int ct = stream.Read(span);
        bytesRead = ct;
        if (ct != 24)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityBoundsParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            center.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            center.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            center.z = Unsafe.ReadUnaligned<float>(ref span[8]);
            extents.x = Unsafe.ReadUnaligned<float>(ref span[12]);
            extents.y = Unsafe.ReadUnaligned<float>(ref span[16]);
            extents.z = Unsafe.ReadUnaligned<float>(ref span[20]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            center.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            center.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            center.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            extents.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[16]));
            extents.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[20]));
            extents.z = Unsafe.As<int, float>(ref read);
        }
#endif

        bounds.center = center;
        bounds.extents = extents;

        return bounds;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Bounds>
    {
        protected override Bounds FlipBits(Bounds toFlip)
        {
            Vector3 v3 = toFlip.center;
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.x));
            v3.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.y));
            v3.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.z));
            v3.z = Unsafe.As<int, float>(ref read);
            toFlip.center = v3;

            v3 = toFlip.extents;
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.x));
            v3.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.y));
            v3.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref v3.z));
            v3.z = Unsafe.As<int, float>(ref read);
            toFlip.extents = v3;

            return toFlip;
        }

        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            bytes += hdrSize;
            byte* end = bytes + size;
            const int elementSize = 24;
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

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 16));
                Unsafe.WriteUnaligned(bytes + 16, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 20));
                Unsafe.WriteUnaligned(bytes + 20, read);

                bytes += elementSize;
            }
        }

        protected override void FlipBits(byte[] bytes, int index, int size)
        {
            const int elementSize = 24;
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

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));
            }
        }

        public Many(SerializationConfiguration config) : base(config, 24, sizeof(float), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Bounds value)
        {
            Vector3 v3 = value.center;
            *(float*)ptr = v3.x;
            *(float*)(ptr + 4)  = v3.y;
            *(float*)(ptr + 8)  = v3.z;

            v3 = value.extents;
            *(float*)(ptr + 12) = v3.x;
            *(float*)(ptr + 16) = v3.y;
            *(float*)(ptr + 20) = v3.z;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Bounds value)
        {
            Vector3 v3 = value.center;
            Unsafe.WriteUnaligned(ptr, v3.x);
            Unsafe.WriteUnaligned(ptr + 4, v3.y);
            Unsafe.WriteUnaligned(ptr + 8, v3.z);

            v3 = value.extents;
            Unsafe.WriteUnaligned(ptr + 12, v3.x);
            Unsafe.WriteUnaligned(ptr + 16, v3.y);
            Unsafe.WriteUnaligned(ptr + 20, v3.z);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Bounds value)
        {
            Vector3 v3 = value.center;
            MemoryMarshal.Write(span, ref v3.x);
            MemoryMarshal.Write(span.Slice(4), ref v3.y);
            MemoryMarshal.Write(span.Slice(8), ref v3.z);

            v3 = value.extents;
            MemoryMarshal.Write(span.Slice(12), ref v3.x);
            MemoryMarshal.Write(span.Slice(16), ref v3.y);
            MemoryMarshal.Write(span.Slice(20), ref v3.z);
        }
        private static Bounds ReadFromBufferIntl(byte* ptr)
        {
            Bounds bounds = default;
            Vector3 v3 = default;
            v3.x = *(float*)ptr;
            v3.y = *(float*)(ptr + 4);
            v3.z = *(float*)(ptr + 8);
            bounds.center = v3;
            v3.x = *(float*)(ptr + 12);
            v3.y = *(float*)(ptr + 16);
            v3.z = *(float*)(ptr + 20);
            bounds.extents = v3;
            return bounds;
        }
        private static Bounds ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Bounds bounds = default;
            Vector3 v3 = default;
            v3.x = Unsafe.ReadUnaligned<float>(ptr);
            v3.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            v3.z = Unsafe.ReadUnaligned<float>(ptr + 8);
            bounds.center = v3;
            v3.x = Unsafe.ReadUnaligned<float>(ptr + 12);
            v3.y = Unsafe.ReadUnaligned<float>(ptr + 16);
            v3.z = Unsafe.ReadUnaligned<float>(ptr + 20);
            bounds.extents = v3;
            return bounds;
        }
        private static Bounds ReadFromBufferSpanIntl(Span<byte> span)
        {
            Bounds bounds = default;
            Vector3 v3 = default;
            v3.x = MemoryMarshal.Read<float>(span);
            v3.y = MemoryMarshal.Read<float>(span.Slice(4));
            v3.z = MemoryMarshal.Read<float>(span.Slice(8));
            bounds.center = v3;
            v3.x = MemoryMarshal.Read<float>(span.Slice(12));
            v3.y = MemoryMarshal.Read<float>(span.Slice(16));
            v3.z = MemoryMarshal.Read<float>(span.Slice(20));
            bounds.extents = v3;
            return bounds;
        }
    }
}