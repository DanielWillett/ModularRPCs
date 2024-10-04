using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityMatrix4x4Parser : BinaryTypeParser<Matrix4x4>, IBinaryTypeParser
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 64;

    public override unsafe int WriteObject(Matrix4x4 value, byte* bytes, uint maxSize)
    {
        return WriteObject(ref value, bytes, maxSize);
    }

    public override int WriteObject(Matrix4x4 value, Stream stream)
    {
        return WriteObject(ref value, stream);
    }

    unsafe int IBinaryTypeParser.WriteObject(TypedReference value, byte* bytes, uint maxSize)
    {
        ref Matrix4x4 matrix = ref __refvalue(value, Matrix4x4);
        return WriteObject(ref matrix, bytes, maxSize);
    }

    int IBinaryTypeParser.WriteObject(TypedReference value, Stream stream)
    {
        ref Matrix4x4 matrix = ref __refvalue(value, Matrix4x4);
        return WriteObject(ref matrix, stream);
    }

    private static unsafe int WriteObject(ref Matrix4x4 value, byte* bytes, uint maxSize)
    {
        if (maxSize < 64)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes,      value.m00);
            Unsafe.WriteUnaligned(bytes +  4, value.m10);
            Unsafe.WriteUnaligned(bytes +  8, value.m20);
            Unsafe.WriteUnaligned(bytes + 12, value.m30);
            Unsafe.WriteUnaligned(bytes + 16, value.m01);
            Unsafe.WriteUnaligned(bytes + 20, value.m11);
            Unsafe.WriteUnaligned(bytes + 24, value.m21);
            Unsafe.WriteUnaligned(bytes + 28, value.m31);
            Unsafe.WriteUnaligned(bytes + 32, value.m02);
            Unsafe.WriteUnaligned(bytes + 36, value.m12);
            Unsafe.WriteUnaligned(bytes + 40, value.m22);
            Unsafe.WriteUnaligned(bytes + 44, value.m32);
            Unsafe.WriteUnaligned(bytes + 48, value.m03);
            Unsafe.WriteUnaligned(bytes + 52, value.m13);
            Unsafe.WriteUnaligned(bytes + 56, value.m23);
            Unsafe.WriteUnaligned(bytes + 60, value.m33);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes,      BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m00)));
            Unsafe.WriteUnaligned(bytes +  4, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m10)));
            Unsafe.WriteUnaligned(bytes +  8, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m20)));
            Unsafe.WriteUnaligned(bytes + 12, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m30)));
            Unsafe.WriteUnaligned(bytes + 16, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m01)));
            Unsafe.WriteUnaligned(bytes + 20, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m11)));
            Unsafe.WriteUnaligned(bytes + 24, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m21)));
            Unsafe.WriteUnaligned(bytes + 28, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m31)));
            Unsafe.WriteUnaligned(bytes + 32, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m02)));
            Unsafe.WriteUnaligned(bytes + 36, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m12)));
            Unsafe.WriteUnaligned(bytes + 40, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m22)));
            Unsafe.WriteUnaligned(bytes + 44, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m32)));
            Unsafe.WriteUnaligned(bytes + 48, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m03)));
            Unsafe.WriteUnaligned(bytes + 52, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m13)));
            Unsafe.WriteUnaligned(bytes + 56, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m23)));
            Unsafe.WriteUnaligned(bytes + 60, BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m33)));
        }

        return 64;
    }

    public static int WriteObject(ref Matrix4x4 value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(ref span[ 0], value.m00);
                Unsafe.WriteUnaligned(ref span[ 4], value.m10);
                Unsafe.WriteUnaligned(ref span[ 8], value.m20);
                Unsafe.WriteUnaligned(ref span[12], value.m30);
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], value.m01);
                Unsafe.WriteUnaligned(ref span[ 4], value.m11);
                Unsafe.WriteUnaligned(ref span[ 8], value.m21);
                Unsafe.WriteUnaligned(ref span[12], value.m31);
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], value.m02);
                Unsafe.WriteUnaligned(ref span[ 4], value.m12);
                Unsafe.WriteUnaligned(ref span[ 8], value.m22);
                Unsafe.WriteUnaligned(ref span[12], value.m32);
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], value.m03);
                Unsafe.WriteUnaligned(ref span[ 4], value.m13);
                Unsafe.WriteUnaligned(ref span[ 8], value.m23);
                Unsafe.WriteUnaligned(ref span[12], value.m33);
                stream.Write(span, 0, 16);
            }
            else
            {
                Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m00)));
                Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m10)));
                Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m20)));
                Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m30)));
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m01)));
                Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m11)));
                Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m21)));
                Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m31)));
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m02)));
                Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m12)));
                Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m22)));
                Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m32)));
                stream.Write(span, 0, 16);
                Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m03)));
                Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m13)));
                Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m23)));
                Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m33)));
                stream.Write(span, 0, 16);
            }
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#else
        Span<byte> span = stackalloc byte[64];
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[ 0], value.m00);
            Unsafe.WriteUnaligned(ref span[ 4], value.m10);
            Unsafe.WriteUnaligned(ref span[ 8], value.m20);
            Unsafe.WriteUnaligned(ref span[12], value.m30);
            Unsafe.WriteUnaligned(ref span[16], value.m01);
            Unsafe.WriteUnaligned(ref span[20], value.m11);
            Unsafe.WriteUnaligned(ref span[24], value.m21);
            Unsafe.WriteUnaligned(ref span[28], value.m31);
            Unsafe.WriteUnaligned(ref span[32], value.m02);
            Unsafe.WriteUnaligned(ref span[36], value.m12);
            Unsafe.WriteUnaligned(ref span[40], value.m22);
            Unsafe.WriteUnaligned(ref span[44], value.m32);
            Unsafe.WriteUnaligned(ref span[48], value.m03);
            Unsafe.WriteUnaligned(ref span[52], value.m13);
            Unsafe.WriteUnaligned(ref span[56], value.m23);
            Unsafe.WriteUnaligned(ref span[60], value.m33);
        }
        else
        {
            Unsafe.WriteUnaligned(ref span[ 0], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m00)));
            Unsafe.WriteUnaligned(ref span[ 4], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m10)));
            Unsafe.WriteUnaligned(ref span[ 8], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m20)));
            Unsafe.WriteUnaligned(ref span[12], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m30)));
            Unsafe.WriteUnaligned(ref span[16], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m01)));
            Unsafe.WriteUnaligned(ref span[20], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m11)));
            Unsafe.WriteUnaligned(ref span[24], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m21)));
            Unsafe.WriteUnaligned(ref span[28], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m31)));
            Unsafe.WriteUnaligned(ref span[32], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m02)));
            Unsafe.WriteUnaligned(ref span[36], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m12)));
            Unsafe.WriteUnaligned(ref span[40], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m22)));
            Unsafe.WriteUnaligned(ref span[44], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m32)));
            Unsafe.WriteUnaligned(ref span[48], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m03)));
            Unsafe.WriteUnaligned(ref span[52], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m13)));
            Unsafe.WriteUnaligned(ref span[56], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m23)));
            Unsafe.WriteUnaligned(ref span[60], BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref value.m33)));
        }

        stream.Write(span);
