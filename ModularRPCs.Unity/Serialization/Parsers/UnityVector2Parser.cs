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
public class UnityVector2Parser : BinaryTypeParser<Vector2>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(Vector2 value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityVector2Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes,     value.x);
            Unsafe.WriteUnaligned(bytes + 4, value.y);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,     BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(bytes + 4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
        }

        return 8;
    }
    public override int WriteObject(Vector2 value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
#else
        Span<byte> span = stackalloc byte[8];
#endif
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value.x);
            Unsafe.WriteUnaligned(ref span[4], value.y);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.x)));
            Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.y)));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 8);
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
        return 8;
    }
    public override unsafe Vector2 ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityVector2Parser))) { ErrorCode = 1 };

        Vector2 v2 = default;
        if (BitConverter.IsLittleEndian)
        {
            v2.x = Unsafe.ReadUnaligned<float>(bytes);
            v2.y = Unsafe.ReadUnaligned<float>(bytes + 4);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v2.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v2.y = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 8;
        return v2;
    }
    public override unsafe Vector2 ReadObject(Stream stream, out int bytesRead)
    {
        Vector2 v2 = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
            int ct = stream.Read(span, 0, 8);
#else
        Span<byte> span = stackalloc byte[8];
        int ct = stream.Read(span);
#endif
            
        bytesRead = ct;
        if (ct != 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityVector2Parser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v2.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            v2.y = Unsafe.ReadUnaligned<float>(ref span[4]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v2.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v2.y = Unsafe.As<int, float>(ref read);
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        bytesRead = 8;
        return v2;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Vector2>
    {
        protected override Vector2 FlipBits(Vector2 toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.y));
            toFlip.y = Unsafe.As<int, float>(ref read);

            return toFlip;
        }
        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            bytes += hdrSize;
            byte* end = bytes + size;
            const int elementSize = 8;
            while (bytes < end)
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
                Unsafe.WriteUnaligned(bytes, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
                Unsafe.WriteUnaligned(bytes + 4, read);

                bytes += elementSize;
            }
        }
        protected override void FlipBits(byte[] bytes, int index, int size)
        {
            const int elementSize = 8;
            for (; index < size; index += elementSize)
            {
                ref byte pos = ref bytes[index];
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));

                pos = ref Unsafe.Add(ref pos, 4);
                Unsafe.WriteUnaligned(ref pos, BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref pos)));
            }
        }

        public Many(SerializationConfiguration config) : base(config, 8, sizeof(float), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Vector2 value)
        {
            *(float*)ptr = value.x;
            *(float*)(ptr + 4) = value.y;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Vector2 value)
        {
            Unsafe.WriteUnaligned(ptr, value.x);
            Unsafe.WriteUnaligned(ptr + 4, value.y);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Vector2 value)
        {
            MemoryMarshal.Write(span, ref value.x);
            MemoryMarshal.Write(span.Slice(4), ref value.y);
        }
        private static Vector2 ReadFromBufferIntl(byte* ptr)
        {
            Vector2 v2 = default;
            v2.x = *(float*)ptr;
            v2.y = *(float*)(ptr + 4);
            return v2;
        }
        private static Vector2 ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Vector2 v2 = default;
            v2.x = Unsafe.ReadUnaligned<float>(ptr);
            v2.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            return v2;
        }
        private static Vector2 ReadFromBufferSpanIntl(Span<byte> span)
        {
            Vector2 v2 = default;
            v2.x = MemoryMarshal.Read<float>(span);
            v2.y = MemoryMarshal.Read<float>(span.Slice(4));
            return v2;
        }
    }
}