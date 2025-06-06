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
public class UnityRayParser : BinaryTypeParser<Ray>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 24;

    public override unsafe int WriteObject(Ray value, byte* bytes, uint maxSize)
    {
        if (maxSize < 24)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 1 };

        Vector3 origin = value.origin;
        Vector3 direction = value.direction;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, origin.x);
            Unsafe.WriteUnaligned(bytes + 4, origin.y);
            Unsafe.WriteUnaligned(bytes + 8, origin.z);
            Unsafe.WriteUnaligned(bytes + 12, direction.x);
            Unsafe.WriteUnaligned(bytes + 16, direction.y);
            Unsafe.WriteUnaligned(bytes + 20, direction.z);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.x)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.y)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.z)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.x)));
            Unsafe.WriteUnaligned(bytes + 16, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.y)));
            Unsafe.WriteUnaligned(bytes + 20, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.z)));
        }

        return 24;
    }

    public override int WriteObject(Ray value, Stream stream)
    {
        Vector3 origin = value.origin;
        Vector3 direction = value.direction;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ref span[0], origin.x);
                Unsafe.WriteUnaligned(ref span[4], origin.y);
                Unsafe.WriteUnaligned(ref span[8], origin.z);
                stream.Write(span, 0, 12);
                Unsafe.WriteUnaligned(ref span[0], direction.x);
                Unsafe.WriteUnaligned(ref span[4], direction.y);
                Unsafe.WriteUnaligned(ref span[8], direction.z);
                stream.Write(span, 0, 12);
            }
            else
            {
                Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.x)));
                Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.y)));
                Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.z)));
                stream.Write(span, 0, 12);
                Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.x)));
                Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.y)));
                Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.z)));
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
            Unsafe.WriteUnaligned(ref span[0], origin.x);
            Unsafe.WriteUnaligned(ref span[4], origin.y);
            Unsafe.WriteUnaligned(ref span[8], origin.z);
            Unsafe.WriteUnaligned(ref span[12], direction.x);
            Unsafe.WriteUnaligned(ref span[16], direction.y);
            Unsafe.WriteUnaligned(ref span[20], direction.z);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.x)));
            Unsafe.WriteUnaligned(ref span[4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.y)));
            Unsafe.WriteUnaligned(ref span[8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref origin.z)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.x)));
            Unsafe.WriteUnaligned(ref span[16], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.y)));
            Unsafe.WriteUnaligned(ref span[20], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref direction.z)));
        }
        stream.Write(span);
#endif
        return 24;
    }
    public override unsafe Ray ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 24)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 1 };

        Ray ray = default;
        Vector3 origin = default;
        Vector3 direction = default;
        if (BitConverter.IsLittleEndian)
        {
            origin.x = Unsafe.ReadUnaligned<float>(bytes);
            origin.y = Unsafe.ReadUnaligned<float>(bytes + 4);
            origin.z = Unsafe.ReadUnaligned<float>(bytes + 8);
            direction.x = Unsafe.ReadUnaligned<float>(bytes + 12);
            direction.y = Unsafe.ReadUnaligned<float>(bytes + 16);
            direction.z = Unsafe.ReadUnaligned<float>(bytes + 20);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            origin.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            origin.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            origin.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            direction.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 16));
            direction.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 20));
            direction.z = Unsafe.As<int, float>(ref read);
        }

        ray.origin = origin;
        ray.direction = direction;

        bytesRead = 24;
        return ray;
    }
    public override Ray ReadObject(Stream stream, out int bytesRead)
    {
        Ray ray = default;
        Vector3 origin = default;
        Vector3 direction = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        bytesRead = 0;
        byte[] span = DefaultSerializer.ArrayPool.Rent(12);
        try
        {
            int ct = stream.Read(span, 0, 12);
            bytesRead += ct;
            if (ct != 12)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 2 };

            if (BitConverter.IsLittleEndian)
            {
                origin.x = Unsafe.ReadUnaligned<float>(ref span[0]);
                origin.y = Unsafe.ReadUnaligned<float>(ref span[4]);
                origin.z = Unsafe.ReadUnaligned<float>(ref span[8]);

                ct = stream.Read(span, 0, 12);
                bytesRead += ct;
                if (ct != 12)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 2 };

                direction.x = Unsafe.ReadUnaligned<float>(ref span[0]);
                direction.y = Unsafe.ReadUnaligned<float>(ref span[4]);
                direction.z = Unsafe.ReadUnaligned<float>(ref span[8]);
            }
            else
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                origin.x = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                origin.y = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                origin.z = Unsafe.As<int, float>(ref read);

                ct = stream.Read(span, 0, 12);
                bytesRead += ct;
                if (ct != 12)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 2 };

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                direction.x = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                direction.y = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                direction.z = Unsafe.As<int, float>(ref read);
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityRayParser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            origin.x = Unsafe.ReadUnaligned<float>(ref span[0]);
            origin.y = Unsafe.ReadUnaligned<float>(ref span[4]);
            origin.z = Unsafe.ReadUnaligned<float>(ref span[8]);
            direction.x = Unsafe.ReadUnaligned<float>(ref span[12]);
            direction.y = Unsafe.ReadUnaligned<float>(ref span[16]);
            direction.z = Unsafe.ReadUnaligned<float>(ref span[20]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            origin.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            origin.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            origin.z = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            direction.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[16]));
            direction.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[20]));
            direction.z = Unsafe.As<int, float>(ref read);
        }