#endif
        return 64;
    }
    public override unsafe Matrix4x4 ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        Matrix4x4 matrix = default;
        ReadObject(ref matrix, bytes, maxSize, out bytesRead);
        return matrix;
    }

    public override Matrix4x4 ReadObject(Stream stream, out int bytesRead)
    {
        Matrix4x4 matrix = default;
        ReadObject(ref matrix, stream, out bytesRead);
        return matrix;
    }

    unsafe void IBinaryTypeParser.ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj)
    {
        ref Matrix4x4 matrix = ref __refvalue(outObj, Matrix4x4);
        ReadObject(ref matrix, bytes, maxSize, out bytesRead);
    }

    void IBinaryTypeParser.ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
    {
        ref Matrix4x4 matrix = ref __refvalue(outObj, Matrix4x4);
        ReadObject(ref matrix, stream, out bytesRead);
    }

    private static unsafe void ReadObject(ref Matrix4x4 matrix, byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 64)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            matrix.m00 = Unsafe.ReadUnaligned<float>(bytes);
            matrix.m10 = Unsafe.ReadUnaligned<float>(bytes + 4);
            matrix.m20 = Unsafe.ReadUnaligned<float>(bytes + 8);
            matrix.m30 = Unsafe.ReadUnaligned<float>(bytes + 12);
            matrix.m01 = Unsafe.ReadUnaligned<float>(bytes + 16);
            matrix.m11 = Unsafe.ReadUnaligned<float>(bytes + 20);
            matrix.m21 = Unsafe.ReadUnaligned<float>(bytes + 24);
            matrix.m31 = Unsafe.ReadUnaligned<float>(bytes + 28);
            matrix.m02 = Unsafe.ReadUnaligned<float>(bytes + 32);
            matrix.m12 = Unsafe.ReadUnaligned<float>(bytes + 36);
            matrix.m22 = Unsafe.ReadUnaligned<float>(bytes + 40);
            matrix.m32 = Unsafe.ReadUnaligned<float>(bytes + 44);
            matrix.m03 = Unsafe.ReadUnaligned<float>(bytes + 48);
            matrix.m13 = Unsafe.ReadUnaligned<float>(bytes + 52);
            matrix.m23 = Unsafe.ReadUnaligned<float>(bytes + 56);
            matrix.m33 = Unsafe.ReadUnaligned<float>(bytes + 60);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes));
            matrix.m00 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 4));
            matrix.m10 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 8));
            matrix.m20 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 12));
            matrix.m30 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 16));
            matrix.m01 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 20));
            matrix.m11 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 24));
            matrix.m21 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 28));
            matrix.m31 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 32));
            matrix.m02 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 36));
            matrix.m12 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 40));
            matrix.m22 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 44));
            matrix.m32 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 48));
            matrix.m03 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 52));
            matrix.m13 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 56));
            matrix.m23 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 60));
            matrix.m33 = Unsafe.As<int, float>(ref read);
        }

        bytesRead = 64;
    }
    private static void ReadObject(ref Matrix4x4 matrix, Stream stream, out int bytesRead)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        bytesRead = 0;
        byte[] span = DefaultSerializer.ArrayPool.Rent(16);
        try
        {
            int ct = stream.Read(span, 0, 16);
            bytesRead += ct;
            if (ct != 16)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

            if (BitConverter.IsLittleEndian)
            {
                matrix.m00 = Unsafe.ReadUnaligned<float>(ref span[ 0]);
                matrix.m10 = Unsafe.ReadUnaligned<float>(ref span[ 4]);
                matrix.m20 = Unsafe.ReadUnaligned<float>(ref span[ 8]);
                matrix.m30 = Unsafe.ReadUnaligned<float>(ref span[12]);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                matrix.m01 = Unsafe.ReadUnaligned<float>(ref span[ 0]);
                matrix.m11 = Unsafe.ReadUnaligned<float>(ref span[ 4]);
                matrix.m21 = Unsafe.ReadUnaligned<float>(ref span[ 8]);
                matrix.m31 = Unsafe.ReadUnaligned<float>(ref span[12]);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                matrix.m02 = Unsafe.ReadUnaligned<float>(ref span[ 0]);
                matrix.m12 = Unsafe.ReadUnaligned<float>(ref span[ 4]);
                matrix.m22 = Unsafe.ReadUnaligned<float>(ref span[ 8]);
                matrix.m32 = Unsafe.ReadUnaligned<float>(ref span[12]);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                matrix.m03 = Unsafe.ReadUnaligned<float>(ref span[ 0]);
                matrix.m13 = Unsafe.ReadUnaligned<float>(ref span[ 4]);
                matrix.m23 = Unsafe.ReadUnaligned<float>(ref span[ 8]);
                matrix.m33 = Unsafe.ReadUnaligned<float>(ref span[12]);
            }
            else
            {
                int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                matrix.m00 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                matrix.m10 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                matrix.m20 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
                matrix.m30 = Unsafe.As<int, float>(ref read);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                matrix.m01 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                matrix.m11 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                matrix.m21 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
                matrix.m31 = Unsafe.As<int, float>(ref read);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                matrix.m02 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                matrix.m12 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                matrix.m22 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
                matrix.m32 = Unsafe.As<int, float>(ref read);

                ct = stream.Read(span, 0, 16);
                bytesRead += ct;
                if (ct != 16)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
                matrix.m03 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
                matrix.m13 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
                matrix.m23 = Unsafe.As<int, float>(ref read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
                matrix.m33 = Unsafe.As<int, float>(ref read);
            }

        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#else
        Span<byte> span = stackalloc byte[64];
        int ct = stream.Read(span);
        bytesRead = ct;
        if (ct != 64)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityMatrix4x4Parser))) { ErrorCode = 2 };

        if (BitConverter.IsLittleEndian)
        {
            matrix.m00 = Unsafe.ReadUnaligned<float>(ref span[0]);
            matrix.m10 = Unsafe.ReadUnaligned<float>(ref span[4]);
            matrix.m20 = Unsafe.ReadUnaligned<float>(ref span[8]);
            matrix.m30 = Unsafe.ReadUnaligned<float>(ref span[12]);
            matrix.m01 = Unsafe.ReadUnaligned<float>(ref span[16]);
            matrix.m11 = Unsafe.ReadUnaligned<float>(ref span[20]);
            matrix.m21 = Unsafe.ReadUnaligned<float>(ref span[24]);
            matrix.m31 = Unsafe.ReadUnaligned<float>(ref span[28]);
            matrix.m02 = Unsafe.ReadUnaligned<float>(ref span[32]);
            matrix.m12 = Unsafe.ReadUnaligned<float>(ref span[36]);
            matrix.m22 = Unsafe.ReadUnaligned<float>(ref span[40]);
            matrix.m32 = Unsafe.ReadUnaligned<float>(ref span[44]);
            matrix.m03 = Unsafe.ReadUnaligned<float>(ref span[48]);
            matrix.m13 = Unsafe.ReadUnaligned<float>(ref span[52]);
            matrix.m23 = Unsafe.ReadUnaligned<float>(ref span[56]);
            matrix.m33 = Unsafe.ReadUnaligned<float>(ref span[60]);
        }
        else
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[0]));
            matrix.m00 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[4]));
            matrix.m10 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[8]));
            matrix.m20 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[12]));
            matrix.m30 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[16]));
            matrix.m01 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[20]));
            matrix.m11 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[24]));
            matrix.m21 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[28]));
            matrix.m31 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[32]));
            matrix.m02 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[36]));
            matrix.m12 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[40]));
            matrix.m22 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[44]));
            matrix.m32 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[48]));
            matrix.m03 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[52]));
            matrix.m13 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[56]));
            matrix.m23 = Unsafe.As<int, float>(ref read);

            read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref span[60]));
            matrix.m33 = Unsafe.As<int, float>(ref read);
        }
