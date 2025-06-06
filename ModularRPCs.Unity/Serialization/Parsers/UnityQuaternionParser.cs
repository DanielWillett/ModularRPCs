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
public class UnityQuaternionParser : BinaryTypeParser<Quaternion>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Quaternion value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityQuaternionParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value.x);
            Unsafe.WriteUnaligned(bytes + 4, value.y);
            Unsafe.WriteUnaligned(bytes + 8, value.z);
            Unsafe.WriteUnaligned(bytes + 12, value.w);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.z)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.w)));
        }

        return 16;
    }
    public override unsafe int WriteObject(Quaternion value, Stream stream)
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
            Unsafe.WriteUnaligned(ref span[0], value.x);
            Unsafe.WriteUnaligned(ref span[4], value.y);
            Unsafe.WriteUnaligned(ref span[8], value.z);
            Unsafe.WriteUnaligned(ref span[12], value.w);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.z)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.w)));
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
    public override unsafe Quaternion ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityQuaternionParser))) { ErrorCode = 1 };

        Quaternion v4 = default;
        if (BitConverter.IsLittleEndian)
        {
            v4.x = Unsafe.ReadUnaligned<float>(bytes);
            v4.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            v4.z = Unsafe.ReadUnaligned<float>(bytes + 8);
            v4.w = Unsafe.ReadUnaligned<float>(bytes + 12);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v4.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v4.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            v4.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            v4.w = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 16;
        return v4;
    }
    public override Quaternion ReadObject(Stream stream, out int bytesRead)
    {
        Quaternion v4 = default;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityQuaternionParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v4.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            v4.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            v4.z = Unsafe.ReadUnaligned<float>(ref span[8]);
            v4.w = Unsafe.ReadUnaligned<float>(ref span[12]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v4.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v4.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            v4.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            v4.w = Unsafe.As<int, float>(ref read);
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
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Quaternion>
    {
        protected override Quaternion FlipBits(Quaternion toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.y));
            toFlip.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.z));
            toFlip.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.w));
            toFlip.w = Unsafe.As<int, float>(ref read);
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
        private static void WriteToBufferIntl(byte* ptr, Quaternion value)
        {
            *(float*)ptr = value.x;
            *(float*)(ptr + 4) = value.y;
            *(float*)(ptr + 8) = value.z;
            *(float*)(ptr + 12) = value.w;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Quaternion value)
        {
            Unsafe.WriteUnaligned(ptr, value.x);
            Unsafe.WriteUnaligned(ptr + 4, value.y);
            Unsafe.WriteUnaligned(ptr + 8, value.z);
            Unsafe.WriteUnaligned(ptr + 12, value.w);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Quaternion value)
        {
            MemoryMarshal.Write(span, ref value.x);
            MemoryMarshal.Write(span.Slice(4), ref value.y);
            MemoryMarshal.Write(span.Slice(8), ref value.z);
            MemoryMarshal.Write(span.Slice(12), ref value.w);
        }
        private static Quaternion ReadFromBufferIntl(byte* ptr)
        {
            Quaternion v4 = default;
            v4.x = *(float*)ptr;
            v4.y = *(float*)(ptr + 4);
            v4.z = *(float*)(ptr + 8);
            v4.w = *(float*)(ptr + 12);
            return v4;
        }
        private static Quaternion ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Quaternion v4 = default;
            v4.x = Unsafe.ReadUnaligned<float>(ptr);
            v4.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            v4.z = Unsafe.ReadUnaligned<float>(ptr + 8);
            v4.w = Unsafe.ReadUnaligned<float>(ptr + 12);
            return v4;
        }
        private static Quaternion ReadFromBufferSpanIntl(Span<byte> span)
        {
            Quaternion v4 = default;
            v4.x = MemoryMarshal.Read<float>(span);
            v4.y = MemoryMarshal.Read<float>(span.Slice(4));
            v4.z = MemoryMarshal.Read<float>(span.Slice(8));
            v4.w = MemoryMarshal.Read<float>(span.Slice(12));
            return v4;
        }
    }
}