using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityRay2DParser : BinaryTypeParser<Ray2D>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;

    public override unsafe int WriteObject(Ray2D value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityRay2DParser))) { ErrorCode = 1 };

        Vector2 origin = value.origin;
        Vector2 direction = value.direction;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, origin.x);
            Unsafe.WriteUnaligned(bytes + 4, origin.y);
            Unsafe.WriteUnaligned(bytes + 8, direction.x);
            Unsafe.WriteUnaligned(bytes + 12, direction.y);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.x)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.y)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.x)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.y)));
        }

        return 16;
    }

    public override int WriteObject(Ray2D value, Stream stream)
    {
        Vector2 origin = value.origin;
        Vector2 direction = value.direction;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
#else
        Span<byte> span = stackalloc byte[16];
#endif
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[ 0], origin.x);
            Unsafe.WriteUnaligned(ref span[ 4], origin.y);
            Unsafe.WriteUnaligned(ref span[ 8], direction.x);
            Unsafe.WriteUnaligned(ref span[12], direction.y);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.x)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.y)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.x)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.y)));
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
    public override unsafe Ray2D ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityRay2DParser))) { ErrorCode = 1 };

        Ray2D ray = default;
        Vector2 origin = default;
        Vector2 direction = default;
        if (BitConverter.IsLittleEndian)
        {
            origin.x = Unsafe.ReadUnaligned<float>(bytes);
            origin.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            direction.x = Unsafe.ReadUnaligned<float>(bytes + 8);
            direction.y = Unsafe.ReadUnaligned<float>(bytes + 12);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            origin.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            origin.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            direction.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            direction.y = Unsafe.As<int, float>(ref read);
        }

        ray.origin = origin;
        ray.direction = direction;

        bytesRead = 16;
        return ray;
    }
    public override Ray2D ReadObject(Stream stream, out int bytesRead)
    {
        Ray2D ray = default;
        Vector2 origin = default;
        Vector2 direction = default;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityVector4Parser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            origin.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            origin.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            direction.x = Unsafe.ReadUnaligned<float>(ref span[8]);
            direction.y = Unsafe.ReadUnaligned<float>(ref span[12]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            origin.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            origin.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            direction.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            direction.y = Unsafe.As<int, float>(ref read);
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        ray.origin = origin;
        ray.direction = direction;

        return ray;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Ray2D>
    {
        public Many(SerializationConfiguration config) : base(config, 16, sizeof(float), false, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }

        private static void Flip(ref Vector2 toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.y));
            toFlip.y = Unsafe.As<int, float>(ref read);
        }

        private static void WriteToBufferIntl(byte* ptr, Ray2D value)
        {
            Vector2 v2 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            *(float*)ptr = v2.x;
            *(float*)(ptr + 4)  = v2.y;

            v2 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            *(float*)(ptr + 8) = v2.x;
            *(float*)(ptr + 12) = v2.y;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Ray2D value)
        {
            Vector2 v2 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            Unsafe.WriteUnaligned(ptr, v2.x);
            Unsafe.WriteUnaligned(ptr + 4, v2.y);

            v2 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            Unsafe.WriteUnaligned(ptr + 8, v2.x);
            Unsafe.WriteUnaligned(ptr + 12, v2.y);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Ray2D value)
        {
            Vector2 v2 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            MemoryMarshal.Write(span, ref v2.x);
            MemoryMarshal.Write(span.Slice(4), ref v2.y);

            v2 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);

            MemoryMarshal.Write(span.Slice(8), ref v2.x);
            MemoryMarshal.Write(span.Slice(12), ref v2.y);
        }
        private static Ray2D ReadFromBufferIntl(byte* ptr)
        {
            Ray2D ray = default;
            Vector2 v2 = default;
            v2.x = *(float*)ptr;
            v2.y = *(float*)(ptr + 4);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);
            ray.origin = v2;
            v2.x = *(float*)(ptr + 8);
            v2.y = *(float*)(ptr + 12);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);
            ray.direction = v2;
            return ray;
        }
        private static Ray2D ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Ray2D ray = default;
            Vector2 v2 = default;
            v2.x = Unsafe.ReadUnaligned<float>(ptr);
            v2.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);
            ray.origin = v2;
            v2.x = Unsafe.ReadUnaligned<float>(ptr + 8);
            v2.y = Unsafe.ReadUnaligned<float>(ptr + 12);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v2);
            ray.direction = v2;
            return ray;
        }
        private static Ray2D ReadFromBufferSpanIntl(Span<byte> span)
        {
            Ray2D ray = default;
            Vector2 v3 = default;
            v3.x = MemoryMarshal.Read<float>(span);
            v3.y = MemoryMarshal.Read<float>(span.Slice(4));
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.origin = v3;
            v3.x = MemoryMarshal.Read<float>(span.Slice(8));
            v3.y = MemoryMarshal.Read<float>(span.Slice(12));
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.direction = v3;
            return ray;
        }
    }
}