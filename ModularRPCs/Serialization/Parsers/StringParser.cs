using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class StringParser : BinaryTypeParser<string?>
{
    private readonly SerializationConfiguration _config;
    private readonly Encoding _encoding;
    private readonly Decoder _decoder;
    private readonly Encoder _encoder;
    public StringParser(SerializationConfiguration config, Encoding encoding)
    {
        _encoding = encoding;
        _config = config;
        _config.Lock();
        _decoder = encoding.GetDecoder();
        _encoder = encoding.GetEncoder();
    }

    /*
     * Header format:
     * [ 1 byte - flags                                                                                         ] [ byte count ] [ char count ] [ data...            ]
     * | 1000AABB            mask   meaning                                                                     | | variable sizes, see flags | | length: byte count |
     * | ^   11   char count 0b1100 0 = same as byte ct, 1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     * | null  11 byte count 0b0011 0 = empty string,    1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     */
    public override bool IsVariableSize => true;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(string? value, byte* bytes, uint maxSize)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (maxSize < 1)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

            *bytes = (byte)(value == null ? 0b10000000 : 0);
            return 1;
        }

        int charLen = value!.Length;

        fixed (char* ptr = value)
        {
            int size = _encoding.GetByteCount(ptr, charLen);

            byte lenFlag = GetLengthFlag(size, charLen);
            int lenSize = GetLengthSize(lenFlag);

            int ttlSize = lenSize + size + 1;
            if (maxSize < ttlSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

            *bytes = lenFlag;
            ++bytes;
            switch (lenFlag & 3)
            {
                case 1:
                    *bytes = (byte)size;
                    ++bytes;
                    break;

                case 2:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes, (ushort)size);
                    }
                    else
                    {
                        bytes[1] = unchecked((byte)size);
                        *bytes = unchecked((byte)(size >>> 8));
                    }

                    bytes += 2;
                    break;

                default:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes, size);
                    }
                    else
                    {
                        bytes[3] = unchecked((byte)size);
                        bytes[2] = unchecked((byte)(size >>> 8));
                        bytes[1] = unchecked((byte)(size >>> 16));
                        *bytes = unchecked((byte)(size >>> 24));
                    }

                    bytes += 4;
                    break;
            }

            switch ((lenFlag >> 2) & 3)
            {
                case 0:
                    break;

                case 1:
                    *bytes = (byte)charLen;
                    ++bytes;
                    break;

                case 2:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes, (ushort)charLen);
                    }
                    else
                    {
                        bytes[1] = unchecked((byte)charLen);
                        *bytes = unchecked((byte)(charLen >>> 8));
                    }

                    bytes += 2;
                    break;

                default:
                    if (BitConverter.IsLittleEndian)
                    {
                        Unsafe.WriteUnaligned(bytes, charLen);
                    }
                    else
                    {
                        bytes[3] = unchecked((byte)charLen);
                        bytes[2] = unchecked((byte)(charLen >>> 8));
                        bytes[1] = unchecked((byte)(charLen >>> 16));
                        *bytes = unchecked((byte)(charLen >>> 24));
                    }

                    bytes += 4;
                    break;
            }

            _encoding.GetBytes(ptr, charLen, bytes, size);

            return ttlSize;
        }
    }
    public override unsafe int WriteObject(string? value, Stream stream)
    {
        if (string.IsNullOrEmpty(value))
        {
            stream.WriteByte((byte)(value == null ? 0b10000000 : 0));
            return 1;
        }

        int size = _encoding.GetByteCount(value);

        int charLen = value!.Length;

        byte lenFlag = GetLengthFlag(size, charLen);
        int lenSize = GetLengthSize(lenFlag);

        int ttlSize = lenSize + size + 1;

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(lenSize + 1);
        try
        {
#else
        Span<byte> span = stackalloc byte[lenSize + 1];
#endif
        span[0] = lenFlag;
        int ind;
        switch (lenFlag & 3)
        {
            case 1:
                span[1] = (byte)size;
                ind = 2;
                break;

            case 2:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[1], (ushort)size);
                }
                else
                {
                    span[2] = unchecked((byte)size);
                    span[1] = unchecked((byte)(size >>> 8));
                }
                ind = 3;

                break;

            default:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[1], size);
                }
                else
                {
                    span[4] = unchecked((byte)size);
                    span[3] = unchecked((byte)(size >>> 8));
                    span[2] = unchecked((byte)(size >>> 16));
                    span[1] = unchecked((byte)(size >>> 24));
                }
                ind = 5;

                break;
        }

        switch ((lenFlag >> 2) & 3)
        {
            case 0:
                break;

            case 1:
                span[ind] = (byte)charLen;
                break;

            case 2:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[ind], (ushort)charLen);
                }
                else
                {
                    span[ind + 1] = unchecked((byte)charLen);
                    span[ind]     = unchecked((byte)(charLen >>> 8));
                }

                break;

            default:
                if (BitConverter.IsLittleEndian)
                {
                    Unsafe.WriteUnaligned(ref span[ind], charLen);
                }
                else
                {
                    span[ind + 3] = unchecked((byte)charLen);
                    span[ind + 2] = unchecked((byte)(charLen >>> 8));
                    span[ind + 1] = unchecked((byte)(charLen >>> 16));
                    span[ind]     = unchecked((byte)(charLen >>> 24));
                }

                break;
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, lenSize + 1);
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
#if (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER) && !NETFRAMEWORK
        if (size <= _config.MaximumStackAllocationSize)
        {
            byte* newData = stackalloc byte[size];
            fixed (char* ptr = value)
                _encoding.GetBytes(ptr, charLen, newData, size);

            stream.Write(new ReadOnlySpan<byte>(newData, size));
        }
        else