#endif

        ray.origin = origin;
        ray.direction = direction;

        return ray;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Ray>
    {
        public Many(SerializationConfiguration config) : base(config, 24, sizeof(float), false, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }

        private static void Flip(ref Vector3 toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.x));
            toFlip.x = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.y));
            toFlip.y = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.z));
            toFlip.z = Unsafe.As<int, float>(ref read);
        }

        private static void WriteToBufferIntl(byte* ptr, Ray value)
        {
            Vector3 v3 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            *(float*)ptr = v3.x;
            *(float*)(ptr + 4)  = v3.y;
            *(float*)(ptr + 8)  = v3.z;

            v3 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            *(float*)(ptr + 12) = v3.x;
            *(float*)(ptr + 16) = v3.y;
            *(float*)(ptr + 20) = v3.z;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Ray value)
        {
            Vector3 v3 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            Unsafe.WriteUnaligned(ptr, v3.x);
            Unsafe.WriteUnaligned(ptr + 4, v3.y);
            Unsafe.WriteUnaligned(ptr + 8, v3.z);

            v3 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            Unsafe.WriteUnaligned(ptr + 12, v3.x);
            Unsafe.WriteUnaligned(ptr + 16, v3.y);
            Unsafe.WriteUnaligned(ptr + 20, v3.z);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Ray value)
        {
            Vector3 v3 = value.origin;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            MemoryMarshal.Write(span, ref v3.x);
            MemoryMarshal.Write(span.Slice(4), ref v3.y);
            MemoryMarshal.Write(span.Slice(8), ref v3.z);

            v3 = value.direction;
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);

            MemoryMarshal.Write(span.Slice(12), ref v3.x);
            MemoryMarshal.Write(span.Slice(16), ref v3.y);
            MemoryMarshal.Write(span.Slice(20), ref v3.z);
        }
        private static Ray ReadFromBufferIntl(byte* ptr)
        {
            Ray ray = default;
            Vector3 v3 = default;
            v3.x = *(float*)ptr;
            v3.y = *(float*)(ptr + 4);
            v3.z = *(float*)(ptr + 8);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.origin = v3;
            v3.x = *(float*)(ptr + 12);
            v3.y = *(float*)(ptr + 16);
            v3.z = *(float*)(ptr + 20);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.direction = v3;
            return ray;
        }
        private static Ray ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Ray ray = default;
            Vector3 v3 = default;
            v3.x = Unsafe.ReadUnaligned<float>(ptr);
            v3.y = Unsafe.ReadUnaligned<float>(ptr + 4);
            v3.z = Unsafe.ReadUnaligned<float>(ptr + 8);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.origin = v3;
            v3.x = Unsafe.ReadUnaligned<float>(ptr + 12);
            v3.y = Unsafe.ReadUnaligned<float>(ptr + 16);
            v3.z = Unsafe.ReadUnaligned<float>(ptr + 20);
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.direction = v3;
            return ray;
        }
        private static Ray ReadFromBufferSpanIntl(Span<byte> span)
        {
            Ray ray = default;
            Vector3 v3 = default;
            v3.x = MemoryMarshal.Read<float>(span);
            v3.y = MemoryMarshal.Read<float>(span.Slice(4));
            v3.z = MemoryMarshal.Read<float>(span.Slice(8));
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.origin = v3;
            v3.x = MemoryMarshal.Read<float>(span.Slice(12));
            v3.y = MemoryMarshal.Read<float>(span.Slice(16));
            v3.z = MemoryMarshal.Read<float>(span.Slice(20));
            if (!BitConverter.IsLittleEndian)
                Flip(ref v3);
            ray.direction = v3;
            return ray;
        }
    }
}