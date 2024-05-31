using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UInt8Parser : BinaryTypeParser<byte>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(byte value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 1 };

        *bytes = value;
        return 1;
    }
    public override int WriteObject(byte value, Stream stream)
    {
        stream.WriteByte(value);
        return 1;
    }
    public override unsafe byte ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 1 };

        bytesRead = 1;
        return *bytes;
    }
    public override byte ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UInt8Parser))) { ErrorCode = 2 };

        bytesRead = 1;
        return (byte)b;
    }

    public unsafe class Many : ArrayBinaryTypeParser<byte>
    {
        private readonly SerializationConfiguration _config;
        public Many(SerializationConfiguration config)
        {
            _config = config;
            _config.Lock();
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ArraySegment<byte> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            value.AsSpan().CopyTo(new Span<byte>(bytes + hdrSize, length));

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] scoped ReadOnlySpan<byte> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            value.CopyTo(new Span<byte>(bytes + hdrSize, length));

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IList<byte>? value, byte* bytes, uint maxSize)
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

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            switch (value)
            {
                case byte[] b:
                    b.AsSpan().CopyTo(new Span<byte>(bytes + hdrSize, length));
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    underlying.AsSpan(0, length).CopyTo(new Span<byte>(bytes + hdrSize, length));
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = value[i];
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyList<byte>? value, byte* bytes, uint maxSize)
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

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            switch (value)
            {
                case byte[] b:
                    b.AsSpan().CopyTo(new Span<byte>(bytes + hdrSize, length));
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    underlying.AsSpan(0, length).CopyTo(new Span<byte>(bytes + hdrSize, length));
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = value[i];
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ICollection<byte>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            if (value is IList<byte> list)
            {
                return WriteObject(list, bytes, maxSize);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, bytes, maxSize);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    value.CopyTo(buffer, 0);
                    buffer.AsSpan(0, length).CopyTo(new Span<byte>(bytes + hdrSize, length));
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                value.CopyTo(buffer, 0);
                buffer.AsSpan(0, length).CopyTo(new Span<byte>(bytes + hdrSize, length));
            }
            else
            {
                using IEnumerator<byte> enumerator = value.GetEnumerator();
                int i = 0;
                bytes += hdrSize;
                while (enumerator.MoveNext())
                {
                    bytes[i] = enumerator.Current;
                    ++i;
                }

                if (i == length)
                    return i + hdrSize;

                int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(i, false));
                if (maxSize < i + newHdrSize)
                    throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };
                if (newHdrSize == hdrSize)
                {
                    index = 0;
                    SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, i, false, this);
                }
                else if (!Compatibility.IncompatibleWithBufferMemoryCopyOverlap || hdrSize > newHdrSize)
                {
                    Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, i, i);
                }
                else
                {
                    for (int i2 = i - 1; i2 >= 0; --i2)
                    {
                        bytes[newHdrSize + i2] = bytes[hdrSize + i2];
                    }
                }

                index = 0;
                SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, i, false, this);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyCollection<byte>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            if (value is IList<byte> list)
            {
                return WriteObject(list, bytes, maxSize);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, bytes, maxSize);
            }
            if (value is ICollection<byte> collection)
            {
                return WriteObject(collection, bytes, maxSize);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            int actualCount = 0;
            using (IEnumerator<byte> enumerator = value.GetEnumerator())
            {
                bytes += hdrSize;
                while (enumerator.MoveNext())
                {
                    if (maxSize - hdrSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    bytes[actualCount] = enumerator.Current;
                    ++actualCount;
                }

                bytes -= hdrSize;
            }

            if (actualCount == length)
                return actualCount + hdrSize;
            
            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (maxSize < actualCount + newHdrSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };
            if (newHdrSize == hdrSize)
            {
                index = 0;
                SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, actualCount, false, this);
            }
            else if (!Compatibility.IncompatibleWithBufferMemoryCopyOverlap || hdrSize > newHdrSize)
            {
                Buffer.MemoryCopy(bytes + hdrSize, bytes + newHdrSize, actualCount, actualCount);
            }
            else
            {
                for (int i = actualCount - 1; i >= 0; --i)
                {
                    bytes[newHdrSize + i] = bytes[hdrSize + i];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, actualCount, false, this);

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IEnumerable<byte>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            if (value is IList<byte> list)
            {
                return WriteObject(list, bytes, maxSize);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, bytes, maxSize);
            }
            if (value is ICollection<byte> collection)
            {
                return WriteObject(collection, bytes, maxSize);
            }
            if (value is IReadOnlyCollection<byte> collection2)
            {
                return WriteObject(collection2, bytes, maxSize);
            }

            int actualCount = 0;
            using (IEnumerator<byte> enumerator = value.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (maxSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    bytes[actualCount] = enumerator.Current;
                    ++actualCount;
                }
            }

            int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (maxSize < actualCount + newHdrSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };
            if (!Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
            {
                Buffer.MemoryCopy(bytes, bytes + newHdrSize, actualCount, actualCount);
            }
            else
            {
                for (int i = actualCount - 1; i >= 0; --i)
                {
                    bytes[newHdrSize + i] = bytes[i];
                }
            }

            index = 0;
            SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, actualCount, false, this);

            return actualCount + newHdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ArraySegment<byte> value, Stream stream)
        {
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            stream.Write(value.Array, value.Offset, length);
            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] scoped ReadOnlySpan<byte> value, Stream stream)
        {
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            stream.Write(value);
#else
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    value.CopyTo(buffer.AsSpan(0, length));
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                value.CopyTo(buffer);
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    value.Slice(length - bytesLeft, sizeToCopy).CopyTo(buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
#endif
            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IList<byte>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            switch (value)
            {
                case byte[] b:
                    stream.Write(b, 0, length);
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    stream.Write(underlying, 0, length);
                    break;

                default:
                    if (length <= DefaultSerializer.MaxArrayPoolSize)
                    {
                        byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                        try
                        {
                            value.CopyTo(buffer, 0);
                            stream.Write(buffer, 0, length);
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(buffer);
                        }
                    }
                    else if (length <= _config.MaximumBufferSize)
                    {
                        byte[] buffer = new byte[length];
                        value.CopyTo(buffer, 0);
                        stream.Write(buffer, 0, length);
                    }
                    else
                    {
                        byte[] buffer = new byte[_config.MaximumBufferSize];
                        int bytesLeft = length;
                        do
                        {
                            int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                            int stInd = length - bytesLeft;
                            for (int i = 0; i < sizeToCopy; ++i)
                                buffer[i] = value[i + stInd];
                            stream.Write(buffer, 0, sizeToCopy);
                            bytesLeft -= sizeToCopy;
                        } while (bytesLeft > 0);
                    }

                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyList<byte>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            switch (value)
            {
                case byte[] b:
                    stream.Write(b, 0, length);
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    stream.Write(underlying, 0, length);
                    break;

                default:
                    if (length <= DefaultSerializer.MaxArrayPoolSize)
                    {
                        byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                        try
                        {
                            if (value is IList<byte> list)
                                list.CopyTo(buffer, 0);
                            else if (value is IList l2)
                                l2.CopyTo(buffer, 0);
                            else
                            {
                                for (int i = 0; i < length; ++i)
                                    buffer[i] = value[i];
                            }
                            stream.Write(buffer, 0, length);
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(buffer);
                        }
                    }
                    else if (length <= _config.MaximumBufferSize)
                    {
                        byte[] buffer = new byte[length];
                        if (value is IList<byte> list)
                            list.CopyTo(buffer, 0);
                        else if (value is IList l2)
                            l2.CopyTo(buffer, 0);
                        else
                        {
                            for (int i = 0; i < length; ++i)
                                buffer[i] = value[i];
                        }
                        stream.Write(buffer, 0, length);
                    }
                    else
                    {
                        byte[] buffer = new byte[_config.MaximumBufferSize];
                        int bytesLeft = length;
                        do
                        {
                            int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                            int stInd = length - bytesLeft;
                            for (int i = 0; i < sizeToCopy; ++i)
                                buffer[i] = value[i + stInd];
                            stream.Write(buffer, 0, sizeToCopy);
                            bytesLeft -= sizeToCopy;
                        } while (bytesLeft > 0);
                    }

                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ICollection<byte>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is IList<byte> list)
            {
                return WriteObject(list, stream);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    value.CopyTo(buffer, 0);
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                value.CopyTo(buffer, 0);
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = length;
                using IEnumerator<byte> enumerator = value.GetEnumerator();
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int index = 0;
                    while (index < sizeToCopy && enumerator.MoveNext())
                        buffer[index++] = enumerator.Current;
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyCollection<byte>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is IList<byte> list)
            {
                return WriteObject(list, stream);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, stream);
            }
            if (value is ICollection<byte> collection)
            {
                return WriteObject(collection, stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            using IEnumerator<byte> enumerator = value.GetEnumerator();
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    int index = 0;
                    while (index < length && enumerator.MoveNext())
                        buffer[index++] = enumerator.Current;
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                int index = 0;
                while (index < length && enumerator.MoveNext())
                    buffer[index++] = enumerator.Current;
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int index = 0;
                    while (index < sizeToCopy && enumerator.MoveNext())
                        buffer[index++] = enumerator.Current;
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IEnumerable<byte>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is IList<byte> list)
            {
                return WriteObject(list, stream);
            }
            if (value is IReadOnlyList<byte> list2)
            {
                return WriteObject(list2, stream);
            }
            if (value is ICollection<byte> collection)
            {
                return WriteObject(collection, stream);
            }
            if (value is IReadOnlyCollection<byte> collection2)
            {
                return WriteObject(collection2, stream);
            }

            IEnumerator<byte> enumerator = value.GetEnumerator();
            try
            {
                int length = 0;
                while (enumerator.MoveNext())
                    checked { ++length; }

                ResetOrReMake(ref enumerator, value);
                int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

                if (length == 0)
                    return hdrSize;

                if (length <= DefaultSerializer.MaxArrayPoolSize)
                {
                    byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                    try
                    {
                        int index = 0;
                        while (index < length && enumerator.MoveNext())
                            buffer[index++] = enumerator.Current;
                        stream.Write(buffer, 0, length);
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(buffer);
                    }
                }
                else if (length <= _config.MaximumBufferSize)
                {
                    byte[] buffer = new byte[length];
                    int index = 0;
                    while (index < length && enumerator.MoveNext())
                        buffer[index++] = enumerator.Current;
                    stream.Write(buffer, 0, length);
                }
                else
                {
                    byte[] buffer = new byte[_config.MaximumBufferSize];
                    int bytesLeft = length;
                    do
                    {
                        int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                        int index = 0;
                        while (index < sizeToCopy && enumerator.MoveNext())
                            buffer[index++] = enumerator.Current;
                        stream.Write(buffer, 0, sizeToCopy);
                        bytesLeft -= sizeToCopy;
                    } while (bytesLeft > 0);
                }

                return length + hdrSize;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        /// <inheritdoc />
        public override byte[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
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
                return Array.Empty<byte>();
            }

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            byte[] arr = new byte[length];
            int size = (int)index + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<byte>(bytes + index, length).CopyTo(arr);
            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<byte> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Count;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    if (maxSize >= bytesRead)
                        bytesRead += length;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<byte>(bytes, length).CopyTo(output);
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<byte> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Length)
                {
                    if (maxSize >= bytesRead)
                        bytesRead += length;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<byte>(bytes, length).CopyTo(output);
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<byte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(0);
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
                            output.Add(0);
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

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            byte[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is byte[] arr1)
            {
                arr = arr1;
            }
            else if (setInsteadOfAdding && output is ArraySegment<byte> arr2)
            {
                arr = arr2.Array;
                arrOffset = arr2.Offset;
            }
            else if (output is List<byte> list)
            {
                if (!setInsteadOfAdding)
                    arrOffset = list.Count;
                if (list.Capacity < arrOffset + length)
                    list.Capacity = arrOffset + length;

                if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                    arr = null;
            }

            if (arr != null)
            {
                new Span<byte>(bytes, length).CopyTo(arr.AsSpan(arrOffset, length));
                bytesRead = size;
                return length;
            }

            bytesRead = size;
            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; ++i)
                    output[i] = bytes[i];
            }
            else
            {
                for (int i = 0; i < length; ++i)
                    output.Add(bytes[i]);
            }

            return length;
        }


        /// <inheritdoc />
        public override byte[]? ReadObject(Stream stream, out int bytesRead)
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                return null;

            if (length == 0)
                return Array.Empty<byte>();

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            byte[] arr = new byte[length];
            int rdCt = stream.Read(arr, 0, length);
            bytesRead += rdCt;
            if (rdCt != length)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] ArraySegment<byte> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Count;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    SerializationHelper.TryAdvanceStream(stream, ref bytesRead, length);
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            int rdCt = stream.Read(output.Array!, output.Offset, output.Count);
            bytesRead += rdCt;
            if (rdCt != length)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] scoped Span<byte> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Length)
                {
                    SerializationHelper.TryAdvanceStream(stream, ref bytesRead, length);
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            int rdCt = stream.Read(output);
            bytesRead += rdCt;
            if (rdCt != length)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

#else
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    int readCt = stream.Read(buffer, 0, length);
                    if (readCt != length)
                    {
                        bytesRead += readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    buffer.AsSpan(0, length).CopyTo(output.Slice(0, length));
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                int readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                buffer.AsSpan().CopyTo(output.Slice(0, length));
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - length + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    buffer.AsSpan(0, sizeToCopy).CopyTo(output.Slice(length - bytesLeft, sizeToCopy));
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;
#endif
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] IList<byte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(0);
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
                            output.Add(0);
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

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(sbyte), length, this);

            int readCt;
            byte[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is byte[] arr1)
            {
                arr = arr1;
            }
            else if (setInsteadOfAdding && output is ArraySegment<byte> arr2)
            {
                arr = arr2.Array;
                arrOffset = arr2.Offset;
            }
            else if (output is List<byte> list)
            {
                if (!setInsteadOfAdding)
                    arrOffset = list.Count;
                if (list.Capacity < arrOffset + length)
                    list.Capacity = arrOffset + length;

                if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                    arr = null;
            }

            if (arr != null)
            {
                readCt = stream.Read(arr, arrOffset, length);
                bytesRead += readCt;
                if (readCt != length)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

                return length;
            }
            
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    readCt = stream.Read(buffer, 0, length);
                    if (readCt != length)
                    {
                        bytesRead += readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    if (setInsteadOfAdding)
                    {
                        for (int i = 0; i < length; ++i)
                            output[i] = buffer[i];
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                            output.Add(buffer[i]);
                    }
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= _config.MaximumBufferSize)
            {
                byte[] buffer = new byte[length];
                readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (setInsteadOfAdding)
                {
                    for (int i = 0; i < length; ++i)
                        output[i] = buffer[i];
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                        output.Add(buffer[i]);
                }
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - length + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    int stInd = length - bytesLeft;

                    if (setInsteadOfAdding)
                    {
                        for (int i = 0; i < sizeToCopy; ++i)
                            output[stInd + i] = buffer[i];
                    }
                    else
                    {
                        for (int i = 0; i < sizeToCopy; ++i)
                            output.Add(buffer[i]);
                    }
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;

            return length;
        }
    }
}