#endif
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                fixed (char* ptr = value)
                fixed (byte* newData = buffer)
                {
                    _encoding.GetBytes(ptr, charLen, newData, size);
                }

                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _config.MaximumBufferSize)
        {
            byte[] buffer = new byte[size];
            fixed (char* ptr = value)
            fixed (byte* newData = buffer)
            {
                _encoding.GetBytes(ptr, charLen, newData, size);
            }

            stream.Write(buffer, 0, size);
        }
        else
        {
            int ttlBytesUsed = 0;
            byte[] buffer = new byte[_config.MaximumBufferSize];
            fixed (char* ptr = value)
            fixed (byte* bufferPtr = buffer)
            {
                int charsLeft = charLen;
                do
                {
                    _encoder.Convert(ptr + (charLen - charsLeft), charsLeft, bufferPtr, buffer.Length,
                        false, out int charsUsed, out int bytesUsed, out bool completed);
                    charsLeft -= charsUsed;
                    stream.Write(buffer, 0, bytesUsed);
                    ttlBytesUsed += bytesUsed;
                    if (completed)
                        break;
                } while (charsLeft > 0);
            }

            return lenSize + ttlBytesUsed + 1;
        }

        return ttlSize;
    }
    public override unsafe string? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

        byte lenFlag = (byte)(*bytes & 0b10001111);
        if (lenFlag == 0)
        {
            bytesRead = 1;
            return string.Empty;
        }
        if ((lenFlag & 0b10000000) != 0)
        {
            bytesRead = 1;
            return null;
        }

        int size;
        int hdrSize;
        switch (lenFlag & 3)
        {
            case 1:
                if (maxSize < 2)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                size = bytes[1];
                hdrSize = 2;
                break;

            case 2:
                if (maxSize < 3)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                size = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<ushort>(bytes + 1)
                        : bytes[1] << 8 | bytes[2];
                hdrSize = 3;
                break;

            default:
                if (maxSize < 5)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                size = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(bytes + 1)
                    : bytes[1] << 24 | bytes[2] << 16 | bytes[3] << 8 | bytes[4];
                hdrSize = 5;
                break;
        }

        maxSize -= (uint)hdrSize;
        bytes += hdrSize;

        int charLen;
        int charHdrSize;
        switch ((lenFlag >> 2) & 3)
        {
            case 0:
                charLen = size;
                charHdrSize = 0;
                break;

            case 1:
                if (maxSize < 1)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                charLen = bytes[0];
                charHdrSize = 1;
                break;

            case 2:
                if (maxSize < 2)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes)
                    : bytes[0] << 8 | bytes[1];
                charHdrSize = 2;
                break;

            default:
                if (maxSize < 4)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(bytes)
                    : bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
                charHdrSize = 4;
                break;
        }

        maxSize -= (uint)charHdrSize;
        bytes += charHdrSize;

        _config.AssertCanCreateArrayOfType(typeof(string), charLen, this);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        string str;
        try
        {
            ReadStringBytesContext ctx = default;
            ctx.Encoding = _encoding;
            ctx.Size = size;
            ctx.Bytes = bytes;
            str = string.Create(charLen, ctx, static (span, state) =>
            {
                state.Encoding.GetChars(new ReadOnlySpan<byte>(state.Bytes, state.Size), span);
            });
        }
        catch (ArgumentException)
        {
            str = _encoding.GetString(bytes, size);
        }
#else
        string str = _encoding.GetString(bytes, size);
