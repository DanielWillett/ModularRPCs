using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class StringParser(Encoding encoding) : BinaryTypeParser<string>
{
    private readonly Encoding _encoding = encoding;

    /*
     * Header format:
     * [ 1 byte - flags                                                                                         ] [ byte count ] [ char count ] [ data...            ]
     * | 0000AABB            mask   meaning                                                                     | | variable sizes, see flags | | length: byte count |
     * |     11   char count 0b1100 0 = same as byte ct, 1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     * |       11 byte count 0b0011 0 = empty string,    1 = 8 bit length, 2 = 16 bit length, 3 = 32 bit length | |                           | |                    |
     */
    public override bool IsVariableSize => true;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(string value, byte* bytes, uint maxSize)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (maxSize < 1)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

            *bytes = 0;
            return 1;
        }

        int charLen = value.Length;

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

            switch (lenFlag >> 2)
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
    public override unsafe int WriteObject(string value, Stream stream)
    {
        if (string.IsNullOrEmpty(value))
        {
            stream.WriteByte(0);
            return 1;
        }

        int size = _encoding.GetByteCount(value);

        int charLen = value.Length;

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

        switch (lenFlag >> 2)
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

        if (size > 512)
        {
            if (size > 2048)
            {
                using StreamWriter writer = new StreamWriter(stream, _encoding, 1024, true);
                writer.Write(value);
                writer.Flush();
            }
            else
            {
                byte[] utf8 = new byte[size];
                fixed (char* ptr = value)
                fixed (byte* newData = utf8)
                {
                    _encoding.GetBytes(ptr, charLen, newData, size);
                }

                stream.Write(utf8, 0, utf8.Length);
            }
        }
        else
        {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
            byte[] utf8 = new byte[size];
            fixed (char* ptr = value)
            fixed (byte* newData = utf8)
            {
                encoding.GetBytes(ptr, charLen, newData, size);
            }

            stream.Write(utf8, 0, utf8.Length);
#else
            byte* newData = stackalloc byte[size];
            fixed (char* ptr = value)
                _encoding.GetBytes(ptr, charLen, newData, size);

            stream.Write(new ReadOnlySpan<byte>(newData, size));
#endif
        }

        return ttlSize;
    }
    public override unsafe string ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

        byte lenFlag = *bytes;
        if (lenFlag == 0)
        {
            bytesRead = 1;
            return string.Empty;
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        int charLen;
#endif
        int charHdrSize;
        switch (lenFlag >> 2)
        {
            case 0:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                charLen = size;
#endif
                charHdrSize = 0;
                break;

            case 1:
                if (maxSize < 1)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                charLen = bytes[1];
#endif
                charHdrSize = 1;
                break;

            case 2:
                if (maxSize < 2)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes + 1)
                    : bytes[1] << 8 | bytes[2];
#endif
                charHdrSize = 2;
                break;

            default:
                if (maxSize < 4)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                charLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<int>(bytes + 1)
                    : bytes[1] << 24 | bytes[2] << 16 | bytes[3] << 8 | bytes[4];
#endif
                charHdrSize = 4;
                break;
        }

        maxSize -= (uint)charHdrSize;
        bytes += charHdrSize;

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
        string str = encoding.GetString(bytes, size);
#endif
        bytesRead = size + hdrSize + charHdrSize;
        return str;
    }
    public override unsafe string ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

        byte lenFlag = (byte)b;
        if (lenFlag == 0)
        {
            bytesRead = 1;
            return string.Empty;
        }


        int hdrSize = GetLengthSize(lenFlag);
        int size;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        int charLen;
#endif
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

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        switch (lenFlag >> 2)
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
#endif

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        string str;
        if (size > 512)
        {
            if (size > 2048)
            {
                ReadStringStreamContext ctx = default;
                ctx.Stream = stream;
                ctx.Parser = this;
                str = string.Create(charLen, ctx, static (span, state) =>
                {
                    using StreamReader reader = new StreamReader(state.Stream, state.Parser._encoding, false, 1024, true);
                    int charCt = reader.ReadBlock(span);
                    if (charCt != span.Length)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, state.Parser.GetType().Name)) { ErrorCode = 2 };
                });
            }
            else
            {
                try
                {
                    ReadStringStreamContext ctx = default;
                    ctx.Stream = stream;
                    ctx.Parser = this;
                    ctx.ByteSize = size;
                    str = string.Create(charLen, ctx, static (span, state) =>
                    {
                        byte[] data = new byte[state.ByteSize];
                        int ct = state.Stream.Read(data, 0, state.ByteSize);
                        if (ct != state.ByteSize)
                            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, state.Parser.GetType().Name)) { ErrorCode = 2 };

                        state.Parser._encoding.GetChars(data, span);
                    });
                }
                catch (ArgumentException)
                {
                    byte[] data = new byte[size];
                    ct = stream.Read(data, 0, size);
                    if (ct != size)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

                    str = _encoding.GetString(data);
                }
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
                str = _encoding.GetString(dataPtr, size);
            }
        }
#else
        byte[] data = new byte[size];
        int ct2 = stream.Read(data, 0, size);
        if (ct2 != size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, GetType().Name)) { ErrorCode = 2 };

        string str = _encoding.GetString(data);
#endif

        bytesRead = size + hdrSize + 1;
        return str;
    }
    private static byte GetLengthFlag(int size, int charLen)
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

    private static int GetLengthSize(byte lenFlag) =>
    (lenFlag & 3) switch
    {
        0 => 0,
        1 => 1,
        2 => 2,
        _ => 4
    } + (lenFlag >> 2) switch
    {
        0 => 0,
        1 => 1,
        2 => 2,
        _ => 4
    };
    public override int GetSize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 1;

        int byteCt = _encoding.GetByteCount(value);
        byte flag = GetLengthFlag(byteCt, value.Length);
        return 1 + GetLengthSize(flag) + byteCt;
    }
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private struct ReadStringStreamContext
    {
        public Stream Stream;
        public int ByteSize;
        public StringParser Parser;
    }
    private struct ReadStringBytesContext
    {
        public unsafe byte* Bytes;
        public int Size;
        public Encoding Encoding;
    }
#endif
}
