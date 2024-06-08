//#define SIM_BIG_ENDIAN
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

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
        private static readonly Type BoolCollectionType = typeof(ICollection<bool>);
        private static readonly Type BoolRoCollectionType = typeof(IReadOnlyCollection<bool>);
        private static readonly Type BoolEnumerableType = typeof(IEnumerable<bool>);
        private static readonly Type BoolArrSegmentType = typeof(ArraySegment<bool>);
        private static readonly Type BoolSpanType = typeof(Span<bool>);
        private static readonly Type BoolRoSpanType = typeof(ReadOnlySpan<bool>);
        private static readonly Type BoolSpanPtrType = typeof(Span<bool>*);
        private static readonly Type BoolRoSpanPtrType = typeof(ReadOnlySpan<bool>*);
        private readonly SerializationConfiguration _config;
        public Many(SerializationConfiguration config)
        {
            _config = config;
            _config.Lock();
        }
        /// <inheritdoc />
        public bool IsVariableSize => true;

        /// <inheritdoc />
        public int MinimumSize => 1;
        private static int CalcLen(int length)
        {
            if (length == 0)
                return 1;
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

            if (length == 0)
            {
                bytesRead = (int)index;
                return new BitArray(0);
            }

            int size = (length - 1) / 8 + 1;
            bytesRead = (int)index + size;

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            BitArray arr = new BitArray(length);

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

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int size = (length - 1) / 8 + 1;

            bool needsReturn = false;
            int rdCt;

#if !SIM_BIG_ENDIAN
            bool isLittleEndian = BitConverter.IsLittleEndian;
#else
            const bool isLittleEndian = false;
#endif

            if (isLittleEndian && size > DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = new byte[size];

                rdCt = stream.Read(bytes, 0, size);
                bytesRead += rdCt;
                if (rdCt != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

                return new BitArray(bytes) { Length = length };
            }

            bool readPartial = false;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            scoped Span<byte> span;
            byte[]? arrayToReturn = null;
            if (size <= _config.MaximumStackAllocationSize)
            {
                span = stackalloc byte[size];
            }
            else if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                span = (arrayToReturn = DefaultSerializer.ArrayPool.Rent(size)).AsSpan(size);
                needsReturn = true;
            }
            else if (size <= _config.MaximumBufferSize)
            {
                span = new byte[size];
            }
            else
            {
                span = new byte[_config.MaximumBufferSize];
                readPartial = true;
            }

#else
            byte[] span;
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                span = DefaultSerializer.ArrayPool.Rent(size);
                needsReturn = true;
            }
            else if (size <= _config.MaximumBufferSize)
            {
                span = new byte[size];
            }
            else
            {
                span = new byte[_config.MaximumBufferSize];
                readPartial = true;
            }
#endif
            BitArray arr;
            try
            {
                if (!readPartial)
                {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    rdCt = stream.Read(span[..size]);
#else
                    rdCt = stream.Read(span, 0, size);
#endif
                    bytesRead += rdCt;
                    if (rdCt != size)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                arr = new BitArray(length);

                if (readPartial)
                {
                    int bytesLeft = size;
                    int elementsLeft = length;
                    do
                    {
                        int sizeToRead = Math.Min(span.Length, bytesLeft);
                        int elemToRead = Math.Min(span.Length * 8, elementsLeft);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        rdCt = stream.Read(span[..sizeToRead]);
#else
                        rdCt = stream.Read(span, 0, sizeToRead);
#endif
                        bytesRead += rdCt;
                        if (rdCt != sizeToRead)
                            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                        int index = 0;
                        byte current = span[0];
                        for (int i = 0; i < elemToRead; i++)
                        {
                            byte mod = (byte)(i % 8);
                            if (mod == 0 & i != 0)
                            {
                                ++index;
                                current = span[index];
                            }
                            arr.Set(i, (1 & (current >>> mod)) != 0);
                        }
                        bytesLeft -= sizeToRead;
                        elementsLeft -= elemToRead;
                    } while (bytesLeft > 0);
                }
                else
                {
                    int index = 0;
                    byte current = span[0];
                    for (int i = 0; i < length; i++)
                    {
                        byte mod = (byte)(i % 8);
                        if (mod == 0 & i != 0)
                        {
                            ++index;
                            current = span[index];
                        }
                        arr.Set(i, (1 & (current >>> mod)) != 0);
                    }
                }
            }
            finally
            {
                if (needsReturn)
                {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    DefaultSerializer.ArrayPool.Return(arrayToReturn!);
#else
                    DefaultSerializer.ArrayPool.Return(span);
#endif
                }
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

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

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

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int size = (length - 1) / 8 + 1;

            bool readPartial = false;
            bool needsReturn = false;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            scoped Span<byte> span;
            byte[]? arrayToReturn = null;
            if (size <= _config.MaximumStackAllocationSize)
            {
                span = stackalloc byte[size];
            }
            else if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                span = (arrayToReturn = DefaultSerializer.ArrayPool.Rent(size)).AsSpan(size);
                needsReturn = true;
            }
            else if (size <= _config.MaximumBufferSize)
            {
                span = new byte[size];
            }
            else
            {
                span = new byte[_config.MaximumBufferSize];
                readPartial = true;
            }

#else
            byte[] span;
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                span = DefaultSerializer.ArrayPool.Rent(size);
                needsReturn = true;
            }
            else if (size <= _config.MaximumBufferSize)
            {
                span = new byte[size];
            }
            else
            {
                span = new byte[_config.MaximumBufferSize];
                readPartial = true;
            }