#endif
        bytesRead = size + hdrSize + charHdrSize;
        return str;
    }
    // ReSharper disable once RedundantUnsafeContext
    public override unsafe string? ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

        byte lenFlag = (byte)((byte)b & 0b10001111);
        if (lenFlag == 0)
        {
            bytesRead = 1;
            return string.Empty;
        }
        if ((lenFlag & 0b10000000) != 0)
        {
            bytesRead = 1;
            return null;
        }


        int hdrSize = GetLengthSize(lenFlag);
        int size;
        int charLen;
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
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

        int ind;
        switch (lenFlag & 3)
        {
            case 1:
                size = span[0];
                ind = 1;
                break;

            case 2:
                size = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref span[0])
                    : span[0] << 8 | span[1];
                ind = 2;
                break;

            default:
                size = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(ref span[0])
                    : span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3];
                ind = 4;
                break;
        }

        switch ((lenFlag >> 2) & 3)
        {
            case 0:
                charLen = size;
                break;
            case 1:
                charLen = span[ind];
                break;

            case 2:
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref span[ind])
                    : span[ind] << 8 | span[ind + 1];
                break;

            default:
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(ref span[ind])
                    : span[ind] << 24 | span[ind + 1] << 16 | span[ind + 2] << 8 | span[ind + 3];
                break;
        }

        _config.AssertCanCreateArrayOfType(typeof(string), charLen, this);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        string str;
        if (size > _config.MaximumStackAllocationSize)
        {
            try
            {
                ReadStringStreamContext ctx = default;
                ctx.Stream = stream;
                ctx.Parser = this;
                ctx.ByteSize = size;
                ctx.MaxBufferSize = _config.MaximumBufferSize;
                str = string.Create(charLen, ctx, static (span, state) =>
                {
                    if (state.ByteSize <= DefaultSerializer.MaxArrayPoolSize)
                    {
                        byte[] buffer = DefaultSerializer.ArrayPool.Rent(state.ByteSize);
                        try
                        {
                            int ct = state.Stream.Read(buffer, 0, state.ByteSize);
                            if (ct != state.ByteSize)
                                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, state.Parser.GetType().Name)) { ErrorCode = 2 };

                            state.Parser._encoding.GetChars(buffer.AsSpan(0, state.ByteSize), span);
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(buffer);
                        }
                    }
                    else if (state.ByteSize <= state.MaxBufferSize)
                    {
                        byte[] buffer = new byte[state.ByteSize];
                        int ct = state.Stream.Read(buffer, 0, state.ByteSize);
                        if (ct != state.ByteSize)
                            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, state.Parser.GetType().Name)) { ErrorCode = 2 };

                        state.Parser._encoding.GetChars(buffer.AsSpan(0, state.ByteSize), span);
                    }
                    else
                    {
                        byte[] buffer = new byte[state.MaxBufferSize];
                        int bytesLeft = state.ByteSize;
                        int charsLeft = span.Length;
                        int keepDataOffset = 0;
                        do
                        {
                            int sizeToRead = Math.Min(buffer.Length - keepDataOffset, bytesLeft);
                            int ct = state.Stream.Read(buffer, keepDataOffset, sizeToRead);
                            if (ct != sizeToRead)
                                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, state.Parser.GetType().Name)) { ErrorCode = 2 };

                            state.Parser._decoder.Convert(buffer.AsSpan(0, sizeToRead), span.Slice(span.Length - charsLeft),
                                bytesLeft - sizeToRead <= 0, out int bytesUsed, out int charsUsed,
                                out _);
                            bytesLeft -= bytesUsed;
                            charsLeft -= charsUsed;
                            if (bytesUsed < sizeToRead)
                            {
                                keepDataOffset = buffer.Length - bytesUsed;
                                Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, keepDataOffset);
                            }
                            else keepDataOffset = 0;
                        } while (bytesLeft > 0 && charsLeft > 0);
                    }
                });
            }
            catch (ArgumentException)
            {
                char[] chars = new char[charLen];
                using StreamReader reader = new StreamReader(stream, _encoding, false, Math.Min(size, _config.MaximumBufferSize), leaveOpen: true);
                int actualChars = reader.Read(chars, 0, charLen);
                str = new string(chars, 0, actualChars);
            }
        }
        else
        {
            byte* dataPtr = stackalloc byte[size];
            Span<byte> dataSpan = new Span<byte>(dataPtr, size);
            ct = stream.Read(dataSpan);
            if (ct != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

            try
            {
                ReadStringBytesContext ctx = default;
                ctx.Bytes = dataPtr;
                ctx.Size = size;
                ctx.Encoding = _encoding;
                str = string.Create(charLen, ctx, static (span, state) =>
                {
                    fixed (char* ptr = span)
                        state.Encoding.GetChars(state.Bytes, state.Size, ptr, span.Length);
                });
            }
            catch (ArgumentException)
            {
                str = _encoding.GetString(dataSpan);
            }
        }
#else
        string str;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] data = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                int ct2 = stream.Read(data, 0, size);
                if (ct2 != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

                str = _encoding.GetString(data, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(data);
            }
        }
        else if (size <= _config.MaximumBufferSize)
        {
            byte[] data = new byte[size];
            int ct2 = stream.Read(data, 0, size);
            if (ct2 != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

            str = _encoding.GetString(data, 0, size);
        }
        else
        {
            char[] charBuffer = new char[charLen];
            byte[] buffer = new byte[_config.MaximumBufferSize];
            int bytesLeft = size;
            int charsLeft = charLen;
            int keepDataOffset = 0;
            fixed (char* ptr = charBuffer)
            fixed (byte* bufferPtr = buffer)
            {
                do
                {
                    int sizeToRead = Math.Min(buffer.Length - keepDataOffset, bytesLeft);
                    int ct = stream.Read(buffer, keepDataOffset, sizeToRead);
                    if (ct != sizeToRead)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

                    _decoder.Convert(bufferPtr, sizeToRead, ptr + (charLen - charsLeft), charsLeft,
                        bytesLeft - sizeToRead <= 0, out int bytesUsed, out int charsUsed,
                        out _);
                    bytesLeft -= bytesUsed;
                    charsLeft -= charsUsed;
                    if (bytesUsed < sizeToRead)
                    {
                        keepDataOffset = buffer.Length - bytesUsed;
                        Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, keepDataOffset);
                    }
                    else keepDataOffset = 0;
                } while (bytesLeft > 0 && charsLeft > 0);
            }

            str = new string(charBuffer, 0, charLen - charsLeft);
        }
        
#endif

        bytesRead = size + hdrSize + 1;
        return str;
    }
    internal static byte GetLengthFlag(int size, int charLen)
    {
        byte f = size switch
        {
            > ushort.MaxValue => 3,
            > byte.MaxValue => 2,
            0 => 0,
            _ => 1
        };
        if (size != charLen)
        {
            f |= charLen switch
            {
                > ushort.MaxValue => 0b1100,
                > byte.MaxValue => 0b1000,
                0 => 0,
                _ => 0b0100
            };
        }
        return f;
    }

    internal static int GetLengthSize(byte lenFlag) =>
    (lenFlag & 3) switch
    {
        0 => 0,
        1 => 1,
        2 => 2,
        _ => 4
    } + ((lenFlag >> 2) & 3) switch
    {
        0 => 0,
        1 => 1,
        2 => 2,
        _ => 4
    };
    public override int GetSize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 1;

        int byteCt = _encoding.GetByteCount(value);
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        byte flag = GetLengthFlag(byteCt, value!.Length);
        return 1 + GetLengthSize(flag) + byteCt;
    }
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private struct ReadStringStreamContext
    {
        public Stream Stream;
        public int ByteSize;
        public int MaxBufferSize;
        public StringParser Parser;
    }
    private struct ReadStringBytesContext
    {
        public unsafe byte* Bytes;
        public int Size;
        public Encoding Encoding;
    }
