using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using DanielWillett.ModularRpcs.Reflection;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class Int8Parser : BinaryTypeParser<sbyte>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 1;
    public override unsafe int WriteObject(sbyte value, byte* bytes, uint maxSize)
    {
        if (maxSize < 1)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 1 };

        *bytes = unchecked( (byte)value );
        return 1;
    }
    public override int WriteObject(sbyte value, Stream stream)
    {
        stream.WriteByte(unchecked( (byte)value ));
        return 1;
    }
    public override unsafe sbyte ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 1 };

        bytesRead = 1;
        return unchecked( (sbyte)*bytes );
    }
    public override sbyte ReadObject(Stream stream, out int bytesRead)
    {
        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(Int8Parser))) { ErrorCode = 2 };

        bytesRead = 1;
        return unchecked( (sbyte)(byte)b );
    }

    public unsafe class Many : ArrayBinaryTypeParser<sbyte>
    {
        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ArraySegment<sbyte> value, byte* bytes, uint maxSize)
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

            value.AsSpan().CopyTo(new Span<sbyte>(bytes + hdrSize, length));

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] scoped ReadOnlySpan<sbyte> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            value.CopyTo(new Span<sbyte>(bytes + hdrSize, length));

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IList<sbyte>? value, byte* bytes, uint maxSize)
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
                case sbyte[] b:
                    b.AsSpan().CopyTo(new Span<sbyte>(bytes + hdrSize, length));
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    underlying.AsSpan(0, length).CopyTo(new Span<sbyte>(bytes + hdrSize, length));
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = unchecked((byte)value[i]);
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyList<sbyte>? value, byte* bytes, uint maxSize)
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
                case sbyte[] b:
                    b.AsSpan().CopyTo(new Span<sbyte>(bytes + hdrSize, length));
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    underlying.AsSpan(0, length).CopyTo(new Span<sbyte>(bytes + hdrSize, length));
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = unchecked((byte)value[i]);
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ICollection<sbyte>? value, byte* bytes, uint maxSize)
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

            int actualCount = 0;
            using (IEnumerator<sbyte> enumerator = value.GetEnumerator())
            {
                bytes += hdrSize;
                while (enumerator.MoveNext())
                {
                    if (maxSize - hdrSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    bytes[actualCount] = unchecked( (byte)enumerator.Current );
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
        public override int WriteObject([InstantHandle] IReadOnlyCollection<sbyte>? value, byte* bytes, uint maxSize)
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

            int actualCount = 0;
            using (IEnumerator<sbyte> enumerator = value.GetEnumerator())
            {
                bytes += hdrSize;
                while (enumerator.MoveNext())
                {
                    if (maxSize - hdrSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    bytes[actualCount] = unchecked( (byte)enumerator.Current );
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
        public override int WriteObject([InstantHandle] IEnumerable<sbyte>? value, byte* bytes, uint maxSize)
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
            using (IEnumerator<sbyte> enumerator = value.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (maxSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    bytes[actualCount] = unchecked( (byte)enumerator.Current );
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
        public override int WriteObject([InstantHandle] ArraySegment<sbyte> value, Stream stream)
        {
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            stream.Write(MemoryMarshal.Cast<sbyte, byte>(value));
#else
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    Buffer.BlockCopy(value.Array, value.Offset, buffer, 0, length);
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                Buffer.BlockCopy(value.Array, value.Offset, buffer, 0, length);
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    Buffer.BlockCopy(value.Array, value.Offset + (length - bytesLeft), buffer, 0, sizeToCopy);
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
#endif

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] scoped ReadOnlySpan<sbyte> value, Stream stream)
        {
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            stream.Write(MemoryMarshal.Cast<sbyte, byte>(value));
#else
            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    value.CopyTo(MemoryMarshal.Cast<byte, sbyte>(buffer.AsSpan(0, length)));
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                value.CopyTo(MemoryMarshal.Cast<byte, sbyte>(buffer));
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    value.Slice(length - bytesLeft, sizeToCopy).CopyTo(MemoryMarshal.Cast<byte, sbyte>(buffer));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
#endif
            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IList<sbyte>? value, Stream stream)
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                case sbyte[] b:
                    stream.Write(MemoryMarshal.Cast<sbyte, byte>(b));
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    stream.Write(MemoryMarshal.Cast<sbyte, byte>(underlying.AsSpan(0, length)));
                    break;
#endif
                default:
                    if (length <= DefaultSerializer.MaxArrayPoolSize)
                    {
                        byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                        try
                        {
                            switch (value)
                            {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                case sbyte[] b:
                                    Buffer.BlockCopy(b, 0, buffer, 0, length);
                                    stream.Write(buffer, 0, length);
                                    break;

                                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                    Buffer.BlockCopy(underlying, 0, buffer, 0, length);
                                    stream.Write(buffer, 0, length);
                                    break;
#endif
                                default:
                                    for (int i = 0; i < length; ++i)
                                        buffer[i] = unchecked((byte)value[i]);
                                    stream.Write(buffer, 0, length);
                                    break;
                            }
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(buffer);
                        }
                    }
                    else if (length <= DefaultSerializer.MaxBufferSize)
                    {
                        byte[] buffer = new byte[length];
                        switch (value)
                        {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            case sbyte[] b:
                                Buffer.BlockCopy(b, 0, buffer, 0, length);
                                stream.Write(buffer, 0, length);
                                break;

                            case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                Buffer.BlockCopy(underlying, 0, buffer, 0, length);
                                stream.Write(buffer, 0, length);
                                break;
#endif
                            default:
                                for (int i = 0; i < length; ++i)
                                    buffer[i] = unchecked((byte)value[i]);
                                stream.Write(buffer, 0, length);
                                break;
                        }
                    }
                    else
                    {
                        byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                        int bytesLeft = length;
                        do
                        {
                            int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                            int stInd = length - bytesLeft;
                            switch (value)
                            {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                case sbyte[] b:
                                    Buffer.BlockCopy(b, stInd, buffer, 0, sizeToCopy);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;

                                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                    Buffer.BlockCopy(underlying, stInd, buffer, 0, sizeToCopy);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;
#endif
                                default:
                                    for (int i = 0; i < sizeToCopy; ++i)
                                        buffer[i] = unchecked((byte)value[i + stInd]);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;
                            }
                            bytesLeft -= sizeToCopy;
                        } while (bytesLeft > 0);
                    }

                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyList<sbyte>? value, Stream stream)
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                case sbyte[] b:
                    stream.Write(MemoryMarshal.Cast<sbyte, byte>(b));
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    stream.Write(MemoryMarshal.Cast<sbyte, byte>(underlying.AsSpan(0, length)));
                    break;
#endif
                default:
                    if (length <= DefaultSerializer.MaxArrayPoolSize)
                    {
                        byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                        try
                        {
                            switch (value)
                            {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                case sbyte[] b:
                                    Buffer.BlockCopy(b, 0, buffer, 0, length);
                                    stream.Write(buffer, 0, length);
                                    break;

                                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                    Buffer.BlockCopy(underlying, 0, buffer, 0, length);
                                    stream.Write(buffer, 0, length);
                                    break;
#endif
                                default:
                                    for (int i = 0; i < length; ++i)
                                        buffer[i] = unchecked((byte)value[i]);
                                    stream.Write(buffer, 0, length);
                                    break;
                            }
                        }
                        finally
                        {
                            DefaultSerializer.ArrayPool.Return(buffer);
                        }
                    }
                    else if (length <= DefaultSerializer.MaxBufferSize)
                    {
                        byte[] buffer = new byte[length];
                        switch (value)
                        {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            case sbyte[] b:
                                Buffer.BlockCopy(b, 0, buffer, 0, length);
                                stream.Write(buffer, 0, length);
                                break;

                            case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                Buffer.BlockCopy(underlying, 0, buffer, 0, length);
                                stream.Write(buffer, 0, length);
                                break;
#endif
                            default:
                                for (int i = 0; i < length; ++i)
                                    buffer[i] = unchecked((byte)value[i]);
                                stream.Write(buffer, 0, length);
                                break;
                        }
                    }
                    else
                    {
                        byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                        int bytesLeft = length;
                        do
                        {
                            int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                            int stInd = length - bytesLeft;
                            switch (value)
                            {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                case sbyte[] b:
                                    Buffer.BlockCopy(b, stInd, buffer, 0, sizeToCopy);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;

                                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                    Buffer.BlockCopy(underlying, stInd, buffer, 0, sizeToCopy);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;
#endif
                                default:
                                    for (int i = 0; i < sizeToCopy; ++i)
                                        buffer[i] = unchecked((byte)value[i + stInd]);
                                    stream.Write(buffer, 0, sizeToCopy);
                                    break;
                            }
                            bytesLeft -= sizeToCopy;
                        } while (bytesLeft > 0);
                    }

                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] ICollection<sbyte>? value, Stream stream)
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
                    int index = 0;
                    using IEnumerator<sbyte> enumerator = value.GetEnumerator();
                    while (index < length && enumerator.MoveNext())
                        buffer[index++] = unchecked( (byte)enumerator.Current );
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                using IEnumerator<sbyte> enumerator = value.GetEnumerator();
                int index = 0;
                while (index < length && enumerator.MoveNext())
                    buffer[index++] = unchecked( (byte)enumerator.Current );
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                using IEnumerator<sbyte> enumerator = value.GetEnumerator();
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    int index = 0;
                    while (index < sizeToCopy && enumerator.MoveNext())
                        buffer[index++] = unchecked( (byte)enumerator.Current );
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IReadOnlyCollection<sbyte>? value, Stream stream)
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

            if (length <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(length);
                try
                {
                    int index = 0;
                    using (IEnumerator<sbyte> enumerator = value.GetEnumerator())
                    {
                        while (index < length && enumerator.MoveNext())
                            buffer[index++] = unchecked( (byte)enumerator.Current );
                    }
                    stream.Write(buffer, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                int index = 0;
                using (IEnumerator<sbyte> enumerator = value.GetEnumerator())
                {
                    while (index < length && enumerator.MoveNext())
                        buffer[index++] = unchecked( (byte)enumerator.Current );
                }
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                using IEnumerator<sbyte> enumerator = value.GetEnumerator();
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    int index = 0;
                    while (index < sizeToCopy && enumerator.MoveNext())
                        buffer[index++] = unchecked( (byte)enumerator.Current );
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject([InstantHandle] IEnumerable<sbyte>? value, Stream stream)
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

            IEnumerator<sbyte> enumerator = value.GetEnumerator();
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
                            buffer[index++] = unchecked( (byte)enumerator.Current );
                        stream.Write(buffer, 0, length);
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(buffer);
                    }
                }
                else if (length <= DefaultSerializer.MaxBufferSize)
                {
                    byte[] buffer = new byte[length];
                    int index = 0;
                    while (index < length && enumerator.MoveNext())
                        buffer[index++] = unchecked( (byte)enumerator.Current );
                    stream.Write(buffer, 0, length);
                }
                else
                {
                    byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                    int bytesLeft = length;
                    do
                    {
                        int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                        int index = 0;
                        while (index < sizeToCopy && enumerator.MoveNext())
                            buffer[index++] = unchecked( (byte)enumerator.Current );
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
        public override sbyte[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
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
                return Array.Empty<sbyte>();
            }

            sbyte[] arr = new sbyte[length];
            int size = (int)index + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<sbyte>(bytes + index, length).CopyTo(arr);

            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<sbyte> output, out int bytesRead, bool hasReadLength = true)
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

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<sbyte>(bytes, length).CopyTo(output.AsSpan(0, length));
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<sbyte> output, out int bytesRead, bool hasReadLength = true)
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

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bytesRead = size;
            new Span<sbyte>(bytes, length).CopyTo(output.Slice(0, length));
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<sbyte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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

            bytes += bytesRead;
            int size = bytesRead + length;

            if (maxSize < size)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };
            
            sbyte[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is sbyte[] arr1)
            {
                arr = arr1;
            }
            else if (setInsteadOfAdding && output is ArraySegment<sbyte> arr2)
            {
                arr = arr2.Array;
                arrOffset = arr2.Offset;
            }
            else if (output is List<sbyte> list)
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
                new Span<sbyte>(bytes, length).CopyTo(arr.AsSpan(arrOffset, length));
                bytesRead = size;
                return length;
            }

            bytesRead = size;
            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; ++i)
                    output[i] = unchecked((sbyte)bytes[i]);
            }
            else
            {
                for (int i = 0; i < length; ++i)
                    output.Add(unchecked((sbyte)bytes[i]));
            }

            return length;
        }


        /// <inheritdoc />
        public override sbyte[]? ReadObject(Stream stream, out int bytesRead)
        {
            if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
                return null;

            if (length == 0)
                return Array.Empty<sbyte>();

            sbyte[] arr = new sbyte[length];
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            int rdCt = stream.Read(MemoryMarshal.Cast<sbyte, byte>(arr));
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

                    Buffer.BlockCopy(buffer, 0, arr, 0, length);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                int readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                Buffer.BlockCopy(buffer, 0, arr, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    int readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - length + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    Buffer.BlockCopy(buffer, 0, arr, length - bytesLeft, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
            bytesRead += length;
#endif
            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] ArraySegment<sbyte> output, out int bytesRead, bool hasReadLength = true)
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            int rdCt = stream.Read(MemoryMarshal.Cast<sbyte, byte>(output[..length]));
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

                    buffer.AsSpan(0, length).CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.AsSpan(0, length)));
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                int readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }
                buffer.AsSpan().CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.AsSpan(0, length)));
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    int readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - length + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    buffer.AsSpan(0, sizeToCopy).CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.AsSpan(length - bytesLeft, sizeToCopy)));
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;
#endif
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] scoped Span<sbyte> output, out int bytesRead, bool hasReadLength = true)
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            int rdCt = stream.Read(MemoryMarshal.Cast<sbyte, byte>(output[..length]));
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

                    buffer.AsSpan(0, length).CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.Slice(0, length)));
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
            {
                byte[] buffer = new byte[length];
                int readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }
                buffer.AsSpan().CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.Slice(0, length)));
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    int readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - length + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    buffer.AsSpan(0, sizeToCopy).CopyTo(MemoryMarshal.Cast<sbyte, byte>(output.Slice(length - bytesLeft, sizeToCopy)));
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;
#endif
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, [InstantHandle] IList<sbyte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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

            int readCt;
            sbyte[]? arr = null;
            int arrOffset = 0;
            if (setInsteadOfAdding && output is sbyte[] arr1)
            {
                arr = arr1;
            }
            else if (setInsteadOfAdding && output is ArraySegment<sbyte> arr2)
            {
                arr = arr2.Array;
                arrOffset = arr2.Offset;
            }
            else if (output is List<sbyte> list)
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                readCt = stream.Read(MemoryMarshal.Cast<sbyte, byte>(arr.AsSpan(arrOffset, length)));
                bytesRead += readCt;
                if (readCt != length)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
