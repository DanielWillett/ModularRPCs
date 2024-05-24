using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DanielWillett.ReflectionTools;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class BooleanParser : BinaryTypeParser<bool>
{

    /// <inheritdoc />
    public override bool IsVariableSize => false;

    /// <inheritdoc />
    public override int MinimumSize => 1;

    /// <inheritdoc />
    public override unsafe int WriteObject(bool value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 1 };

        *bytes = value ? (byte)1 : (byte)0;
        return 1;
    }

    /// <inheritdoc />
    public override int WriteObject(bool value, Stream stream)
    {
        stream.WriteByte(value ? (byte)1 : (byte)0);
        return 1;
    }

    /// <inheritdoc />
    public override unsafe bool ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 1 };

        bytesRead = 1;
        return *bytes > 0;
    }

    /// <inheritdoc />
    public override bool ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(BooleanParser))) { ErrorCode = 2 };

        bytesRead = 1;
        return b > 0;
    }
    public class Many : IArrayBinaryTypeParser<bool>, IBinaryTypeParser<BitArray>
    {
        private static readonly Type BitArrType = typeof(BitArray);
        private static readonly Type BoolArrType = typeof(bool[]);
        private static readonly Type BoolListType = typeof(IList<bool>);
        private static readonly Type BoolRoListType = typeof(IReadOnlyList<bool>);
        private static readonly Type BoolSpanType = typeof(Span<bool>);
        private static readonly Type BoolRoSpanType = typeof(ReadOnlySpan<bool>);

        /// <inheritdoc />
        public bool IsVariableSize => true;

        /// <inheritdoc />
        public int MinimumSize => 1;
        private static int CalcLen(int length)
        {
            byte lenFlag = SerializationHelper.GetLengthFlag(length, false);
            int hdrSize = SerializationHelper.GetHeaderSize(lenFlag);
            return hdrSize + (length - 1) / 8 + 1;
        }

        /// <inheritdoc />
        public unsafe int ReadArrayLength(byte* bytes, uint maxSize, out int bytesRead)
        {
            uint index = 0;
            SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this);
            bytesRead = (int)index;
            return length;
        }

        /// <inheritdoc />
        public int ReadArrayLength(Stream stream, out int bytesRead)
        {
            SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this);
            return length;
        }
        public unsafe BitArray? ReadBitArray(byte* bytes, uint maxSize, out int bytesRead)
        {
            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = (int)index;
                return null;
            }

            BitArray arr = new BitArray(length);

            if (length == 0)
            {
                bytesRead = (int)index;
                return arr;
            }
            bytesRead = (int)index + (length - 1) / 8 + 1;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytes += index;
            byte current = *bytes;
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++bytes;
                    current = *bytes;
                }
                arr[i] = (1 & (current >>> mod)) != 0;
            }

            return arr;
        }
        public BitArray? ReadBitArray(Stream stream, out int bytesRead)
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                return null;

            if (length == 0)
                return new BitArray(0);

            int size = (length - 1) / 8 + 1;

            byte[] newBytes = new byte[size];
            int rdCt = stream.Read(newBytes, 0, size);

            if (rdCt != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            if (BitConverter.IsLittleEndian)
            {
                return new BitArray(newBytes);
            }
                
            BitArray arr = new BitArray(length);

            int index = 0;
            byte current = newBytes[0];
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++index;
                    current = newBytes[index];
                }
                arr[i] = (1 & (current >>> mod)) != 0;
            }

            return arr;
        }
        public unsafe bool[]? ReadBooleanArray(byte* bytes, uint maxSize, out int bytesRead)
        {
            uint index = 0;
            if (!SerializationHelper.ReadStandardArrayHeader(bytes, maxSize, ref index, out int length, this))
            {
                bytesRead = (int)index;
                return null;
            }

            if (length == 0)
            {
                bytesRead = (int)index;
                return Array.Empty<bool>();
            }

            bool[] arr = new bool[length];
            bytesRead = (int)index + (length - 1) / 8 + 1;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytes += index;
            byte current = *bytes;
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++bytes;
                    current = *bytes;
                }
                arr[i] = (1 & (current >>> mod)) != 0;
            }

            return arr;
        }
        public bool[]? ReadBooleanArray(Stream stream, out int bytesRead)
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                return null;

            if (length == 0)
                return Array.Empty<bool>();

            int size = (length - 1) / 8 + 1;

            byte[] newBytes = new byte[size];
            int rdCt = stream.Read(newBytes, 0, size);

            if (rdCt != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            bool[] arr = new bool[length];

            int index = 0;
            byte current = newBytes[0];
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++index;
                    current = newBytes[index];
                }
                arr[i] = (1 & (current >>> mod)) != 0;
            }

            return arr;
        }

        /// <inheritdoc />
        public unsafe int ReadObject(byte* bytes, uint maxSize, Span<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Length)
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            int size = (length - 1) / 8 + 1;
            bytes += bytesRead;
            bytesRead += size;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            byte current = *bytes;
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++bytes;
                    current = *bytes;
                }
                output[i] = (1 & (current >>> mod)) != 0;
            }

            return length;
        }

        /// <inheritdoc />
        public unsafe int ReadObject(byte* bytes, uint maxSize, IList<bool> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
        {
            if (output.IsReadOnly)
                throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

            int length = setInsteadOfAdding ? output.Count : measuredCount;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (setInsteadOfAdding)
                {
                    while (length > output.Count)
                        output.Add(false);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    while (length > output.Count)
                        output.Add(false);
                }
            }

            if (length <= 0)
                return 0;

            int size = (length - 1) / 8 + 1;
            bytes += bytesRead;
            bytesRead += size;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            byte current = *bytes;
            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; i++)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0 & i != 0)
                    {
                        ++bytes;
                        current = *bytes;
                    }

                    output[i] = (1 & (current >>> mod)) != 0;
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0 & i != 0)
                    {
                        ++bytes;
                        current = *bytes;
                    }

                    output.Add((1 & (current >>> mod)) != 0);
                }
            }

            return length;
        }

        /// <inheritdoc />
        public int ReadObject(Stream stream, Span<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Length)
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            int size = (length - 1) / 8 + 1;

            byte[] newBytes = new byte[size];
            int rdCt = stream.Read(newBytes, 0, size);

            if (rdCt != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            int index = 0;
            byte current = newBytes[0];
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++index;
                    current = newBytes[index];
                }
                output[i] = (1 & (current >>> mod)) != 0;
            }

            return length;
        }

        /// <inheritdoc />
        public int ReadObject(Stream stream, IList<bool> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
        {
            if (output.IsReadOnly)
                throw new ArgumentException(nameof(output), string.Format(Properties.Exceptions.OutputListReadOnlyIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));

            int length = setInsteadOfAdding ? output.Count : measuredCount;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (setInsteadOfAdding)
                {
                    while (length > output.Count)
                        output.Add(false);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    while (length > output.Count)
                        output.Add(false);
                }
            }

            if (length == 0)
                return 0;

            int size = (length - 1) / 8 + 1;

            byte[] newBytes = new byte[size];
            int rdCt = stream.Read(newBytes, 0, size);

            if (rdCt != size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            int index = 0;
            byte current = newBytes[0];
            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; i++)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0 & i != 0)
                    {
                        ++index;
                        current = newBytes[index];
                    }

                    output[i] = (1 & (current >>> mod)) != 0;
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0 & i != 0)
                    {
                        ++index;
                        current = newBytes[index];
                    }

                    output.Add((1 & (current >>> mod)) != 0);
                }
            }

            return length;
        }

        /// <inheritdoc />
        public unsafe int WriteObject(bool[]? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name));

            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject(IList<bool>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name));

            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject(IReadOnlyList<bool>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name));

            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject(ReadOnlySpan<bool> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name));

            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject(bool[]? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, len, false);

            if (value.Length == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;
            byte[] buffer = new byte[byteSize];

            int index = 0;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    buffer[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                buffer[index] = current;

            stream.Write(buffer, 0, byteSize);
            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject(IList<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, len, false);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;
            byte[] buffer = new byte[byteSize];

            int index = 0;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    buffer[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                buffer[index] = current;

            stream.Write(buffer, 0, byteSize);
            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject(IReadOnlyList<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, len, false);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;
            byte[] buffer = new byte[byteSize];

            int index = 0;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    buffer[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                buffer[index] = current;

            stream.Write(buffer, 0, byteSize);
            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject(ReadOnlySpan<bool> value, Stream stream)
        {
            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, len, false);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;
            byte[] buffer = new byte[byteSize];

            int index = 0;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    buffer[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                buffer[index] = current;

            stream.Write(buffer, 0, byteSize);
            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject(BitArray? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, value.Length, false, this);

            if (value.Length == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = value.Get(i);
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            if (len % 8 != 0)
                *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject(BitArray? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, len, false);

            if (value.Length == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;
            byte[] buffer = new byte[byteSize];

            if (BitConverter.IsLittleEndian)
            {
                value.CopyTo(buffer, 0);
            }
            else
            {
                int index = 0;
                byte current = 0;
                for (int i = 0; i < len; i++)
                {
                    bool c = value.Get(i);
                    int mod = i % 8;
                    if (mod == 0 && i != 0)
                    {
                        buffer[index] = current;
                        ++index;
                        current = (byte)(c ? 1 : 0);
                    }
                    else if (c) current |= (byte)(1 << mod);
                }

                if (len % 8 != 0)
                    buffer[index] = current;
            }

            stream.Write(buffer, 0, byteSize);
            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int GetSize(TypedReference value)
        {
            Type t = __reftype(value);
            int len;
            if (t == BitArrType)
                len = __refvalue(value, BitArray).Length;
            else if (t == BoolArrType)
                len = __refvalue(value, bool[]).Length;
            else if (t == BoolListType)
                len = __refvalue(value, IList<bool>).Count;
            else if (t == BoolRoListType)
                len = __refvalue(value, IReadOnlyList<bool>).Count;
            else if (t == BoolRoSpanType)
                len = __refvalue(value, ReadOnlySpan<bool>).Length;
            else if (t == BoolSpanType)
                len = __refvalue(value, Span<bool>).Length;
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));

            return CalcLen(len);
        }

        /// <inheritdoc />
        public unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize)
        {
            Type t = __reftype(value);
            if (t == BitArrType)
                return WriteObject(__refvalue(value, BitArray), bytes, maxSize);
            if (t == BoolArrType)
                return WriteObject(__refvalue(value, bool[]), bytes, maxSize);
            if (t == BoolListType)
                return WriteObject(__refvalue(value, IList<bool>), bytes, maxSize);
            if (t == BoolRoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<bool>), bytes, maxSize);
            if (t == BoolRoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<bool>), bytes, maxSize);
            if (t == BoolSpanType)
                return WriteObject(__refvalue(value, Span<bool>), bytes, maxSize);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public int WriteObject(TypedReference value, Stream stream)
        {
            Type t = __reftype(value);
            if (t == BitArrType)
                return WriteObject(__refvalue(value, BitArray), stream);
            if (t == BoolArrType)
                return WriteObject(__refvalue(value, bool[]), stream);
            if (t == BoolListType)
                return WriteObject(__refvalue(value, IList<bool>), stream);
            if (t == BoolRoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<bool>), stream);
            if (t == BoolRoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<bool>), stream);
            if (t == BoolSpanType)
                return WriteObject(__refvalue(value, Span<bool>), stream);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public unsafe void ReadObject(byte* bytes, uint maxSize, out int bytesRead, TypedReference outObj)
        {
            Type t = __reftype(outObj);
            if (t == BitArrType)
                __refvalue(outObj, BitArray?) = ReadBitArray(bytes, maxSize, out bytesRead);
            else if (t == BoolArrType)
                __refvalue(outObj, bool[]?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolListType)
                __refvalue(outObj, IList<bool>?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolRoListType)
                __refvalue(outObj, IReadOnlyList<bool>?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolRoSpanType)
                __refvalue(outObj, ReadOnlySpan<bool>) = ReadBooleanArray(bytes, maxSize, out bytesRead).AsSpan();
            else if (t == BoolSpanType)
            {
                ref Span<bool> existingSpan = ref __refvalue(outObj, Span<bool>);
                if (!existingSpan.IsEmpty)
                {
                    ReadObject(bytes, maxSize, existingSpan, out bytesRead, false);
                }
                else
                {
                    existingSpan = ReadBooleanArray(bytes, maxSize, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public void ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
        {
            Type t = __reftype(outObj);
            if (t == BitArrType)
                __refvalue(outObj, BitArray?) = ReadBitArray(stream, out bytesRead);
            else if (t == BoolArrType)
                __refvalue(outObj, bool[]?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolListType)
                __refvalue(outObj, IList<bool>?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolRoListType)
                __refvalue(outObj, IReadOnlyList<bool>?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolRoSpanType)
                __refvalue(outObj, ReadOnlySpan<bool>) = ReadBooleanArray(stream, out bytesRead).AsSpan();
            else if (t == BoolSpanType)
            {
                ref Span<bool> existingSpan = ref __refvalue(outObj, Span<bool>);
                if (!existingSpan.IsEmpty)
                {
                    ReadObject(stream, existingSpan, out bytesRead, false);
                }
                else
                {
                    existingSpan = ReadBooleanArray(stream, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public int GetSize(BitArray? value) => value == null ? 1 : CalcLen(value.Length);

        /// <inheritdoc />
        public int GetSize(bool[]? value) => value == null ? 1 : CalcLen(value.Length);

        /// <inheritdoc />
        public int GetSize(IList<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize(IReadOnlyList<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize(ReadOnlySpan<bool> value) => CalcLen(value.Length);

        /// <inheritdoc />
        public int GetSize(object? value)
        {
            int len;
            switch (value)
            {
                case BitArray bits:
                    len = bits.Length;
                    break;
                case bool[] arr:
                    len = arr.Length;
                    break;
                case IList<bool> list:
                    len = list.Count;
                    break;
                case IReadOnlyList<bool> list:
                    len = list.Count;
                    break;
                case null:
                    return 1;
                default:
                    throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())));
            }

            return CalcLen(len);
        }

        /// <inheritdoc />
        public unsafe int WriteObject(object? value, byte* bytes, uint maxSize)
        {
            return value switch
            {
                BitArray bits => WriteObject(bits, bytes, maxSize),
                bool[] arr => WriteObject(arr, bytes, maxSize),
                IList<bool> list => WriteObject(list, bytes, maxSize),
                IReadOnlyList<bool> list => WriteObject(list, bytes, maxSize),
                null => WriteObject((bool[])null!, bytes, maxSize),
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }

        /// <inheritdoc />
        public int WriteObject(object? value, Stream stream)
        {
            return value switch
            {
                BitArray bits => WriteObject(bits, stream),
                bool[] arr => WriteObject(arr, stream),
                IList<bool> list => WriteObject(list, stream),
                IReadOnlyList<bool> list => WriteObject(list, stream),
                null => WriteObject((bool[])null!, stream),
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }

        /// <inheritdoc />
        unsafe BitArray? IBinaryTypeParser<BitArray>.ReadObject(byte* bytes, uint maxSize, out int bytesRead) => ReadBitArray(bytes, maxSize, out bytesRead);

        /// <inheritdoc />
        BitArray? IBinaryTypeParser<BitArray>.ReadObject(Stream stream, out int bytesRead) => ReadBitArray(stream, out bytesRead);

        /// <inheritdoc />
        unsafe bool[]? IBinaryTypeParser<bool[]>.ReadObject(byte* bytes, uint maxSize, out int bytesRead) => ReadBooleanArray(bytes, maxSize, out bytesRead);

        /// <inheritdoc />
        bool[]? IBinaryTypeParser<bool[]>.ReadObject(Stream stream, out int bytesRead) => ReadBooleanArray(stream, out bytesRead);

        /// <inheritdoc />
        public unsafe object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
        {
            if (type == BitArrType)
                return ReadBitArray(bytes, maxSize, out bytesRead);
            if (type == BoolArrType || type == BoolListType || type == BoolRoListType)
                return ReadBooleanArray(bytes, maxSize, out bytesRead);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public object? ReadObject(Type type, Stream stream, out int bytesRead)
        {
            if (type == BitArrType)
                return ReadBitArray(stream, out bytesRead);
            if (type == BoolArrType || type == BoolListType || type == BoolRoListType)
                return ReadBooleanArray(stream, out bytesRead);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
        }
    }
}