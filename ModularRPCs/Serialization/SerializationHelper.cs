using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization;
public static class SerializationHelper
{
    /// <summary>
    /// Adds or updates a serializer in a dictionary for all the supported array/list types.
    /// <para>These collection types are <typeparamref name="TElementType"/>[], <see cref="IList{T}"/> of <typeparamref name="TElementType"/>, <see cref="IReadOnlyList{T}"/> of <typeparamref name="TElementType"/>, <see cref="Span{T}"/> of <typeparamref name="TElementType"/>, and <see cref="ReadOnlySpan{T}"/> of <typeparamref name="TElementType"/>. If <typeparamref name="TElementType"/> is <see cref="bool"/>, it also includes <see cref="BitArray"/>.</para>
    /// </summary>
    /// <typeparam name="TElementType">The element type of the array.</typeparam>
    public static void AddManySerializer<TElementType>(this IDictionary<Type, IBinaryTypeParser> dict, IArrayBinaryTypeParser<TElementType> parser)
    {
        dict[typeof(TElementType[])] = parser;
        dict[typeof(IList<TElementType>)] = parser;
        dict[typeof(IReadOnlyList<TElementType>)] = parser;
        dict[typeof(Span<TElementType>)] = parser;
        dict[typeof(ReadOnlySpan<TElementType>)] = parser;
        if (typeof(TElementType) == typeof(bool))
            dict[typeof(BitArray)] = parser;
    }

    /// <summary>
    /// Adds or updates a serializer in a dictionary for all the supported array/list types from a factory taking in the collection type. Return <see langword="null"/> to skip the type.
    /// <para>These collection types are <typeparamref name="TElementType"/>[], <see cref="IList{T}"/> of <typeparamref name="TElementType"/>, <see cref="IReadOnlyList{T}"/> of <typeparamref name="TElementType"/>, <see cref="Span{T}"/> of <typeparamref name="TElementType"/>, and <see cref="ReadOnlySpan{T}"/> of <typeparamref name="TElementType"/>. If <typeparamref name="TElementType"/> is <see cref="bool"/>, it also includes <see cref="BitArray"/>.</para>
    /// </summary>
    /// <typeparam name="TElementType">The element type of the array.</typeparam>
    public static void AddManySerializer<TElementType>(this IDictionary<Type, IBinaryTypeParser> dict, Func<Type, IBinaryTypeParser?> parserFactory)
    {
        IBinaryTypeParser? parser = parserFactory(typeof(TElementType[]));
        if (parser != null)
            dict[typeof(TElementType[])] = parser;

        parser = parserFactory(typeof(IList<TElementType>));
        if (parser != null)
            dict[typeof(IList<TElementType>)] = parser;

        parser = parserFactory(typeof(IReadOnlyList<TElementType>));
        if (parser != null)
            dict[typeof(IReadOnlyList<TElementType>)] = parser;

        parser = parserFactory(typeof(Span<TElementType>));
        if (parser != null)
            dict[typeof(Span<TElementType>)] = parser;

        parser = parserFactory(typeof(ReadOnlySpan<TElementType>));
        if (parser != null)
            dict[typeof(ReadOnlySpan<TElementType>)] = parser;

        if (typeof(TElementType) != typeof(bool))
            return;

        parser = parserFactory(typeof(BitArray));
        if (parser != null)
            dict[typeof(BitArray)] = parser;
    }

    /*
     * Header format:
     * [ 1 byte - flags                                                                                     ] [ byte count                ] [ data...            ]
     * | 100000BB            mask   meaning                                                                 | | variable sizes, see flags | | length: byte count |
     * | ^     11 elem count 0b0011 0 = empty array, 1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     * | null                                                                                               | |                           | |                    |
     */
    internal static int GetArraySize(int length, bool isNull, int sizeOf)
    {
        byte lenFlag = GetLengthFlag(length, isNull);
        int hdrSize = GetHeaderSize(lenFlag);
        return hdrSize + length * sizeOf;
    }
    internal static unsafe int WriteStandardArrayHeader(byte* bytes, uint maxSize, ref uint index, int length, bool isNull, object parser)
    {
        byte lenFlag = GetLengthFlag(length, isNull);

        int hdrSize = GetHeaderSize(lenFlag);
        if (maxSize - index < hdrSize)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };

        bytes[index] = lenFlag;
        ++index;
        if ((lenFlag & 3) == 0)
            return 1;

        switch (lenFlag & 3)
        {
            case 1:
                bytes[index] = (byte)length;
                ++index;
                break;

            case 2:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(bytes + index, (ushort)length);
                }
                else
                {
                    bytes[index + 1] = unchecked( (byte) length );
                    bytes[index]     = unchecked( (byte)(length >>> 8) );
                }

