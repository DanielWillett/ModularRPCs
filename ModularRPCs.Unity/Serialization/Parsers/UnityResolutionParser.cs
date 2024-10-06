using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityResolutionParser : BinaryTypeParser<Resolution>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 12;
    public override unsafe int WriteObject(Resolution value, byte* bytes, uint maxSize)
    {
        if (maxSize < 12)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityResolutionParser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes,     value.width);
            Unsafe.WriteUnaligned(bytes + 4, value.height);
            Unsafe.WriteUnaligned(bytes + 8, value.refreshRate);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,     BinaryPrimitives.ReverseEndianness(value.width));
            Unsafe.WriteUnaligned(bytes + 4, BinaryPrimitives.ReverseEndianness(value.height));
            Unsafe.WriteUnaligned(bytes + 8, BinaryPrimitives.ReverseEndianness(value.refreshRate));
        }

        return 12;
    }
    public override int WriteObject(Resolution value, Stream stream)
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
            Unsafe.WriteUnaligned(ref span[0], value.width);
            Unsafe.WriteUnaligned(ref span[4], value.height);
            Unsafe.WriteUnaligned(ref span[8], value.refreshRate);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(value.width));
            Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(value.height));
            Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(value.refreshRate));
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
    public override unsafe Resolution ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 12)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityResolutionParser))) { ErrorCode = 1 };

        Resolution v3 = default;
        if (BitConverter.IsLittleEndian)
        {
            v3.width = Unsafe.ReadUnaligned<int>(bytes);
            v3.height = Unsafe.ReadUnaligned<int>(bytes + 4);
            v3.refreshRate = Unsafe.ReadUnaligned<int>(bytes + 8);
        }
        else
        {
            v3.width = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            v3.height = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            v3.refreshRate = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
        }

        bytesRead = 12;
        return v3;
    }
    public override Resolution ReadObject(Stream stream, out int bytesRead)
    {
        Resolution v3 = default;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityResolutionParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            v3.width = Unsafe.ReadUnaligned<int>(ref span[0]);
            v3.height = Unsafe.ReadUnaligned<int>(ref span[4]);
            v3.refreshRate = Unsafe.ReadUnaligned<int>(ref span[8]);
        }
        else
        {
            v3.width = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            v3.height = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            v3.refreshRate = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
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
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Resolution>
    {
        protected override Resolution FlipBits(Resolution toFlip)
        {
            toFlip.width = BinaryPrimitives.ReverseEndianness(toFlip.width);
            toFlip.height = BinaryPrimitives.ReverseEndianness(toFlip.height);
            toFlip.refreshRate = BinaryPrimitives.ReverseEndianness(toFlip.refreshRate);
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

        public Many(SerializationConfiguration config) : base(config, 12, sizeof(int), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Resolution value)
        {
            *(int*)ptr = value.width;
            *(int*)(ptr + 4) = value.height;
            *(int*)(ptr + 8) = value.refreshRate;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Resolution value)
        {
            Unsafe.WriteUnaligned(ptr, value.width);
            Unsafe.WriteUnaligned(ptr + 4, value.height);
            Unsafe.WriteUnaligned(ptr + 8, value.refreshRate);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Resolution value)
        {
            int w = value.width, h = value.height, r = value.refreshRate;
            MemoryMarshal.Write(span, ref w);
            MemoryMarshal.Write(span.Slice(4), ref h);
            MemoryMarshal.Write(span.Slice(8), ref r);
        }
        private static Resolution ReadFromBufferIntl(byte* ptr)
        {
            Resolution v3 = default;
            v3.width = *(int*)ptr;
            v3.height = *(int*)(ptr + 4);
            v3.refreshRate = *(int*)(ptr + 8);
            return v3;
        }
        private static Resolution ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Resolution v3 = default;
            v3.width = Unsafe.ReadUnaligned<int>(ptr);
            v3.height = Unsafe.ReadUnaligned<int>(ptr + 4);
            v3.refreshRate = Unsafe.ReadUnaligned<int>(ptr + 8);
            return v3;
        }
        private static Resolution ReadFromBufferSpanIntl(Span<byte> span)
        {
            Resolution v3 = default;
            v3.width = MemoryMarshal.Read<int>(span);
            v3.height = MemoryMarshal.Read<int>(span.Slice(4));
            v3.refreshRate = MemoryMarshal.Read<int>(span.Slice(8));
            return v3;
        }
    }
}