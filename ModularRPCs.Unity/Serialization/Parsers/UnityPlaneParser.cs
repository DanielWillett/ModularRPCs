using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityPlaneParser : BinaryTypeParser<Plane>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Plane value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityPlaneParser))) { ErrorCode = 1 };

        Vector3 normal = value.normal;
        float distance = value.distance;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, distance);
            Unsafe.WriteUnaligned(bytes +  4, normal.x);
            Unsafe.WriteUnaligned(bytes +  8, normal.y);
            Unsafe.WriteUnaligned(bytes + 12, normal.z);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref distance)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.x)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.y)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.z)));
        }

        return 16;
    }
    public override unsafe int WriteObject(Plane value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
#else
        Span<byte> span = stackalloc byte[16];
#endif
        Vector3 normal = value.normal;
        float distance = value.distance;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[ 0], distance);
            Unsafe.WriteUnaligned(ref span[ 4], normal.x);
            Unsafe.WriteUnaligned(ref span[ 8], normal.y);
            Unsafe.WriteUnaligned(ref span[12], normal.z);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref distance)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.x)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.y)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.z)));
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
    public override unsafe Plane ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityPlaneParser))) { ErrorCode = 1 };

        Plane v4 = default;
        Vector3 normal = default;
        if (BitConverter.IsLittleEndian)
        {
            v4.distance = Unsafe.ReadUnaligned<float>(bytes);
            normal.x = Unsafe.ReadUnaligned<float>(bytes + 4);
            normal.y = Unsafe.ReadUnaligned<float>(bytes + 8);
            normal.z = Unsafe.ReadUnaligned<float>(bytes + 12);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v4.distance = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            normal.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            normal.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            normal.z = Unsafe.As<int, float>(ref read);
        }

        v4.normal = normal;

        bytesRead = 16;
        return v4;
    }
    public override Plane ReadObject(Stream stream, out int bytesRead)
    {
        Plane v4 = default;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityPlaneParser))) { ErrorCode = 2 };

        Vector3 normal = default;
        if (BitConverter.IsLittleEndian)
        {
            v4.distance = Unsafe.ReadUnaligned<float>(ref span[0]);
            normal.x = Unsafe.ReadUnaligned<float>(ref span[4]);
            normal.y = Unsafe.ReadUnaligned<float>(ref span[8]);
            normal.z = Unsafe.ReadUnaligned<float>(ref span[12]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v4.distance = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            normal.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            normal.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            normal.z = Unsafe.As<int, float>(ref read);
        }

        v4.normal = normal;

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return v4;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Plane>
    {
        protected override Plane FlipBits(Plane toFlip)
        {
            float distance = toFlip.distance;

            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref distance));
            toFlip.distance = Unsafe.As<int, float>(ref read);

            Vector3 normal = toFlip.normal;
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.x));
            normal.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.y));
            normal.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref normal.z));
            normal.z = Unsafe.As<int, float>(ref read);

            toFlip.normal = normal;
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
        private static void WriteToBufferIntl(byte* ptr, Plane value)
        {
            *(float*)ptr = value.distance;
            Vector3 normal = value.normal;
            *(float*)(ptr + 4) = normal.x;
            *(float*)(ptr + 8) = normal.y;
            *(float*)(ptr + 12) = normal.z;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Plane value)
        {
            Unsafe.WriteUnaligned(ptr, value.distance);
            Vector3 normal = value.normal;
            Unsafe.WriteUnaligned(ptr + 4, normal.x);
            Unsafe.WriteUnaligned(ptr + 8, normal.y);
            Unsafe.WriteUnaligned(ptr + 12, normal.z);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Plane value)
        {
            float distance = value.distance;
            MemoryMarshal.Write(span, ref distance);
            Vector3 normal = value.normal;
            MemoryMarshal.Write(span.Slice(4), ref normal.x);
            MemoryMarshal.Write(span.Slice(8), ref normal.y);
            MemoryMarshal.Write(span.Slice(12), ref normal.z);
        }
        private static Plane ReadFromBufferIntl(byte* ptr)
        {
            Plane v4 = default;
            v4.distance = *(float*)ptr;
            Vector3 normal = default;
            normal.x = *(float*)(ptr + 4);
            normal.y = *(float*)(ptr + 8);
            normal.z = *(float*)(ptr + 12);
            v4.normal = normal;
            return v4;
        }
        private static Plane ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Plane v4 = default;
            v4.distance = Unsafe.ReadUnaligned<float>(ptr);
            Vector3 normal = default;
            normal.x = Unsafe.ReadUnaligned<float>(ptr + 4);
            normal.y = Unsafe.ReadUnaligned<float>(ptr + 8);
            normal.z = Unsafe.ReadUnaligned<float>(ptr + 12);
            v4.normal = normal;
            return v4;
        }
        private static Plane ReadFromBufferSpanIntl(Span<byte> span)
        {
            Plane v4 = default;
            v4.distance = MemoryMarshal.Read<float>(span);
            Vector3 normal = default;
            normal.x = MemoryMarshal.Read<float>(span.Slice(4));
            normal.y = MemoryMarshal.Read<float>(span.Slice(8));
            normal.z = MemoryMarshal.Read<float>(span.Slice(12));
            v4.normal = normal;
            return v4;
        }
    }
}