                index += 2;
                break;

            default:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(bytes + index, length);
                }
                else
                {
                    bytes[index + 3] = unchecked( (byte) length );
                    bytes[index + 2] = unchecked( (byte)(length >>> 8) );
                    bytes[index + 1] = unchecked( (byte)(length >>> 16) );
                    bytes[index]     = unchecked( (byte)(length >>> 24) );
                }

                index += 4;
                break;
        }

        return hdrSize;
    }
    internal static int WriteStandardArrayHeader(Stream stream, int length, bool isNull)
    {
        byte lenFlag = GetLengthFlag(length, isNull);
        int hdrSize = GetHeaderSize(lenFlag);
        
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(hdrSize);
        try
        {
#else
        Span<byte> span = stackalloc byte[hdrSize];
#endif
        span[0] = lenFlag;
        switch (lenFlag & 3)
        {
            case 0:
                break;
            case 1:
                span[1] = (byte)length;
                break;

            case 2:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[1], (ushort)length);
                }
                else
                {
                    span[2] = unchecked( (byte) length );
                    span[1] = unchecked( (byte)(length >>> 8) );
                }
                break;

            default:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[1], length);
                }
                else
                {
                    span[4] = unchecked( (byte) length );
                    span[3] = unchecked( (byte)(length >>> 8) );
                    span[2] = unchecked( (byte)(length >>> 16) );
                    span[1] = unchecked( (byte)(length >>> 24) );
                }
                break;
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, hdrSize);
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
        return hdrSize;
    }
    internal static unsafe bool ReadStandardArrayHeader(byte* bytes, uint maxSize, ref uint index, out int length, object parser)
    {
        if (maxSize - index < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };

        byte lenFlag = bytes[index];
        ++index;
        if (lenFlag == 0)
        {
            length = 0;
            return true;
        }

        if ((lenFlag & 0b10000000) != 0)
        {
            length = -1;
            return false;
        }

        switch (lenFlag & 3)
        {
            case 1:
                if (maxSize < 2)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = bytes[index];
                ++index;
                break;

            case 2:
                if (maxSize < 3)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes + index)
                    : bytes[index] << 8 | bytes[index + 1];
                index += 2;
                break;

            default:
                if (maxSize < 5)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 1 };
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(bytes + index)
                    : bytes[index] << 24 | bytes[index + 1] << 16 | bytes[index + 2] << 8 | bytes[index + index + 3];
                index += 4;
                break;
        }

        return true;
    }
    internal static bool ReadStandardArrayHeader(Stream stream, out int length, out int bytesRead, object parser)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 2 };

        byte lenFlag = (byte)b;
        if (lenFlag == 0)
        {
            length = 0;
            bytesRead = 1;
            return true;
        }

        if ((lenFlag & 0b10000000) != 0)
        {
            length = -1;
            bytesRead = 1;
            return false;
        }

        int hdrSize = GetHeaderSize(lenFlag) - 1;
        
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(hdrSize);
        try
        {
#else
        Span<byte> span = stackalloc byte[hdrSize];
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        int ct = stream.Read(span, 0, hdrSize);
#else
        int ct = stream.Read(span);
#endif

        if (ct != hdrSize)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, parser.GetType().Name)) { ErrorCode = 2 };

        switch (lenFlag & 3)
        {
            case 1:
                length = span[0];
                bytesRead = 2;
                break;

            case 2:
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref span[0])
                    : span[0] << 8 | span[1];
                bytesRead = 3;
                break;

            default:
                length = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(ref span[0])
                    : span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3];
                bytesRead = 5;
                break;
        }
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        return true;
    }
    
    internal static byte GetLengthFlag(int length, bool isNull)
    {
        if (isNull)
            return 0b10000000;

        byte f = length switch
        {
            > ushort.MaxValue => 3,
            > byte.MaxValue => 2,
            0 => 0,
            _ => 1
        };
        return f;
    }

    internal static int GetHeaderSize(byte lenFlag) =>
        (lenFlag & 3) switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            _ => 5
        };

    public static void TryAdvanceStream(Stream stream, ref int bytesRead, int length)
    {
        if (!stream.CanSeek)
            return;

        try
        {
            long oldPos = stream.Position;
            long newPos = stream.Seek(length, SeekOrigin.Current);
            bytesRead += (int)(newPos - oldPos);
        }
        catch (NotSupportedException)
        {
            // ignored
        }
    }
}