#else
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

                        Buffer.BlockCopy(buffer, 0, arr, arrOffset, length);
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(buffer);
                    }
                }
                else if (length <= DefaultSerializer.MaxBufferSize)
                {
                    byte[] buffer = new byte[length];
                    readCt = stream.Read(buffer, 0, length);
                    if (readCt != length)
                    {
                        bytesRead += readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    Buffer.BlockCopy(buffer, 0, arr, arrOffset, length);
                }
                else
                {
                    byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                    int bytesLeft = length;
                    do
                    {
                        int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                        readCt = stream.Read(buffer, 0, sizeToCopy);
                        if (readCt != sizeToCopy)
                        {
                            bytesRead += bytesLeft - length + readCt;
                            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                        }

                        Buffer.BlockCopy(buffer, 0, arr, arrOffset + (length - bytesLeft), sizeToCopy);
                        bytesLeft -= sizeToCopy;
                    } while (bytesLeft > 0);
                }
                bytesRead += length;
#endif
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
                            output[i] = unchecked((sbyte)buffer[i]);
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                            output.Add(unchecked((sbyte)buffer[i]));
                    }
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (length <= DefaultSerializer.MaxBufferSize)
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
                        output[i] = unchecked((sbyte)buffer[i]);
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                        output.Add(unchecked((sbyte)buffer[i]));
                }
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
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
                            output[stInd + i] = unchecked((sbyte)buffer[i]);
                    }
                    else
                    {
                        for (int i = 0; i < sizeToCopy; ++i)
                            output.Add(unchecked((sbyte)buffer[i]));
                    }
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;

            return length;
        }
    }
}