#endif

    public unsafe class Many : IArrayBinaryTypeParser<string?>
    {
        private static readonly Type ArrType = typeof(string?[]);
        private static readonly Type ListType = typeof(IList<string?>);
        private static readonly Type RoListType = typeof(IReadOnlyList<string?>);
        private static readonly Type EnumerableType = typeof(IEnumerable<string?>);
        private static readonly Type CollectionType = typeof(ICollection<string?>);
        private static readonly Type RoCollectionType = typeof(IReadOnlyCollection<string?>);
        private static readonly Type ArrSegmentType = typeof(ArraySegment<string?>);
        private static readonly Type SpanType = typeof(Span<string?>);
        private static readonly Type RoSpanType = typeof(ReadOnlySpan<string?>);
        private static readonly Type SpanPtrType = typeof(Span<string?>*);
        private static readonly Type RoSpanPtrType = typeof(ReadOnlySpan<string?>*);
        private readonly SerializationConfiguration _config;
        private readonly StringParser _stringParser;
        public Many(SerializationConfiguration config, Encoding encoding)
        {
            _config = config;
            _config.Lock();
            _stringParser = new StringParser(config, encoding);
        }

        /// <inheritdoc />
        public bool IsVariableSize => true;

        /// <inheritdoc />
        public int MinimumSize => 1;
        protected static ArraySegment<string?> EmptySegment()
        {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return ArraySegment<string?>.Empty;
#else
            return new ArraySegment<string?>(Array.Empty<string?>());
#endif
        }
        protected static void ResetOrReMake(ref IEnumerator<string?> enumerator, IEnumerable<string?> enumerable)
        {
            try
            {
                enumerator.Reset();
            }
            catch (NotSupportedException)
            {
                enumerator.Dispose();
                enumerator = enumerable.GetEnumerator();
            }
        }

        /// <inheritdoc />
        public int ReadArrayLength(byte* bytes, uint maxSize, out int bytesRead)
        {
            uint index = 0;
            SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this);
            bytesRead = (int)index;
            return length;
        }

        /// <inheritdoc />
        public int ReadArrayLength([InstantHandle] Stream stream, out int bytesRead)
        {
            SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this);
            return length;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] string?[]? value)
        {
            if (value == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Length, false));
            int ttlLen = 0;
            for (int i = 0; i < value.Length; ++i)
            {
                string? val = value[i];
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] ArraySegment<string?> value)
        {
            if (value.Array == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Count, false));
            int ttlLen = 0;
            int ofs = value.Offset;
            int ct = value.Count + ofs;
            string[] arr = value.Array!;
            for (int i = ofs; i < ct; ++i)
            {
                string? val = arr[i];
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] scoped ReadOnlySpan<string?> value)
        {
            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Length, false));
            int ttlLen = 0;
            for (int i = 0; i < value.Length; ++i)
            {
                string? val = value[i];
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] IList<string?>? value)
        {
            if (value == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Count, false));
            int ttlLen = 0;
            foreach (string? val in value)
            {
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] IReadOnlyList<string?>? value)
        {
            if (value == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Count, false));
            int ttlLen = 0;
            foreach (string? val in value)
            {
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] ICollection<string?>? value)
        {
            if (value == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Count, false));
            int ttlLen = 0;
            foreach (string? val in value)
            {
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] IReadOnlyCollection<string?>? value)
        {
            if (value == null)
                return 1;

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(value.Count, false));
            int ttlLen = 0;
            foreach (string? val in value)
            {
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            return size + ttlLen;
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] IEnumerable<string?>? value)
        {
            if (value == null)
                return 1;

            int ttlLen = 0;
            int ct = 0;
            foreach (string? val in value)
            {
                ++ct;
                if (val == null)
                {
                    ++ttlLen;
                    continue;
                }

                ttlLen += _stringParser.GetSize(val);
            }

            int size = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(ct, false));
            return size + ttlLen;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ArraySegment<string?> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            int ofs = value.Offset;
            length += ofs;
            string?[] arr = value.Array!; 

            for (int i = ofs; i < length; ++i)
            {
                string? val = arr[i];
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
            }

            return (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] scoped ReadOnlySpan<string?> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            for (int i = 0; i < length; ++i)
            {
                string? val = value[i];
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
            }

            return (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IList<string?>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            int actualCount = 0;
            foreach (string? val in value)
            {
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
                ++actualCount;
            }

            if (length == actualCount)
                return (int)index;

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (newHdrSize == hdrSize)
            {
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);
                return (int)index;
            }

            if (hdrSize < newHdrSize && maxSize - newHdrSize < index - hdrSize)
            {
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
            }
            int sz = (int)index - hdrSize;
            if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, maxSize - newHdrSize, sz);
            }
            else if (hdrSize > newHdrSize)
            {
                for (int i = 0; i < sz; ++i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }
            else
            {
                for (int i = sz - 1; i >= 0; --i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);

            return sz + newHdrSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyList<string?>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            int actualCount = 0;
            foreach (string? val in value)
            {
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
                ++actualCount;
            }

            if (length == actualCount)
                return (int)index;

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (newHdrSize == hdrSize)
            {
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);
                return (int)index;
            }

            if (hdrSize < newHdrSize && maxSize - newHdrSize < index - hdrSize)
            {
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
            }
            int sz = (int)index - hdrSize;
            if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, maxSize - newHdrSize, sz);
            }
            else if (hdrSize > newHdrSize)
            {
                for (int i = 0; i < sz; ++i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }
            else
            {
                for (int i = sz - 1; i >= 0; --i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);

            return sz + newHdrSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ICollection<string?>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            int actualCount = 0;
            foreach (string? val in value)
            {
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
                ++actualCount;
            }

            if (length == actualCount)
                return (int)index;

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (newHdrSize == hdrSize)
            {
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);
                return (int)index;
            }

            if (hdrSize < newHdrSize && maxSize - newHdrSize < index - hdrSize)
            {
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
            }
            int sz = (int)index - hdrSize;
            if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, maxSize - newHdrSize, sz);
            }
            else if (hdrSize > newHdrSize)
            {
                for (int i = 0; i < sz; ++i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }
            else
            {
                for (int i = sz - 1; i >= 0; --i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);

            return sz + newHdrSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyCollection<string?>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            int actualCount = 0;
            foreach (string? val in value)
            {
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
                ++actualCount;
            }

            if (length == actualCount)
                return (int)index;

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (newHdrSize == hdrSize)
            {
                SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);
                return (int)index;
            }

            if (hdrSize < newHdrSize && maxSize - newHdrSize < index - hdrSize)
            {
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
            }
            int sz = (int)index - hdrSize;
            if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, maxSize - newHdrSize, sz);
            }
            else if (hdrSize > newHdrSize)
            {
                for (int i = 0; i < sz; ++i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }
            else
            {
                for (int i = sz - 1; i >= 0; --i)
                {
                    bytes[i + newHdrSize] = bytes[i + hdrSize];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);

            return sz + newHdrSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IEnumerable<string?>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int actualCount = 0;
            foreach (string? val in value)
            {
                index += (uint)_stringParser.WriteObject(val, bytes + index, maxSize - index);
                ++actualCount;
            }

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            
            if (maxSize - newHdrSize < index)
            {
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
            }
            int sz = (int)index;
            if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes, bytes + newHdrSize, maxSize - newHdrSize, sz);
            }
            else
            {
                for (int i = sz - 1; i >= 0; --i)
                {
                    bytes[i + newHdrSize] = bytes[i];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);

            return sz + newHdrSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] scoped ReadOnlySpan<string?> value, Stream stream)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            for (int i = 0; i < length; ++i)
            {
                string? val = value[i];
                index += (uint)_stringParser.WriteObject(val, stream);
            }

            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IList<string?>? value, Stream stream)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            for (int i = 0; i < length; ++i)
            {
                string? val = value[i];
                index += (uint)_stringParser.WriteObject(val, stream);
            }

            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyList<string?>? value, Stream stream)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            for (int i = 0; i < length; ++i)
            {
                string? val = value[i];
                index += (uint)_stringParser.WriteObject(val, stream);
            }

            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ICollection<string?>? value, Stream stream)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            using (IEnumerator<string?> enumerator = value.GetEnumerator())
            {
                int actualCount = 0;
                while (actualCount < length && enumerator.MoveNext())
                {
                    string? val = enumerator.Current;
                    index += (uint)_stringParser.WriteObject(val, stream);
                    ++actualCount;
                }
            }

            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyCollection<string?>? value, Stream stream)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            using (IEnumerator<string?> enumerator = value.GetEnumerator())
            {
                int actualCount = 0;
                while (actualCount < length && enumerator.MoveNext())
                {
                    string? val = enumerator.Current;
                    index += (uint)_stringParser.WriteObject(val, stream);
                    ++actualCount;
                }
            }

            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IEnumerable<string?>? value, Stream stream)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            IEnumerator<string?> enumerator = value.GetEnumerator();
            try
            {
                int length = 0;
                while (enumerator.MoveNext())
                    checked { ++length; }

                int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);
                
                if (length == 0)
                    return hdrSize;

                ResetOrReMake(ref enumerator, value);

                int actualCount = 0;
                while (actualCount < length && enumerator.MoveNext())
                {
                    string? val = enumerator.Current;
                    index += (uint)_stringParser.WriteObject(val, stream);
                    ++actualCount;
                }

                return hdrSize + (int)index;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ArraySegment<string?> value, Stream stream)
        {
            uint index = 0;
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int ofs = value.Offset;
            string?[] arr = value.Array!;
            length += ofs;
            for (int i = ofs; i < length; ++i)
            {
                string? val = arr[i];
                index += (uint)_stringParser.WriteObject(val, stream);
            }
            
            return hdrSize + (int)index;
        }

        /// <inheritdoc />
        public string?[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = 1;
                return null;
            }

            if (length == 0)
            {
                bytesRead = (int)index;
                return Array.Empty<string>();
            }

            _config.AssertCanCreateArrayOfType(null, length, this);

            string?[] arr = new string[length];
            for (int i = 0; i < length; ++i)
            {
                arr[i] = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;
            return arr;
        }

        /// <inheritdoc />
        public string?[]? ReadObject(Stream stream, out int bytesRead)
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                return null;

            uint index = (uint)bytesRead;
            if (length == 0)
                return Array.Empty<string>();

            _config.AssertCanCreateArrayOfType(null, length, this);

            string?[] arr = new string[length];
            for (int i = 0; i < length; ++i)
            {
                arr[i] = _stringParser.ReadObject(stream, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;
            return arr;
        }

        /// <inheritdoc />
        public int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<string?> output, out int bytesRead, bool hasReadLength = true)
        {
            uint index;
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Length)
                {
                    index = (uint)bytesRead;
                    for (int i = 0; i < length; ++i)
                    {
                        _ = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                        index += (uint)bytesRead2;
                    }
                    bytesRead = (int)index;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            index = (uint)bytesRead;
            for (int i = 0; i < length; ++i)
            {
                output[i] = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;

            return length;
        }

        /// <inheritdoc />
        public int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<string?> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Count;
            uint index;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    index = (uint)bytesRead;
                    for (int i = 0; i < length; ++i)
                    {
                        _ = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                        index += (uint)bytesRead2;
                    }
                    bytesRead = (int)index;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
                    
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            index = (uint)bytesRead;
            string?[] arr = output.Array!;
            int ofs = output.Offset;
            length += ofs;
            for (int i = ofs; i < length; ++i)
            {
                arr[i] = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;

            return length - ofs;
        }

        /// <inheritdoc />
        public int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<string?> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
        {
            int length = setInsteadOfAdding ? output.Count : measuredCount;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (setInsteadOfAdding && length > output.Count)
                {
                    if (output.IsReadOnly)
                        throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

                    do
                        output.Add(null);
                    while (length > output.Count);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    if (measuredCount > output.Count)
                    {
                        if (output.IsReadOnly)
                            throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

                        do
                            output.Add(null);
                        while (measuredCount > output.Count);
                    }
                    length = measuredCount;
                }
            }

            if (!setInsteadOfAdding)
            {
                if (output.IsReadOnly)
                    throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }

            if (length <= 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            bytes += bytesRead;

            string?[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is string?[] arr1)
            {
                arr = arr1;
            }
            else if (output is List<string?> list)
            {
                if (!setInsteadOfAdding)
                    arrOffset = list.Count;
                if (list.Capacity < arrOffset + length)
                    list.Capacity = arrOffset + length;

                if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                    arr = null;
            }

            uint index = 0;
            if (arr != null)
            {
                length += arrOffset;
                for (int i = arrOffset; i < length; ++i)
                {
                    output[i] = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                    index += (uint)bytesRead2;
                }

                bytesRead += (int)index;

                return length - arrOffset;
            }

            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = _stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2);
                    index += (uint)bytesRead2;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(_stringParser.ReadObject(bytes + index, maxSize - index, out int bytesRead2));
                    index += (uint)bytesRead2;
                }
            }

            bytesRead += (int)index;

            return length;
        }
        
        /// <inheritdoc />
        public int ReadObject(Stream stream, [InstantHandle] scoped Span<string?> output, out int bytesRead, bool hasReadLength = true)
        {
            uint index;
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Length)
                {
                    index = (uint)bytesRead;
                    for (int i = 0; i < length; ++i)
                    {
                        _ = _stringParser.ReadObject(stream, out int bytesRead2);
                        index += (uint)bytesRead2;
                    }

                    bytesRead = (int)index;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            index = (uint)bytesRead;
            for (int i = 0; i < length; ++i)
            {
                output[i] = _stringParser.ReadObject(stream, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;

            return length;
        }

        /// <inheritdoc />
        public int ReadObject(Stream stream, [InstantHandle] ArraySegment<string?> output, out int bytesRead, bool hasReadLength = true)
        {
            uint index;
            int length = output.Count;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    index = (uint)bytesRead;
                    for (int i = 0; i < length; ++i)
                    {
                        _ = _stringParser.ReadObject(stream, out int bytesRead2);
                        index += (uint)bytesRead2;
                    }

                    bytesRead = (int)index;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            index = (uint)bytesRead;
            string?[] arr = output.Array!;
            int ofs = output.Offset;
            length += ofs;
            for (int i = ofs; i < length; ++i)
            {
                arr[i] = _stringParser.ReadObject(stream, out int bytesRead2);
                index += (uint)bytesRead2;
            }

            bytesRead = (int)index;

            return length - ofs;
        }
        
        /// <inheritdoc />
        public int ReadObject(Stream stream, [InstantHandle] IList<string?> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
        {
            int length = setInsteadOfAdding ? output.Count : measuredCount;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (setInsteadOfAdding && length > output.Count)
                {
                    if (output.IsReadOnly)
                        throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

                    do
                        output.Add(null);
                    while (length > output.Count);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    if (measuredCount > output.Count)
                    {
                        if (output.IsReadOnly)
                            throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

                        do
                            output.Add(null);
                        while (measuredCount > output.Count);
                    }
                    length = measuredCount;
                }
            }

            if (!setInsteadOfAdding)
            {
                if (output.IsReadOnly)
                    throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }

            if (length <= 0)
                return 0;

            _config.AssertCanCreateArrayOfType(null, length, this);

            string?[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is string?[] arr1)
            {
                arr = arr1;
            }
            else if (output is List<string?> list)
            {
                if (!setInsteadOfAdding)
                    arrOffset = list.Count;
                if (list.Capacity < arrOffset + length)
                    list.Capacity = arrOffset + length;

                if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                    arr = null;
            }

            uint index = 0;
            if (arr != null)
            {
                length += arrOffset;
                for (int i = arrOffset; i < length; ++i)
                {
                    output[i] = _stringParser.ReadObject(stream, out int bytesRead2);
                    index += (uint)bytesRead2;
                }

                bytesRead += (int)index;

                return length - arrOffset;
            }

            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = _stringParser.ReadObject(stream, out int bytesRead2);
                    index += (uint)bytesRead2;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(_stringParser.ReadObject(stream, out int bytesRead2));
                    index += (uint)bytesRead2;
                }
            }

            bytesRead += (int)index;

            return length;
        }

        /// <inheritdoc />
        IList<string?>? IBinaryTypeParser<IList<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyList<string?>? IBinaryTypeParser<IReadOnlyList<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        ICollection<string?>? IBinaryTypeParser<ICollection<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyCollection<string?>? IBinaryTypeParser<IReadOnlyCollection<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        IEnumerable<string?>? IBinaryTypeParser<IEnumerable<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadObject(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        ArraySegment<string?> IBinaryTypeParser<ArraySegment<string?>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            string?[]? arr = ReadObject(bytes, maxSize, out bytesRead);
            return arr == null ? default : new ArraySegment<string?>(arr);
        }

        /// <inheritdoc />
        IList<string?>? IBinaryTypeParser<IList<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadObject(stream, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyList<string?>? IBinaryTypeParser<IReadOnlyList<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadObject(stream, out bytesRead);
        }

        /// <inheritdoc />
        ICollection<string?>? IBinaryTypeParser<ICollection<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadObject(stream, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyCollection<string?>? IBinaryTypeParser<IReadOnlyCollection<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadObject(stream, out bytesRead);
        }

        /// <inheritdoc />
        IEnumerable<string?>? IBinaryTypeParser<IEnumerable<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadObject(stream, out bytesRead);
        }

        /// <inheritdoc />
        ArraySegment<string?> IBinaryTypeParser<ArraySegment<string?>>.ReadObject(Stream stream, out int bytesRead)
        {
            string?[]? arr = ReadObject(stream, out bytesRead);
            return arr == null ? default : new ArraySegment<string?>(arr);
        }

        /// <inheritdoc />
        public int GetSize(object? value)
        {
            return value switch
            {
                string?[] arr => GetSize(arr),
                ArraySegment<string?> arrSeg => GetSize(arrSeg),
                IList<string?> list => GetSize(list),
                IReadOnlyList<string?> list => GetSize(list),
                ICollection<string?> collection => GetSize(collection),
                IReadOnlyCollection<string?> collection => GetSize(collection),
                IEnumerable<string?> enu => GetSize(enu),
                null => 1,
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }
        
        /// <inheritdoc />
        public int WriteObject(object? value, byte* bytes, uint maxSize)
        {
            return value switch
            {
                string?[] arr => WriteObject(new ArraySegment<string?>(arr), bytes, maxSize),
                ArraySegment<string?> arrSeg => WriteObject(arrSeg, bytes, maxSize),
                IList<string?> list => WriteObject(list, bytes, maxSize),
                IReadOnlyList<string?> list => WriteObject(list, bytes, maxSize),
                ICollection<string?> collection => WriteObject(collection, bytes, maxSize),
                IReadOnlyCollection<string?> collection => WriteObject(collection, bytes, maxSize),
                IEnumerable<string?> enu => WriteObject(enu, bytes, maxSize),
                null => WriteObject((IList<string?>?)null, bytes, maxSize),
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }
        
        /// <inheritdoc />
        public int WriteObject(object? value, Stream stream)
        {
            return value switch
            {
                string?[] arr => WriteObject(new ArraySegment<string?>(arr), stream),
                ArraySegment<string?> arrSeg => WriteObject(arrSeg, stream),
                IList<string?> list => WriteObject(list, stream),
                IReadOnlyList<string?> list => WriteObject(list, stream),
                ICollection<string?> collection => WriteObject(collection, stream),
                IReadOnlyCollection<string?> collection => WriteObject(collection, stream),
                IEnumerable<string?> enu => WriteObject(enu, stream),
                null => WriteObject((IList<string?>?)null, stream),
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }
        
        /// <inheritdoc />
        public object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
        {
            if (type == ArrType || type == ListType || type == RoListType)
                return ReadObject(bytes, maxSize, out bytesRead);

            if (type == ArrSegmentType)
            {
                string?[]? arr = ReadObject(bytes, maxSize, out bytesRead);
                return arr == null ? EmptySegment() : new ArraySegment<string?>(arr);
            }

            if (type.IsAssignableFrom(ArrType))
            {
                return ReadObject(bytes, maxSize, out bytesRead);
            }

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public object? ReadObject(Type type, Stream stream, out int bytesRead)
        {
            if (type == ArrType || type == ListType || type == RoListType)
                return ReadObject(stream, out bytesRead);

            if (type == ArrSegmentType)
            {
                string?[]? arr = ReadObject(stream, out bytesRead);
                return arr == null ? EmptySegment() : new ArraySegment<string?>(arr);
            }

            if (type.IsAssignableFrom(ArrType))
            {
                return ReadObject(stream, out bytesRead);
            }

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public int WriteObject([InstantHandle] string?[]? value, byte* bytes, uint maxSize)
        {
            if (value == null)
            {
                uint index = 0;
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            return WriteObject(new ArraySegment<string?>(value, 0, value.Length), bytes, maxSize);
        }
       
        /// <inheritdoc />
        public int WriteObject([InstantHandle] string?[]? value, Stream stream)
        {
            if (value == null)
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);

            return WriteObject(new ArraySegment<string?>(value, 0, value.Length), stream);
        }
        
        /// <inheritdoc />
        public int WriteObject(TypedReference value, byte* bytes, uint maxSize)
        {
            Type t = __reftype(value);
            if (t == ArrType)
                return WriteObject(__refvalue(value, string?[]?), bytes, maxSize);
            if (t == ListType)
                return WriteObject(__refvalue(value, IList<string?>?), bytes, maxSize);
            if (t == RoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<string?>?), bytes, maxSize);
            if (t == ArrSegmentType)
                return WriteObject(__refvalue(value, ArraySegment<string?>), bytes, maxSize);
            if (t == RoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<string?>), bytes, maxSize);
            if (t == EnumerableType)
                return WriteObject(__refvalue(value, IEnumerable<string?>?), bytes, maxSize);
            if (t == CollectionType)
                return WriteObject(__refvalue(value, ICollection<string?>?), bytes, maxSize);
            if (t == RoCollectionType)
                return WriteObject(__refvalue(value, IReadOnlyCollection<string?>?), bytes, maxSize);
            if (t == SpanType)
                return WriteObject(__refvalue(value, Span<string?>), bytes, maxSize);
            if (t == RoSpanPtrType)
                return WriteObject(*__refvalue(value, ReadOnlySpan<string?>*), bytes, maxSize);
            if (t == SpanPtrType)
                return WriteObject(*__refvalue(value, Span<string?>*), bytes, maxSize);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public int WriteObject(TypedReference value, Stream stream)
        {
            Type t = __reftype(value);
            if (t == ArrType)
                return WriteObject(__refvalue(value, string?[]?), stream);
            if (t == ListType)
                return WriteObject(__refvalue(value, IList<string?>?), stream);
            if (t == RoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<string?>?), stream);
            if (t == ArrSegmentType)
                return WriteObject(__refvalue(value, ArraySegment<string?>), stream);
            if (t == RoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<string?>), stream);
            if (t == EnumerableType)
                return WriteObject(__refvalue(value, IEnumerable<string?>?), stream);
            if (t == CollectionType)
                return WriteObject(__refvalue(value, ICollection<string?>?), stream);
            if (t == RoCollectionType)
                return WriteObject(__refvalue(value, IReadOnlyCollection<string?>?), stream);
            if (t == SpanType)
                return WriteObject(__refvalue(value, Span<string?>), stream);
            if (t == RoSpanPtrType)
                return WriteObject(*__refvalue(value, ReadOnlySpan<string?>*), stream);
            if (t == SpanPtrType)
                return WriteObject(*__refvalue(value, Span<string?>*), stream);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public void ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj)
        {
            Type t = __reftype(outObj);
            if (t == ArrType)
                __refvalue(outObj, string?[]?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == ListType)
                __refvalue(outObj, IList<string?>?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == RoListType)
                __refvalue(outObj, IReadOnlyList<string?>?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == ArrSegmentType)
            {
                string?[]? arr = ReadObject(bytes, maxSize, out bytesRead);
                __refvalue(outObj, ArraySegment<string?>) = arr == null ? EmptySegment() : new ArraySegment<string?>(arr);
            }
            else if (t == RoSpanType)
                __refvalue(outObj, ReadOnlySpan<string?>) = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
            else if (t == EnumerableType)
                __refvalue(outObj, IEnumerable<string?>?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == CollectionType)
                __refvalue(outObj, ICollection<string?>?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == RoCollectionType)
                __refvalue(outObj, IReadOnlyCollection<string?>?) = ReadObject(bytes, maxSize, out bytesRead);
            else if (t == SpanType)
            {
                ref Span<string?> existingSpan = ref __refvalue(outObj, Span<string?>);
                if (!existingSpan.IsEmpty)
                {
                    ReadObject(bytes, maxSize, existingSpan, out bytesRead, false);
                }
                else
                {
                    existingSpan = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
                }
            }
            else if (t == RoSpanPtrType)
                *__refvalue(outObj, ReadOnlySpan<string?>*) = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
            else if (t == SpanPtrType)
            {
                Span<string?>* existingSpan = __refvalue(outObj, Span<string?>*);
                if (!existingSpan->IsEmpty)
                {
                    ReadObject(bytes, maxSize, *existingSpan, out bytesRead, false);
                }
                else
                {
                    *existingSpan = ReadObject(bytes, maxSize, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public void ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
        {
            Type t = __reftype(outObj);
            if (t == ArrType)
                __refvalue(outObj, string?[]?) = ReadObject(stream, out bytesRead);
            else if (t == ListType)
                __refvalue(outObj, IList<string?>?) = ReadObject(stream, out bytesRead);
            else if (t == RoListType)
                __refvalue(outObj, IReadOnlyList<string?>?) = ReadObject(stream, out bytesRead);
            else if (t == ArrSegmentType)
            {
                string?[]? arr = ReadObject(stream, out bytesRead);
                __refvalue(outObj, ArraySegment<string?>) = arr == null ? EmptySegment() : new ArraySegment<string?>(arr);
            }
            else if (t == RoSpanType)
                __refvalue(outObj, ReadOnlySpan<string?>) = ReadObject(stream, out bytesRead).AsSpan();
            else if (t == EnumerableType)
                __refvalue(outObj, IEnumerable<string?>?) = ReadObject(stream, out bytesRead);
            else if (t == CollectionType)
                __refvalue(outObj, ICollection<string?>?) = ReadObject(stream, out bytesRead);
            else if (t == RoCollectionType)
                __refvalue(outObj, IReadOnlyCollection<string?>?) = ReadObject(stream, out bytesRead);
            else if (t == SpanType)
            {
                ref Span<string?> existingSpan = ref __refvalue(outObj, Span<string?>);
                if (!existingSpan.IsEmpty)
                {
                    ReadObject(stream, existingSpan, out bytesRead, false);
                }
                else
                {
                    existingSpan = ReadObject(stream, out bytesRead).AsSpan();
                }
            }
            else if (t == RoSpanPtrType)
                *__refvalue(outObj, ReadOnlySpan<string?>*) = ReadObject(stream, out bytesRead).AsSpan();
            else if (t == SpanPtrType)
            {
                Span<string?>* existingSpan = __refvalue(outObj, Span<string?>*);
                if (!existingSpan->IsEmpty)
                {
                    ReadObject(stream, *existingSpan, out bytesRead, false);
                }
                else
                {
                    *existingSpan = ReadObject(stream, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
        
        /// <inheritdoc />
        public int GetSize(TypedReference value)
        {
            Type t = __reftype(value);
            if (t == ArrType)
            {
                return GetSize(__refvalue(value, string?[]?));
            }
            if (t == ListType)
            {
                return GetSize(__refvalue(value, IList<string?>?));
            }
            if (t == RoListType)
            {
                return GetSize(__refvalue(value, IReadOnlyList<string?>?));
            }
            if (t == ArrSegmentType)
            {
                return GetSize(__refvalue(value, ArraySegment<string?>));
            }
            if (t == RoSpanType)
            {
                return GetSize(__refvalue(value, ReadOnlySpan<string?>));
            }
            if (t == EnumerableType)
            {
                return GetSize(__refvalue(value, IEnumerable<string?>?));
            }
            if (t == CollectionType)
            {
                return GetSize(__refvalue(value, ICollection<string?>?));
            }
            if (t == RoCollectionType)
            {
                return GetSize(__refvalue(value, IReadOnlyCollection<string?>?));
            }
            if (t == SpanType)
            {
                return GetSize(__refvalue(value, Span<string?>));
            }
            if (t == RoSpanPtrType)
            {
                ReadOnlySpan<string?>* span = __refvalue(value, ReadOnlySpan<string?>*);
                return span == null ? 1 : GetSize(*span);
            }
            if (t == RoSpanPtrType)
            {
                Span<string?>* span = __refvalue(value, Span<string?>*);
                return span == null ? 1 : GetSize(*span);
            }

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
    }
}