#endif
            bool[] arr;
            try
            {
                int rdCt;
                if (!readPartial)
                {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    rdCt = stream.Read(span[..size]);
#else
                    rdCt = stream.Read(span, 0, size);
#endif
                    bytesRead += rdCt;
                    if (rdCt != size)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                arr = new bool[length];

                if (readPartial)
                {
                    int bytesLeft = size;
                    int elementsLeft = length;
                    do
                    {
                        int sizeToRead = Math.Min(span.Length, bytesLeft);
                        int elemToRead = Math.Min(span.Length * 8, elementsLeft);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                        rdCt = stream.Read(span[..sizeToRead]);
#else
                        rdCt = stream.Read(span, 0, sizeToRead);
#endif
                        bytesRead += rdCt;
                        if (rdCt != sizeToRead)
                            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                        int index = 0;
                        byte current = span[0];
                        int stInd = length - elementsLeft;
                        for (int i = 0; i < elemToRead; i++)
                        {
                            byte mod = (byte)(i % 8);
                            if (mod == 0 & i != 0)
                            {
                                ++index;
                                current = span[index];
                            }
                            arr[i + stInd] = (1 & (current >>> mod)) != 0;
                        }
                        bytesLeft -= sizeToRead;
                        elementsLeft -= elemToRead;
                    } while (bytesLeft > 0);
                }
                else
                {
                    int index = 0;
                    byte current = span[0];
                    for (int i = 0; i < length; i++)
                    {
                        byte mod = (byte)(i % 8);
                        if (mod == 0 & i != 0)
                        {
                            ++index;
                            current = span[index];
                        }
                        arr[i] = (1 & (current >>> mod)) != 0;
                    }
                }
            }
            finally
            {
                if (needsReturn)
                {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    DefaultSerializer.ArrayPool.Return(arrayToReturn!);
#else
                    DefaultSerializer.ArrayPool.Return(span);
#endif
                }
            }

            return arr;
        }

        /// <inheritdoc />
        public unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Count;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    int sz = (length - 1) / 8 + 1;
                    if (maxSize >= bytesRead + sz)
                        bytesRead += sz;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int size = (length - 1) / 8 + 1;
            bytes += bytesRead;
            bytesRead += size;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            bool[] arr = output.Array!;
            int ofs = output.Offset;

            byte current = *bytes;
            for (int i = 0; i < length; i++)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0 & i != 0)
                {
                    ++bytes;
                    current = *bytes;
                }
                arr[i + ofs] = (1 & (current >>> mod)) != 0;
            }

            return length;
        }

        /// <inheritdoc />
        public unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(bytes, maxSize, out bytesRead);
                if (length > output.Length)
                {
                    int sz = (length - 1) / 8 + 1;
                    if (maxSize >= bytesRead + sz)
                        bytesRead += sz;
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

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
        public unsafe int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<bool> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(false);
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
                            output.Add(false);
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

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

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
        public int ReadObject(Stream stream, [InstantHandle] ArraySegment<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Count;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Count || length > 0 && output.Array == null)
                {
                    SerializationHelper.TryAdvanceStream(stream, _config, ref bytesRead, (length - 1) / 8 + 1);
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int ct = ReadObject(stream, output.AsSpan(), out int bytesRead2, hasReadLength: true);
            bytesRead += bytesRead2;
            return ct;
        }
        private static void ReadToSpan([InstantHandle] scoped ReadOnlySpan<byte> bytes, [InstantHandle] scoped Span<bool> output)
        {
            byte current = 0;
            int index = -1;
            for (int i = 0; i < output.Length; ++i)
            {
                byte mod = (byte)(i % 8);
                if (mod == 0)
                {
                    ++index;
                    if (index >= bytes.Length)
                        break;
                    current = bytes[index];
                }

                output[i] = (1 & (current >>> mod)) != 0;
            }
        }
        /// <inheritdoc />
        public int ReadObject(Stream stream, [InstantHandle] scoped Span<bool> output, out int bytesRead, bool hasReadLength = true)
        {
            int length = output.Length;
            if (!hasReadLength)
            {
                length = ReadArrayLength(stream, out bytesRead);
                if (length > output.Length)
                {
                    SerializationHelper.TryAdvanceStream(stream, _config, ref bytesRead, (length - 1) / 8 + 1);
                    throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
                }
            }
            else bytesRead = 0;

            if (length == 0)
                return 0;

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int size = (length - 1) / 8 + 1;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                int rdCt = stream.Read(bytes);
                bytesRead += rdCt;
                if (rdCt != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                ReadToSpan(bytes, output);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    int rdCt = stream.Read(bytes, 0, size);
                    bytesRead += rdCt;
                    if (rdCt != size)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    ReadToSpan(bytes.AsSpan(0, size),  output);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                int rdCt = stream.Read(bytes, 0, size);
                bytesRead += rdCt;
                if (rdCt != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                ReadToSpan(bytes, output);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int rdCt = stream.Read(buffer, 0, sizeToCopy);
                    bytesRead += rdCt;
                    if (rdCt != sizeToCopy)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    
                    ReadToSpan(buffer.AsSpan(0, sizeToCopy), output.Slice((size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, output.Length - (size - bytesLeft) * 8)));
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length;
        }

        private static void ReadToList([InstantHandle] scoped Span<byte> bytes, [InstantHandle] IList<bool> output, int startIndex, int length, bool setInsteadOfAdding)
        {
            byte current = 0;
            int index = -1;
            if (setInsteadOfAdding)
            {
                for (int i = 0; i < length; ++i)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0)
                    {
                        ++index;
                        if (index >= bytes.Length)
                            break;
                        current = bytes[index];
                    }

                    output[startIndex + i] = (1 & (current >>> mod)) != 0;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0)
                    {
                        ++index;
                        if (index >= bytes.Length)
                            break;
                        current = bytes[index];
                    }

                    output.Add((1 & (current >>> mod)) != 0);
                }
            }
        }

        /// <inheritdoc />
        public int ReadObject(Stream stream, [InstantHandle] IList<bool> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(false);
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
                            output.Add(false);
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

            _config.AssertCanCreateArrayOfType(typeof(bool), length, this);

            int size = (length - 1) / 8 + 1;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                int rdCt = stream.Read(bytes);
                bytesRead += rdCt;
                if (rdCt != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                
                ReadToList(bytes, output, 0, length, setInsteadOfAdding);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    int rdCt = stream.Read(bytes, 0, size);
                    bytesRead += rdCt;
                    if (rdCt != size)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    
                    ReadToList(bytes.AsSpan(0, size), output, 0, length, setInsteadOfAdding);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                int rdCt = stream.Read(bytes, 0, size);
                bytesRead += rdCt;
                if (rdCt != size)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                
                ReadToList(bytes, output, 0, length, setInsteadOfAdding);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int rdCt = stream.Read(buffer, 0, sizeToCopy);
                    bytesRead += rdCt;
                    if (rdCt != sizeToCopy)
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

                    ReadToList(buffer.AsSpan(0, sizeToCopy), output, (size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8), setInsteadOfAdding);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return length;
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] bool[]? value, byte* bytes, uint maxSize)
        {
            return WriteObject(value == null ? default : new ArraySegment<bool>(value), bytes, maxSize);
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] bool[]? value, Stream stream)
        {
            return WriteObject(value == null ? default : new ArraySegment<bool>(value), stream);
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] ArraySegment<bool> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

            bool[] arr = value.Array;
            int ofs = value.Offset;
            bytes += index;
            byte current = 0;
            for (int i = 0; i < len; i++)
            {
                bool c = arr[ofs + i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    *bytes = current;
                    ++bytes;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] IList<bool>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }
            if (value is bool[] arr)
            {
                return WriteObject(arr, bytes, maxSize);
            }
            if (value is List<bool> list && Accessor.TryGetUnderlyingArray(list, out bool[] underlyingArray))
            {
                return WriteObject(new ArraySegment<bool>(underlyingArray, 0, list.Count), bytes, maxSize);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

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

            *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] IReadOnlyList<bool>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }
            if (value is bool[] arr)
            {
                return WriteObject(arr, bytes, maxSize);
            }
            if (value is List<bool> list && Accessor.TryGetUnderlyingArray(list, out bool[] underlyingArray))
            {
                return WriteObject(new ArraySegment<bool>(underlyingArray, 0, list.Count), bytes, maxSize);
            }

            int len = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

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

            *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] ICollection<bool>? value, byte* bytes, uint maxSize)
        {
            return WriteObject((IEnumerable<bool>?)value, bytes, maxSize);
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] IReadOnlyCollection<bool>? value, byte* bytes, uint maxSize)
        {
            return WriteObject((IEnumerable<bool>?)value, bytes, maxSize);
        }
        /// <inheritdoc />
        unsafe BitArray? IBinaryTypeParser<BitArray>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBitArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        BitArray? IBinaryTypeParser<BitArray>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBitArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        unsafe IList<bool>? IBinaryTypeParser<IList<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBooleanArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        unsafe IReadOnlyList<bool>? IBinaryTypeParser<IReadOnlyList<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBooleanArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        unsafe ICollection<bool>? IBinaryTypeParser<ICollection<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBooleanArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        unsafe IReadOnlyCollection<bool>? IBinaryTypeParser<IReadOnlyCollection<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBooleanArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        unsafe IEnumerable<bool>? IBinaryTypeParser<IEnumerable<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            return ReadBooleanArray(bytes, maxSize, out bytesRead);
        }

        /// <inheritdoc />
        unsafe ArraySegment<bool> IBinaryTypeParser<ArraySegment<bool>>.ReadObject(byte* bytes, uint maxSize, out int bytesRead)
        {
            bool[]? arr = ReadBooleanArray(bytes, maxSize, out bytesRead);
            return arr == null ? default : new ArraySegment<bool>(arr);
        }

        /// <inheritdoc />
        IList<bool>? IBinaryTypeParser<IList<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBooleanArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyList<bool>? IBinaryTypeParser<IReadOnlyList<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBooleanArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        ICollection<bool>? IBinaryTypeParser<ICollection<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBooleanArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        IReadOnlyCollection<bool>? IBinaryTypeParser<IReadOnlyCollection<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBooleanArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        IEnumerable<bool>? IBinaryTypeParser<IEnumerable<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            return ReadBooleanArray(stream, out bytesRead);
        }

        /// <inheritdoc />
        ArraySegment<bool> IBinaryTypeParser<ArraySegment<bool>>.ReadObject(Stream stream, out int bytesRead)
        {
            bool[]? arr = ReadBooleanArray(stream, out bytesRead);
            return arr == null ? default : new ArraySegment<bool>(arr);
        }

        /// <inheritdoc />
        unsafe bool[]? IBinaryTypeParser<bool[]>.ReadObject(byte* bytes, uint maxSize, out int bytesRead) => ReadBooleanArray(bytes, maxSize, out bytesRead);

        /// <inheritdoc />
        bool[]? IBinaryTypeParser<bool[]>.ReadObject(Stream stream, out int bytesRead) => ReadBooleanArray(stream, out bytesRead);

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] IEnumerable<bool>? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }
            if (value is IList<bool> list2)
            {
                return WriteObject(list2, bytes, maxSize);
            }
            if (value is IReadOnlyList<bool> roList)
            {
                return WriteObject(roList, bytes, maxSize);
            }

            int actualCount = 0;
            int byteCt = 2;
            byte* startPtr = bytes + 2;
            using (IEnumerator<bool> enumerator = value.GetEnumerator())
            {
                byte current = 0;
                while (enumerator.MoveNext())
                {
                    bool c = enumerator.Current;
                    int mod = actualCount % 8;
                    if (mod == 0 && actualCount != 0)
                    {
                        *startPtr = current;
                        if (byteCt >= maxSize)
                            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };
                        ++byteCt;
                        ++startPtr;
                        current = (byte)(c ? 1 : 0);
                    }
                    else if (c) current |= (byte)(1 << mod);
                    ++actualCount;
                }

                if (actualCount > 0)
                {
                    *startPtr = current;
                    ++byteCt;
                    ++startPtr;
                }
            }

            byteCt -= 2;

            startPtr -= byteCt;
            if (actualCount == 0)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, actualCount, false, this);
            }

            int hdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount, false));
            if (hdrSize != 2)
            {
                if (Compatibility.IncompatibleWithBufferMemoryCopyOverlap)
                {
                    for (int i = byteCt - 1; i >= 0; --i)
                    {
                        *(bytes + hdrSize + i) = *(startPtr + i);
                    }
                }
                else
                {
                    Buffer.MemoryCopy(startPtr, bytes + hdrSize, maxSize - hdrSize, byteCt);
                }
            }

            SerializationHelper.WriteStandardArrayHeader(bytes, (uint)hdrSize, ref index, actualCount, false, this);
            return byteCt + hdrSize;
        }
        
        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] scoped ReadOnlySpan<bool> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int len = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, len, false, this);

            if (len == 0)
                return hdrSize;

            int byteSize = (len - 1) / 8 + 1;

            if (maxSize - hdrSize < byteSize)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, GetType().Name)) { ErrorCode = 1 };

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

            *bytes = current;

            return hdrSize + byteSize;
        }

        private static void WriteFromArrSeg([InstantHandle] ArraySegment<bool> value, [InstantHandle] scoped Span<byte> output)
        {
            bool[] arr = value.Array!;
            int ofs = value.Offset;
            int index = 0;
            byte current = 0;
            for (int i = 0; i < value.Count; i++)
            {
                bool c = arr[ofs + i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            output[index] = current;
        }
        private static void WriteFromSpan([InstantHandle] scoped ReadOnlySpan<bool> value, [InstantHandle] scoped Span<byte> output)
        {
            int index = 0;
            byte current = 0;
            for (int i = 0; i < value.Length; i++)
            {
                bool c = value[i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            output[index] = current;
        }
        private static void WriteFromList([InstantHandle] IList<bool> value, int startIndex, int count, [InstantHandle] scoped Span<byte> output)
        {
            int index = 0;
            byte current = 0;
            for (int i = 0; i < count; i++)
            {
                bool c = value[startIndex + i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            output[index] = current;
        }
        private static void WriteFromList([InstantHandle] IReadOnlyList<bool> value, int startIndex, int count, [InstantHandle] scoped Span<byte> output)
        {
            int index = 0;
            byte current = 0;
            for (int i = 0; i < count; i++)
            {
                bool c = value[startIndex + i];
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            output[index] = current;
        }
        private static void WriteFromBitArray([InstantHandle] BitArray value, int startIndex, int count, [InstantHandle] scoped Span<byte> output)
        {
            int index = 0;
            byte current = 0;
            for (int i = 0; i < count; i++)
            {
                bool c = value.Get(startIndex + i);
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
            }

            output[index] = current;
        }
        private static void WriteFromEnumerable([InstantHandle] IEnumerable<bool> enumerable, [InstantHandle] scoped Span<byte> output)
        {
            using IEnumerator<bool> enumerator = enumerable.GetEnumerator();
            WriteFromEnumerator(enumerator, output);
        }
        private static void WriteFromEnumerator([InstantHandle] IEnumerator<bool> enumerator, [InstantHandle] scoped Span<byte> output)
        {
            int i = 0;
            int index = 0;
            byte current = 0;
            bool any = false;
            while (enumerator.MoveNext())
            {
                any = true;
                bool c = enumerator.Current;
                int mod = i % 8;
                if (mod == 0 && i != 0)
                {
                    output[index] = current;
                    ++index;
                    current = (byte)(c ? 1 : 0);
                }
                else if (c) current |= (byte)(1 << mod);
                ++i;
            }

            if (any)
                output[index] = current;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ArraySegment<bool> value, Stream stream)
        {
            if (value.Array == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (value.Count == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromArrSeg(value, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromArrSeg(value, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromArrSeg(value, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    WriteFromArrSeg(new ArraySegment<bool>(value.Array, value.Offset + (size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8)), buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IList<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is bool[] arr)
            {
                return WriteObject(arr, stream);
            }
            if (value is List<bool> list && Accessor.TryGetUnderlyingArray(list, out bool[] underlyingArray))
            {
                return WriteObject(new ArraySegment<bool>(underlyingArray, 0, list.Count), stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromList(value, 0, length, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromList(value, 0, length, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromList(value, 0, length, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    WriteFromList(value, (size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8), buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyList<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is bool[] arr)
            {
                return WriteObject(arr, stream);
            }
            if (value is List<bool> list && Accessor.TryGetUnderlyingArray(list, out bool[] underlyingArray))
            {
                return WriteObject(new ArraySegment<bool>(underlyingArray, 0, list.Count), stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromList(value, 0, length, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromList(value, 0, length, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromList(value, 0, length, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    WriteFromList(value, (size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8), buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] ICollection<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is IList<bool> list1)
            {
                return WriteObject(list1, stream);
            }
            if (value is IReadOnlyList<bool> list2)
            {
                return WriteObject(list2, stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromEnumerable(value, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromEnumerable(value, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromEnumerable(value, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                using IEnumerator<bool> enumerator = value.GetEnumerator();
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int elementsToCopy = Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8);

                    int i = 0;
                    int index = 0;
                    byte current = 0;
                    bool any = false;
                    while (i < elementsToCopy && enumerator.MoveNext())
                    {
                        any = true;
                        bool c = enumerator.Current;
                        int mod = i % 8;
                        if (mod == 0 && i != 0)
                        {
                            buffer[index] = current;
                            ++index;
                            current = (byte)(c ? 1 : 0);
                        }
                        else if (c) current |= (byte)(1 << mod);
                        ++i;
                    }

                    if (any)
                        buffer[index] = current;

                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] IReadOnlyCollection<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is IList<bool> list1)
            {
                return WriteObject(list1, stream);
            }
            if (value is IReadOnlyList<bool> list2)
            {
                return WriteObject(list2, stream);
            }

            int length = value.Count;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromEnumerable(value, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromEnumerable(value, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromEnumerable(value, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                using IEnumerator<bool> enumerator = value.GetEnumerator();
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int elementsToCopy = Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8);

                    int i = 0;
                    int index = 0;
                    byte current = 0;
                    bool any = false;
                    while (i < elementsToCopy && enumerator.MoveNext())
                    {
                        any = true;
                        bool c = enumerator.Current;
                        int mod = i % 8;
                        if (mod == 0 && i != 0)
                        {
                            buffer[index] = current;
                            ++index;
                            current = (byte)(c ? 1 : 0);
                        }
                        else if (c) current |= (byte)(1 << mod);
                        ++i;
                    }

                    if (any)
                        buffer[index] = current;

                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        private static void ResetOrReMake(ref IEnumerator<bool> enumerator, IEnumerable<bool> enumerable)
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
        public int WriteObject([InstantHandle] IEnumerable<bool>? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }
            if (value is ICollection<bool> col1)
            {
                return WriteObject(col1, stream);
            }
            if (value is IReadOnlyCollection<bool> col2)
            {
                return WriteObject(col2, stream);
            }
            if (value is IList<bool> list1)
            {
                return WriteObject(list1, stream);
            }
            if (value is IReadOnlyList<bool> list2)
            {
                return WriteObject(list2, stream);
            }

            IEnumerator<bool> enumerator = value.GetEnumerator();
            try
            {
                int length = 0;
                while (enumerator.MoveNext())
                {
                    checked { ++length; }
                }

                ResetOrReMake(ref enumerator, value);
                int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);
                if (length == 0)
                    return hdrSize;
                int size = (length - 1) / 8 + 1;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (size <= _config.MaximumStackAllocationSize)
                {
                    Span<byte> bytes = stackalloc byte[size];
                    WriteFromEnumerator(enumerator, bytes);
                    stream.Write(bytes);
                }
                else
#endif
                if (size <= DefaultSerializer.MaxArrayPoolSize)
                {
                    byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                    try
                    {
                        WriteFromEnumerator(enumerator, bytes);
                        stream.Write(bytes, 0, size);
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(bytes);
                    }
                }
                else if (size <= _config.MaximumBufferSize)
                {
                    byte[] bytes = new byte[size];
                    WriteFromEnumerator(enumerator, bytes);
                    stream.Write(bytes, 0, size);
                }
                else
                {
                    byte[] buffer = new byte[_config.MaximumBufferSize];
                    int bytesLeft = size;
                    do
                    {
                        int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                        int elementsToCopy = Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8);

                        int i = 0;
                        int index = 0;
                        byte current = 0;
                        bool any = false;
                        while (i < elementsToCopy && enumerator.MoveNext())
                        {
                            any = true;
                            bool c = enumerator.Current;
                            int mod = i % 8;
                            if (mod == 0 && i != 0)
                            {
                                buffer[index] = current;
                                ++index;
                                current = (byte)(c ? 1 : 0);
                            }
                            else if (c) current |= (byte)(1 << mod);
                            ++i;
                        }

                        if (any)
                            buffer[index] = current;

                        stream.Write(buffer, 0, sizeToCopy);
                        bytesLeft -= sizeToCopy;
                    } while (bytesLeft > 0);
                }

                return hdrSize + size;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] scoped ReadOnlySpan<bool> value, Stream stream)
        {
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (size <= _config.MaximumStackAllocationSize)
            {
                Span<byte> bytes = stackalloc byte[size];
                WriteFromSpan(value, bytes);
                stream.Write(bytes);
            }
            else
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    WriteFromSpan(value, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                WriteFromSpan(value, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    WriteFromSpan(value.Slice((size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8)), buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public unsafe int WriteObject([InstantHandle] BitArray? value, byte* bytes, uint maxSize)
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

            *bytes = current;

            return hdrSize + byteSize;
        }

        /// <inheritdoc />
        public int WriteObject([InstantHandle] BitArray? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (value.Length == 0)
                return hdrSize;

            int size = (length - 1) / 8 + 1;

#if !SIM_BIG_ENDIAN
            bool isLittleEndian = BitConverter.IsLittleEndian;
#else
            const bool isLittleEndian = false;
#endif
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] bytes = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    if (isLittleEndian)
                        value.CopyTo(bytes, 0);
                    else
                        WriteFromBitArray(value, 0, length, bytes);
                    stream.Write(bytes, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(bytes);
                }
            }
            else if (size <= _config.MaximumBufferSize)
            {
                byte[] bytes = new byte[size];
                if (isLittleEndian)
                    value.CopyTo(bytes, 0);
                else
                    WriteFromBitArray(value, 0, length, bytes);
                stream.Write(bytes, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_config.MaximumBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    WriteFromBitArray(value, (size - bytesLeft) * 8, Math.Min(sizeToCopy * 8, length - (size - bytesLeft) * 8), buffer.AsSpan(0, sizeToCopy));
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return hdrSize + size;
        }

        /// <inheritdoc />
        public unsafe int GetSize(TypedReference value)
        {
            Type t = __reftype(value);
            int len;
            if (t == BitArrType)
            {
                BitArray? bitArray = __refvalue(value, BitArray?);
                if (bitArray == null)
                    return 1;
                len = bitArray.Length;
            }
            else if (t == BoolArrType)
            {
                bool[]? arr = __refvalue(value, bool[]?);
                if (arr == null)
                    return 1;
                len = arr.Length;
            }
            else if (t == BoolListType)
            {
                IList<bool>? arr = __refvalue(value, IList<bool>?);
                if (arr == null)
                    return 1;
                len = arr.Count;
            }
            else if (t == BoolRoListType)
            {
                IReadOnlyList<bool>? arr = __refvalue(value, IReadOnlyList<bool>?);
                if (arr == null)
                    return 1;
                len = arr.Count;
            }
            else if (t == BoolCollectionType)
            {
                ICollection<bool>? arr = __refvalue(value, ICollection<bool>?);
                if (arr == null)
                    return 1;
                len = arr.Count;
            }
            else if (t == BoolRoCollectionType)
            {
                IReadOnlyCollection<bool>? arr = __refvalue(value, IReadOnlyCollection<bool>?);
                if (arr == null)
                    return 1;
                len = arr.Count;
            }
            else if (t == BoolEnumerableType)
            {
                IEnumerable<bool>? arr = __refvalue(value, IEnumerable<bool>?);
                if (arr == null)
                    return 1;
                len = arr.Count();
            }
            else if (t == BoolArrSegmentType)
                len = __refvalue(value, ArraySegment<bool>).Count;
            else if (t == BoolRoSpanType)
                len = __refvalue(value, ReadOnlySpan<bool>).Length;
            else if (t == BoolSpanType)
                len = __refvalue(value, Span<bool>).Length;
            else if (t == BoolRoSpanPtrType)
            {
                ReadOnlySpan<bool>* span = __refvalue(value, ReadOnlySpan<bool>*);
                if (span == null)
                    return 1;
                len = span->Length;
            }
            else if (t == BoolSpanPtrType)
            {
                Span<bool>* span = __refvalue(value, Span<bool>*);
                if (span == null)
                    return 1;
                len = span->Length;
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));

            return CalcLen(len);
        }

        /// <inheritdoc />
        public unsafe int WriteObject(TypedReference value, byte* bytes, uint maxSize)
        {
            Type t = __reftype(value);
            if (t == BitArrType)
                return WriteObject(__refvalue(value, BitArray?), bytes, maxSize);
            if (t == BoolArrType)
                return WriteObject(__refvalue(value, bool[]?), bytes, maxSize);
            if (t == BoolListType)
                return WriteObject(__refvalue(value, IList<bool>?), bytes, maxSize);
            if (t == BoolRoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<bool>?), bytes, maxSize);
            if (t == BoolCollectionType)
                return WriteObject(__refvalue(value, ICollection<bool>?), bytes, maxSize);
            if (t == BoolRoCollectionType)
                return WriteObject(__refvalue(value, IReadOnlyCollection<bool>?), bytes, maxSize);
            if (t == BoolEnumerableType)
                return WriteObject(__refvalue(value, IEnumerable<bool>?), bytes, maxSize);
            if (t == BoolArrSegmentType)
                return WriteObject(__refvalue(value, ArraySegment<bool>), bytes, maxSize);
            if (t == BoolRoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<bool>), bytes, maxSize);
            if (t == BoolSpanType)
                return WriteObject(__refvalue(value, Span<bool>), bytes, maxSize);
            if (t == BoolRoSpanPtrType)
                return WriteObject(*__refvalue(value, ReadOnlySpan<bool>*), bytes, maxSize);
            if (t == BoolSpanPtrType)
                return WriteObject(*__refvalue(value, Span<bool>*), bytes, maxSize);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public unsafe int WriteObject(TypedReference value, Stream stream)
        {
            Type t = __reftype(value);
            if (t == BitArrType)
                return WriteObject(__refvalue(value, BitArray?), stream);
            if (t == BoolArrType)
                return WriteObject(__refvalue(value, bool[]?), stream);
            if (t == BoolListType)
                return WriteObject(__refvalue(value, IList<bool>?), stream);
            if (t == BoolRoListType)
                return WriteObject(__refvalue(value, IReadOnlyList<bool>?), stream);
            if (t == BoolCollectionType)
                return WriteObject(__refvalue(value, ICollection<bool>?), stream);
            if (t == BoolRoCollectionType)
                return WriteObject(__refvalue(value, IReadOnlyCollection<bool>?), stream);
            if (t == BoolEnumerableType)
                return WriteObject(__refvalue(value, IEnumerable<bool>?), stream);
            if (t == BoolArrSegmentType)
                return WriteObject(__refvalue(value, ArraySegment<bool>), stream);
            if (t == BoolRoSpanType)
                return WriteObject(__refvalue(value, ReadOnlySpan<bool>), stream);
            if (t == BoolSpanType)
                return WriteObject(__refvalue(value, Span<bool>), stream);
            if (t == BoolRoSpanPtrType)
                return WriteObject(*__refvalue(value, ReadOnlySpan<bool>*), stream);
            if (t == BoolSpanPtrType)
                return WriteObject(*__refvalue(value, Span<bool>*), stream);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }
        private static ArraySegment<bool> EmptySegment()
        {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return ArraySegment<bool>.Empty;
#else
            return new ArraySegment<bool>(Array.Empty<bool>());
#endif
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
            else if (t == BoolCollectionType)
                __refvalue(outObj, ICollection<bool>?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolRoCollectionType)
                __refvalue(outObj, IReadOnlyCollection<bool>?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolEnumerableType)
                __refvalue(outObj, IEnumerable<bool>?) = ReadBooleanArray(bytes, maxSize, out bytesRead);
            else if (t == BoolArrSegmentType)
            {
                bool[]? arr = ReadBooleanArray(bytes, maxSize, out bytesRead);
                __refvalue(outObj, ArraySegment<bool>) = arr == null ? EmptySegment() : new ArraySegment<bool>(arr);
            }
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
            else if (t == BoolRoSpanPtrType)
                *__refvalue(outObj, ReadOnlySpan<bool>*) = ReadBooleanArray(bytes, maxSize, out bytesRead).AsSpan();
            else if (t == BoolSpanPtrType)
            {
                Span<bool>* existingSpan = __refvalue(outObj, Span<bool>*);
                if (!existingSpan->IsEmpty)
                {
                    ReadObject(bytes, maxSize, *existingSpan, out bytesRead, false);
                }
                else
                {
                    *existingSpan = ReadBooleanArray(bytes, maxSize, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public unsafe void ReadObject(Stream stream, out int bytesRead, TypedReference outObj)
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
            else if (t == BoolCollectionType)
                __refvalue(outObj, ICollection<bool>?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolRoCollectionType)
                __refvalue(outObj, IReadOnlyCollection<bool>?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolEnumerableType)
                __refvalue(outObj, IEnumerable<bool>?) = ReadBooleanArray(stream, out bytesRead);
            else if (t == BoolArrSegmentType)
            {
                bool[]? arr = ReadBooleanArray(stream, out bytesRead);
                __refvalue(outObj, ArraySegment<bool>) = arr == null ? EmptySegment() : new ArraySegment<bool>(arr);
            }
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
            else if (t == BoolRoSpanPtrType)
                *__refvalue(outObj, ReadOnlySpan<bool>*) = ReadBooleanArray(stream, out bytesRead).AsSpan();
            else if (t == BoolSpanPtrType)
            {
                Span<bool>* existingSpan = __refvalue(outObj, Span<bool>*);
                if (!existingSpan->IsEmpty)
                {
                    ReadObject(stream, *existingSpan, out bytesRead, false);
                }
                else
                {
                    *existingSpan = ReadBooleanArray(stream, out bytesRead).AsSpan();
                }
            }
            else
                throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(t), Accessor.ExceptionFormatter.Format(GetType())));
        }

        /// <inheritdoc />
        public int GetSize([InstantHandle] BitArray? value) => value == null ? 1 : CalcLen(value.Length);

        /// <inheritdoc />
        public int GetSize([InstantHandle] bool[]? value) => value == null ? 1 : CalcLen(value.Length);

        /// <inheritdoc />
        public int GetSize([InstantHandle] ArraySegment<bool> value) => value.Array == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize([InstantHandle] IList<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize([InstantHandle] IReadOnlyList<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize([InstantHandle] ICollection<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize([InstantHandle] IReadOnlyCollection<bool>? value) => value == null ? 1 : CalcLen(value.Count);

        /// <inheritdoc />
        public int GetSize([InstantHandle] IEnumerable<bool>? value) => value == null ? 1 : CalcLen(value.Count());

        /// <inheritdoc />
        public int GetSize([InstantHandle] scoped ReadOnlySpan<bool> value) => CalcLen(value.Length);

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
                case ArraySegment<bool> arr:
                    len = arr.Count;
                    break;
                case IList<bool> list:
                    len = list.Count;
                    break;
                case IReadOnlyList<bool> list:
                    len = list.Count;
                    break;
                case ICollection<bool> collection:
                    len = collection.Count;
                    break;
                case IReadOnlyCollection<bool> collection:
                    len = collection.Count;
                    break;
                case IEnumerable<bool> enu:
                    len = enu.Count();
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
                ArraySegment<bool> arr => WriteObject(arr, bytes, maxSize),
                IList<bool> list => WriteObject(list, bytes, maxSize),
                IReadOnlyList<bool> list => WriteObject(list, bytes, maxSize),
                ICollection<bool> collection => WriteObject(collection, bytes, maxSize),
                IReadOnlyCollection<bool> collection => WriteObject(collection, bytes, maxSize),
                IEnumerable<bool> enu => WriteObject(enu, bytes, maxSize),
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
                ArraySegment<bool> arr => WriteObject(arr, stream),
                IList<bool> list => WriteObject(list, stream),
                IReadOnlyList<bool> list => WriteObject(list, stream),
                ICollection<bool> collection => WriteObject(collection, stream),
                IReadOnlyCollection<bool> collection => WriteObject(collection, stream),
                IEnumerable<bool> enu => WriteObject(enu, stream),
                null => WriteObject((bool[])null!, stream),
                _ => throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(value.GetType()), Accessor.ExceptionFormatter.Format(GetType())))
            };
        }

        /// <inheritdoc />
        public unsafe object? ReadObject(Type type, byte* bytes, uint maxSize, out int bytesRead)
        {
            if (type == BitArrType)
                return ReadBitArray(bytes, maxSize, out bytesRead);
            if (type == BoolArrType || type == BoolListType || type == BoolRoListType)
                return ReadBooleanArray(bytes, maxSize, out bytesRead);
            if (type == BoolArrSegmentType)
            {
                bool[]? arr = ReadBooleanArray(bytes, maxSize, out bytesRead);
                return arr == null ? EmptySegment() : new ArraySegment<bool>(arr);
            }

            if (type.IsAssignableFrom(BoolArrType))
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
            if (type == BoolArrSegmentType)
            {
                bool[]? arr = ReadBooleanArray(stream, out bytesRead);
                return arr == null ? EmptySegment() : new ArraySegment<bool>(arr);
            }

            if (type.IsAssignableFrom(BoolArrType))
                return ReadBooleanArray(stream, out bytesRead);

            throw new InvalidCastException(string.Format(Properties.Exceptions.InvalidCastExceptionInvalidType, Accessor.ExceptionFormatter.Format(type), Accessor.ExceptionFormatter.Format(GetType())));
        }
    }
}