#endif
    }

    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Matrix4x4>
    {
        protected override Matrix4x4 FlipBits(Matrix4x4 toFlip)
        {
            int read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m00));
            toFlip.m00 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m10));
            toFlip.m10 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m20));
            toFlip.m20 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m30));
            toFlip.m30 = Unsafe.As<int, float>(ref read);
            
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m01));
            toFlip.m01 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m11));
            toFlip.m11 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m21));
            toFlip.m21 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m31));
            toFlip.m31 = Unsafe.As<int, float>(ref read);
            
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m02));
            toFlip.m02 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m12));
            toFlip.m12 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m22));
            toFlip.m22 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m32));
            toFlip.m32 = Unsafe.As<int, float>(ref read);
            
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m03));
            toFlip.m03 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m13));
            toFlip.m13 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m23));
            toFlip.m23 = Unsafe.As<int, float>(ref read);
            read = BinaryPrimitives.ReverseEndianness(Unsafe.As<float, int>(ref toFlip.m33));
            toFlip.m33 = Unsafe.As<int, float>(ref read);

            return toFlip;
        }

        protected override void FlipBits(byte* bytes, int hdrSize, int size)
        {
            bytes += hdrSize;
            byte* end = bytes + size;
            const int elementSize = 64;
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

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 24));
                Unsafe.WriteUnaligned(bytes + 24, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 28));
                Unsafe.WriteUnaligned(bytes + 28, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 32));
                Unsafe.WriteUnaligned(bytes + 32, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 36));
                Unsafe.WriteUnaligned(bytes + 36, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 40));
                Unsafe.WriteUnaligned(bytes + 40, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 44));
                Unsafe.WriteUnaligned(bytes + 44, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 48));
                Unsafe.WriteUnaligned(bytes + 48, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 52));
                Unsafe.WriteUnaligned(bytes + 52, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 56));
                Unsafe.WriteUnaligned(bytes + 56, read);

                read = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(bytes + 60));
                Unsafe.WriteUnaligned(bytes + 60, read);

                bytes += elementSize;
            }
        }

        protected override void FlipBits(byte[] bytes, int index, int size)
        {
            const int elementSize = 64;
            for (; index < size; index += elementSize)
            {
                ref byte pos = ref bytes[index * elementSize];
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

        public Many(SerializationConfiguration config) : base(config, 64, sizeof(float), true, &WriteToBufferIntl, &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferUnalignedIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Matrix4x4 value)
        {
            *(float*)ptr        = value.m00;
            *(float*)(ptr +  4) = value.m10;
            *(float*)(ptr +  8) = value.m20;
            *(float*)(ptr + 12) = value.m30;
            *(float*)(ptr + 16) = value.m01;
            *(float*)(ptr + 20) = value.m11;
            *(float*)(ptr + 24) = value.m21;
            *(float*)(ptr + 28) = value.m31;
            *(float*)(ptr + 32) = value.m02;
            *(float*)(ptr + 36) = value.m12;
            *(float*)(ptr + 40) = value.m22;
            *(float*)(ptr + 44) = value.m32;
            *(float*)(ptr + 48) = value.m03;
            *(float*)(ptr + 52) = value.m13;
            *(float*)(ptr + 56) = value.m23;
            *(float*)(ptr + 60) = value.m33;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, Matrix4x4 value)
        {
            Unsafe.WriteUnaligned(ptr     , value.m00);
            Unsafe.WriteUnaligned(ptr +  4, value.m10);
            Unsafe.WriteUnaligned(ptr +  8, value.m20);
            Unsafe.WriteUnaligned(ptr + 12, value.m30);
            Unsafe.WriteUnaligned(ptr + 16, value.m01);
            Unsafe.WriteUnaligned(ptr + 20, value.m11);
            Unsafe.WriteUnaligned(ptr + 24, value.m21);
            Unsafe.WriteUnaligned(ptr + 28, value.m31);
            Unsafe.WriteUnaligned(ptr + 32, value.m02);
            Unsafe.WriteUnaligned(ptr + 36, value.m12);
            Unsafe.WriteUnaligned(ptr + 40, value.m22);
            Unsafe.WriteUnaligned(ptr + 44, value.m32);
            Unsafe.WriteUnaligned(ptr + 48, value.m03);
            Unsafe.WriteUnaligned(ptr + 52, value.m13);
            Unsafe.WriteUnaligned(ptr + 56, value.m23);
            Unsafe.WriteUnaligned(ptr + 60, value.m33);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Matrix4x4 value)
        {
            MemoryMarshal.Write(span.Slice( 0), ref value.m00);
            MemoryMarshal.Write(span.Slice( 4), ref value.m10);
            MemoryMarshal.Write(span.Slice( 8), ref value.m20);
            MemoryMarshal.Write(span.Slice(12), ref value.m30);
            MemoryMarshal.Write(span.Slice(16), ref value.m01);
            MemoryMarshal.Write(span.Slice(20), ref value.m11);
            MemoryMarshal.Write(span.Slice(24), ref value.m21);
            MemoryMarshal.Write(span.Slice(28), ref value.m31);
            MemoryMarshal.Write(span.Slice(32), ref value.m02);
            MemoryMarshal.Write(span.Slice(36), ref value.m12);
            MemoryMarshal.Write(span.Slice(40), ref value.m22);
            MemoryMarshal.Write(span.Slice(44), ref value.m32);
            MemoryMarshal.Write(span.Slice(48), ref value.m03);
            MemoryMarshal.Write(span.Slice(52), ref value.m13);
            MemoryMarshal.Write(span.Slice(56), ref value.m23);
            MemoryMarshal.Write(span.Slice(60), ref value.m33);
        }
        private static Matrix4x4 ReadFromBufferIntl(byte* ptr)
        {
            Matrix4x4 matrix = default;
            matrix.m00 = *(float*)ptr;
            matrix.m10 = *(float*)(ptr + 4);
            matrix.m20 = *(float*)(ptr + 8);
            matrix.m30 = *(float*)(ptr + 12);
            matrix.m01 = *(float*)(ptr + 16);
            matrix.m11 = *(float*)(ptr + 20);
            matrix.m21 = *(float*)(ptr + 24);
            matrix.m31 = *(float*)(ptr + 28);
            matrix.m02 = *(float*)(ptr + 32);
            matrix.m12 = *(float*)(ptr + 36);
            matrix.m22 = *(float*)(ptr + 40);
            matrix.m32 = *(float*)(ptr + 44);
            matrix.m03 = *(float*)(ptr + 48);
            matrix.m13 = *(float*)(ptr + 52);
            matrix.m23 = *(float*)(ptr + 56);
            matrix.m33 = *(float*)(ptr + 60);
            return matrix;
        }
        private static Matrix4x4 ReadFromBufferUnalignedIntl(byte* ptr)
        {
            Matrix4x4 matrix = default;
            matrix.m00 = Unsafe.ReadUnaligned<float>(ptr);
            matrix.m10 = Unsafe.ReadUnaligned<float>(ptr + 4);
            matrix.m20 = Unsafe.ReadUnaligned<float>(ptr + 8);
            matrix.m30 = Unsafe.ReadUnaligned<float>(ptr + 12);
            matrix.m01 = Unsafe.ReadUnaligned<float>(ptr + 16);
            matrix.m11 = Unsafe.ReadUnaligned<float>(ptr + 20);
            matrix.m21 = Unsafe.ReadUnaligned<float>(ptr + 24);
            matrix.m31 = Unsafe.ReadUnaligned<float>(ptr + 28);
            matrix.m02 = Unsafe.ReadUnaligned<float>(ptr + 32);
            matrix.m12 = Unsafe.ReadUnaligned<float>(ptr + 36);
            matrix.m22 = Unsafe.ReadUnaligned<float>(ptr + 40);
            matrix.m32 = Unsafe.ReadUnaligned<float>(ptr + 44);
            matrix.m03 = Unsafe.ReadUnaligned<float>(ptr + 48);
            matrix.m13 = Unsafe.ReadUnaligned<float>(ptr + 52);
            matrix.m23 = Unsafe.ReadUnaligned<float>(ptr + 56);
            matrix.m33 = Unsafe.ReadUnaligned<float>(ptr + 60);
            return matrix;
        }
        private static Matrix4x4 ReadFromBufferSpanIntl(Span<byte> span)
        {
            Matrix4x4 matrix = default;
            matrix.m00 = MemoryMarshal.Read<float>(span.Slice(0));
            matrix.m10 = MemoryMarshal.Read<float>(span.Slice(4));
            matrix.m20 = MemoryMarshal.Read<float>(span.Slice(8));
            matrix.m30 = MemoryMarshal.Read<float>(span.Slice(12));
            matrix.m01 = MemoryMarshal.Read<float>(span.Slice(16));
            matrix.m11 = MemoryMarshal.Read<float>(span.Slice(20));
            matrix.m21 = MemoryMarshal.Read<float>(span.Slice(24));
            matrix.m31 = MemoryMarshal.Read<float>(span.Slice(28));
            matrix.m02 = MemoryMarshal.Read<float>(span.Slice(32));
            matrix.m12 = MemoryMarshal.Read<float>(span.Slice(36));
            matrix.m22 = MemoryMarshal.Read<float>(span.Slice(40));
            matrix.m32 = MemoryMarshal.Read<float>(span.Slice(44));
            matrix.m03 = MemoryMarshal.Read<float>(span.Slice(48));
            matrix.m13 = MemoryMarshal.Read<float>(span.Slice(52));
            matrix.m23 = MemoryMarshal.Read<float>(span.Slice(56));
            matrix.m33 = MemoryMarshal.Read<float>(span.Slice(60));
            return matrix;
        }
    }
}