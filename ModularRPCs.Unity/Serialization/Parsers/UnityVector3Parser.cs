using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityVector3Parser : BinaryTypeParser<Vector3>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 12;
    public override unsafe int WriteObject(Vector3 value, byte* bytes, uint maxSize)
    {
        if (maxSize < 12)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityVector3Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes,     value.x);
            Unsafe.WriteUnaligned(bytes + 4, value.y);
            Unsafe.WriteUnaligned(bytes + 8, value.z);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,     BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(bytes + 4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
            Unsafe.WriteUnaligned(bytes + 8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.z)));
        }

        return 12;
    }
    public override unsafe int WriteObject(Vector3 value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
#else
        Span<byte> span = stackalloc byte[12];
#endif
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value.x);
            Unsafe.WriteUnaligned(ref span[4], value.y);
            Unsafe.WriteUnaligned(ref span[8], value.z);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
            Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.z)));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 12);
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
        return 12;
    }
    public override unsafe Vector3 ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 12)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityVector3Parser))) { ErrorCode = 1 };

        Vector3 v3 = default;
        if (BitConverter.IsLittleEndian)
        {
            v3.x = Unsafe.ReadUnaligned<float>(bytes);
            v3.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            v3.z = Unsafe.ReadUnaligned<float>(bytes + 8);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v3.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v3.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            v3.z = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 12;
        return v3;
    }
    public override unsafe Vector3 ReadObject(Stream stream, out int bytesRead)
    {
        Vector3 v3 = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
            int ct = stream.Read(span, 0, 12);
#else
        Span<byte> span = stackalloc byte[12];
        int ct = stream.Read(span);
#endif
            
        bytesRead = ct;
        if (ct != 12)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityVector3Parser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v3.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            v3.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            v3.z = Unsafe.ReadUnaligned<float>(ref span[8]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v3.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v3.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            v3.z = Unsafe.As<int, float>(ref read);
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return v3;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Vector3>
    {
        protected override Vector3 FlipBits(Vector3 toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.y));
            toFlip.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.z));
            toFlip.z = Unsafe.As<int, float>(ref read);
            return toFlip;
        }

        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            bytes += hdrSize;
            byte* end = bytes + size;
            const int elementSize = 12;
            while (bytes < end)
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
                Unsafe.WriteUnaligned(bytes, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
                Unsafe.WriteUnaligned(bytes + 4, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
                Unsafe.WriteUnaligned(bytes + 8, read);

                bytes += elementSize;
            }
        }

        protected override void FlipBits(byte[] bytes, int index, int size)
        {
            const int elementSize = 12;
            for (; index < size; index += elementSize)
            {
                ref byte pos = ref bytes[index];
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));
            }
        }

        public Many(SerializationConfiguration config) : base(config, 12, sizeof(float), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Vector3 value)
        {
            *(float*)ptr = value.x;
            *(float*)(ptr + 4) = value.y;
            *(float*)(ptr + 8) = value.z;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Vector3 value)
        {
            Unsafe.WriteUnaligned(ptr, value.x);
            Unsafe.WriteUnaligned(ptr + 4, value.y);
            Unsafe.WriteUnaligned(ptr + 8, value.z);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Vector3 value)
        {
            MemoryMarshal.Write(span, ref value.x);
            MemoryMarshal.Write(span.Slice(4), ref value.y);
            MemoryMarshal.Write(span.Slice(8), ref value.z);
        }
        private static Vector3 ReadFromBufferIntl(byte* ptr)
        {
            Vector3 v3 = default;
            v3.x = *(float*)ptr;
            v3.y = *(float*)(ptr + 4);
            v3.z = *(float*)(ptr + 8);
            return v3;
        }
        private static Vector3 ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Vector3 v3 = default;
            v3.x = Unsafe.ReadUnaligned<float>(ptr);
            v3.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            v3.z = Unsafe.ReadUnaligned<float>(ptr + 8);
            return v3;
        }
        private static Vector3 ReadFromBufferSpanIntl(Span<byte> span)
        {
            Vector3 v3 = default;
            v3.x = MemoryMarshal.Read<float>(span);
            v3.y = MemoryMarshal.Read<float>(span.Slice(4));
            v3.z = MemoryMarshal.Read<float>(span.Slice(8));
            return v3;
        }
    }
}