using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Subclass of <see cref="ArrayBinaryTypeParser{T}"/> to take some boilerplate away from writing array parsers for quickly convertible unmanaged, fixed-size types.
/// Supports arrays, <see cref="IList{T}"/>,
/// <see cref="IReadOnlyList{T}"/>, <see cref="IEnumerable{T}"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>,
/// <see cref="ArraySegment{T}"/>, <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> pointers (with <see cref="TypedReference"/>'s), 
/// and <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
/// </summary>
/// <remarks>Uses function pointers instead of virtual functions for performance.</remarks>
/// <typeparam name="TElementType">The element type to parse.</typeparam>
public unsafe class UnmanagedConvValueTypeBinaryArrayTypeParser<TElementType> : ArrayBinaryTypeParser<TElementType> where TElementType : unmanaged
{
    // not virtual for performance
    private readonly delegate*<byte*, TElementType, void> _writeToBuffer;
    private readonly delegate*<byte*, TElementType, void> _writeToBufferUnaligned;
    private readonly delegate*<Span<byte>, TElementType, void> _writeToBufferSpan;
    private readonly delegate*<byte*, TElementType> _readFromBuffer;
    private readonly delegate*<byte*, TElementType> _readFromBufferUnaligned;
    private readonly delegate*<Span<byte>, TElementType> _readFromBufferSpan;

    protected readonly SerializationConfiguration Configuration;
    private readonly bool _flipBits;
    private readonly int _maxBufferSize;
    private readonly int _elementSize;
    private readonly int _alignSize;
    public override int ElementSize => _elementSize;

    /// <summary>
    /// Create a new <see cref="UnmanagedConvValueTypeBinaryArrayTypeParser{TElementType}"/> with the given read/write functions.
    /// </summary>
    /// <param name="config">The configuration to use when parsing.</param>
    /// <param name="elementSize">Size in bytes of each element. This will affect the rounding of <see cref="SerializationConfiguration.MaximumBufferSize"/> as well.</param>
    /// <param name="alignSize">What size in bytes should be considered 'aligned'. This is usually the element size but may sometimes be the smallest primitive type in a union-like structure.</param>
    /// <param name="flipBits">If the bits should be flipped. Usually this will equal <c>!<see cref="BitConverter.IsLittleEndian"/></c>.</param>
    /// <param name="writeToBuffer">Pointer to a static function to write to the buffer when the pointer is aligned to <paramref name="alignSize"/>.</param>
    /// <param name="writeToBufferUnaligned">Pointer to a static function to write to the buffer when the pointer is not aligned to <paramref name="alignSize"/>.</param>
    /// <param name="writeToBufferSpan">Pointer to a static function to write to a span of bytes.</param>
    /// <param name="readFromBuffer">Pointer to a static function to read from the buffer when the pointer is aligned to <paramref name="alignSize"/>.</param>
    /// <param name="readFromBufferUnaligned">Pointer to a static function to read from the buffer when the pointer is not aligned to <paramref name="alignSize"/>.</param>
    /// <param name="readFromBufferSpan">Pointer to a static function to read from a span of bytes.</param>
    protected UnmanagedConvValueTypeBinaryArrayTypeParser(SerializationConfiguration config, int elementSize, int alignSize, bool flipBits,
        delegate*<byte*, TElementType, void> writeToBuffer,
        delegate*<byte*, TElementType, void> writeToBufferUnaligned,
        delegate*<Span<byte>, TElementType, void> writeToBufferSpan,
        delegate*<byte*, TElementType> readFromBuffer,
        delegate*<byte*, TElementType> readFromBufferUnaligned,
        delegate*<Span<byte>, TElementType> readFromBufferSpan)
    {
        Configuration = config;
        Configuration.Lock();
        _flipBits = flipBits;
        _elementSize = elementSize;
        _alignSize = alignSize;
        _maxBufferSize = config.MaximumBufferSize / elementSize * elementSize;
        _writeToBuffer = writeToBuffer;
        _writeToBufferUnaligned = writeToBufferUnaligned;
        _writeToBufferSpan = writeToBufferSpan;
        _readFromBuffer = readFromBuffer;
        _readFromBufferUnaligned = readFromBufferUnaligned;
        _readFromBufferSpan = readFromBufferSpan;
    }
    protected virtual TElementType FlipBits(TElementType toFlip)
    {
        byte* ptr = (byte*)&toFlip;
        int elementSize = _elementSize;
        for (int i = 0; i < elementSize / 2; ++i)
        {
            byte b = ptr[i];
            int ind = elementSize - i - 1;
            ptr[i] = ptr[ind];
            ptr[ind] = b;
        }

        return toFlip;
    }
    protected virtual void FlipBits(byte* bytes, int hdrSize, int size)
    {
        bytes += hdrSize;
        byte* end = bytes + size;
        int elementSize = _elementSize;
        while (bytes < end)
        {
            for (int i = 0; i < elementSize / 2; ++i)
            {
                byte b = bytes[i];
                int ind = elementSize - i - 1;
                bytes[i] = bytes[ind];
                bytes[ind] = b;
            }

            bytes += elementSize;
        }
    }
    protected virtual void FlipBits([InstantHandle] byte[] bytes, int index, int size)
    {
        int elementSize = _elementSize;
        for (; index < size; index += elementSize)
        {
            for (int i = 0; i < elementSize / 2; ++i)
            {
                int stInd = i + index;
                byte b = bytes[stInd];
                int ind = elementSize - i - 1 + index;
                bytes[stInd] = bytes[ind];
                bytes[ind] = b;
            }
        }
    }
    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ArraySegment<TElementType> value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value.Array == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        TElementType[] arr = value.Array;
        int ofs = value.Offset;
        delegate*<byte*, TElementType, void> ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
        for (int i = 0; i < length; ++i)
        {
            ftn(bytes + hdrSize + i * elementSize, arr[ofs + i]);
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] scoped ReadOnlySpan<TElementType> value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        int length = value.Length;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        delegate*<byte*, TElementType, void> ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
        for (int i = 0; i < length; ++i)
        {
            ftn(bytes + hdrSize + i * elementSize, value[i]);
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IList<TElementType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        switch (value)
        {
            case TElementType[] b:
                delegate*<byte*, TElementType, void> ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + hdrSize + i * elementSize, b[i]);
                }
                break;

            case List<TElementType> l when Accessor.TryGetUnderlyingArray(l, out TElementType[] underlying):
                ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + hdrSize + i * elementSize, underlying[i]);
                }
                break;

            default:
                bytes += hdrSize;
                ftn = (nint)bytes % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + i * elementSize, value[i]);
                }
                bytes -= hdrSize;
                break;
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyList<TElementType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        switch (value)
        {
            case TElementType[] b:
                delegate*<byte*, TElementType, void> ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + hdrSize + i * elementSize, b[i]);
                }
                break;

            case List<TElementType> l when Accessor.TryGetUnderlyingArray(l, out TElementType[] underlying):
                ftn = (nint)(bytes + hdrSize) % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + hdrSize + i * elementSize, underlying[i]);
                }
                break;

            default:
                bytes += hdrSize;
                ftn = (nint)bytes % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
                for (int i = 0; i < length; ++i)
                {
                    ftn(bytes + i * elementSize, value[i]);
                }
                bytes -= hdrSize;
                break;
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ICollection<TElementType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytes += hdrSize;
        using IEnumerator<TElementType> enumerator = value.GetEnumerator();
        int i = 0;

        delegate*<byte*, TElementType, void> ftn = (nint)bytes % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
        while (i < length && enumerator.MoveNext())
        {
            ftn(bytes + i++ * elementSize, enumerator.Current);
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, 0, size);

        if (i == length)
            return i * elementSize + hdrSize;

        bytes -= hdrSize;
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

        return i * elementSize + newHdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyCollection<TElementType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytes += hdrSize;
        using IEnumerator<TElementType> enumerator = value.GetEnumerator();
        int i = 0;
        delegate*<byte*, TElementType, void> ftn = (nint)bytes % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
        while (i < length && enumerator.MoveNext())
        {
            ftn(bytes + i++ * elementSize, enumerator.Current);
        }

        bytes -= hdrSize;

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, hdrSize, size);

        if (i == length)
            return i * elementSize + hdrSize;

        bytes -= hdrSize;
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

        return i * elementSize + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IEnumerable<TElementType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }
        if (value is ICollection<TElementType> collection)
        {
            return WriteObject(collection, bytes, maxSize);
        }
        if (value is IReadOnlyCollection<TElementType> collection2)
        {
            return WriteObject(collection2, bytes, maxSize);
        }

        int actualCount = 0;
        int elementSize = _elementSize;
        using (IEnumerator<TElementType> enumerator = value.GetEnumerator())
        {
            delegate*<byte*, TElementType, void> ftn = (nint)bytes % _alignSize == 0 ? _writeToBuffer : _writeToBufferUnaligned;
            while (enumerator.MoveNext())
            {
                if (maxSize < actualCount + 1)
                    throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                ftn(bytes + actualCount, enumerator.Current);
                actualCount += elementSize;
            }
        }

        if (!BitConverter.IsLittleEndian && _flipBits)
            FlipBits(bytes, 0, actualCount);

        int newHdrSize = SerializationHelper.GetHeaderSize(SerializationHelper.GetLengthFlag(actualCount / elementSize, false));
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
        SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, actualCount / elementSize, false, this);
        return actualCount + newHdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ArraySegment<TElementType> value, Stream stream)
    {
        if (value.Array == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        TElementType[] arr = value.Array;
        int ofs = value.Offset;
        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                for (int i = 0; i < length; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), arr[ofs + i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            for (int i = 0; i < length; ++i)
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), arr[ofs + i]);
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int offset = ofs + (size - bytesLeft) / elementSize;
                int elementsToCopy = sizeToCopy / elementSize;
                for (int i = 0; i < elementsToCopy; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), arr[offset + i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] scoped ReadOnlySpan<TElementType> value, Stream stream)
    {
        int length = value.Length;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                for (int i = 0; i < length; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            for (int i = 0; i < length; ++i)
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int ofs = (size - bytesLeft) / elementSize;
                int elementsToCopy = sizeToCopy / elementSize;
                for (int i = 0; i < elementsToCopy; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[ofs + i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IList<TElementType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is TElementType[] arr)
        {
            return WriteObject(new ArraySegment<TElementType>(arr), stream);
        }
        if (value is List<TElementType> list && Accessor.TryGetUnderlyingArray(list, out TElementType[] underlying))
        {
            return WriteObject(new ArraySegment<TElementType>(underlying, 0, list.Count), stream);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                for (int i = 0; i < length; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            for (int i = 0; i < length; ++i)
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int ofs = (size - bytesLeft) / elementSize;
                int elementsToCopy = sizeToCopy / elementSize;
                for (int i = 0; i < elementsToCopy; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[ofs + i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyList<TElementType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is TElementType[] arr)
        {
            return WriteObject(new ArraySegment<TElementType>(arr), stream);
        }
        if (value is List<TElementType> list && Accessor.TryGetUnderlyingArray(list, out TElementType[] underlying))
        {
            return WriteObject(new ArraySegment<TElementType>(underlying, 0, list.Count), stream);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                for (int i = 0; i < length; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            for (int i = 0; i < length; ++i)
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), value[i]);
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int ofs = (size - bytesLeft) / elementSize;
                int elementsToCopy = sizeToCopy / elementSize;
                for (int i = 0; i < elementsToCopy; ++i)
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), value[ofs + i]);
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ICollection<TElementType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, stream);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                using IEnumerator<TElementType> enumerator = value.GetEnumerator();
                int i = 0;
                while (i < length && enumerator.MoveNext())
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                    ++i;
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            using IEnumerator<TElementType> enumerator = value.GetEnumerator();
            int i = 0;
            while (i < length && enumerator.MoveNext())
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                ++i;
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            using IEnumerator<TElementType> enumerator = value.GetEnumerator();
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int elemToCopy = sizeToCopy / elementSize;
                int i = 0;
                while (i < elemToCopy && enumerator.MoveNext())
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                    ++i;
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyCollection<TElementType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, stream);
        }

        int length = value.Count;
        int elementSize = _elementSize;
        int size = length * elementSize;
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                using IEnumerator<TElementType> enumerator = value.GetEnumerator();
                int i = 0;
                while (i < length && enumerator.MoveNext())
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                    ++i;
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (size <= _maxBufferSize)
        {
            byte[] buffer = new byte[size];
            using IEnumerator<TElementType> enumerator = value.GetEnumerator();
            int i = 0;
            while (i < length && enumerator.MoveNext())
            {
                ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                ++i;
            }
            if (!BitConverter.IsLittleEndian && _flipBits)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            using IEnumerator<TElementType> enumerator = value.GetEnumerator();
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int elemToCopy = sizeToCopy / elementSize;
                int i = 0;
                while (i < elemToCopy && enumerator.MoveNext())
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                    ++i;
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IEnumerable<TElementType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TElementType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TElementType> list2)
        {
            return WriteObject(list2, stream);
        }

        IEnumerator<TElementType> enumerator = value.GetEnumerator();
        try
        {
            int length = 0;
            while (enumerator.MoveNext())
                checked { ++length; }

            int elementSize = _elementSize;
            int size = length * elementSize;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);
            if (length == 0)
                return hdrSize;

            ResetOrReMake(ref enumerator, value);

            delegate*<Span<byte>, TElementType, void> ftn = _writeToBufferSpan;
            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    int i = 0;
                    while (i < length && enumerator.MoveNext())
                    {
                        ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                        ++i;
                    }
                    if (!BitConverter.IsLittleEndian && _flipBits)
                        FlipBits(buffer, 0, size);
                    stream.Write(buffer, 0, size);
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (size <= _maxBufferSize)
            {
                byte[] buffer = new byte[size];
                int i = 0;
                while (i < length && enumerator.MoveNext())
                {
                    ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                    ++i;
                }
                if (!BitConverter.IsLittleEndian && _flipBits)
                    FlipBits(buffer, 0, size);
                stream.Write(buffer, 0, size);
            }
            else
            {
                byte[] buffer = new byte[_maxBufferSize];
                int bytesLeft = size;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    int elemToCopy = sizeToCopy / elementSize;
                    int i = 0;
                    while (i < elemToCopy && enumerator.MoveNext())
                    {
                        ftn(buffer.AsSpan(i * elementSize, elementSize), enumerator.Current);
                        ++i;
                    }
                    if (!BitConverter.IsLittleEndian && _flipBits)
                        FlipBits(buffer, 0, sizeToCopy);
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            return size + hdrSize;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    /// <inheritdoc />
    public override TElementType[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
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
            return Array.Empty<TElementType>();
        }

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        TElementType[] arr = new TElementType[length];
        int elementSize = _elementSize;
        int size = (int)index + length * elementSize;

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        bytes += index;
        delegate*<byte*, TElementType> ftn = (nint)bytes % _alignSize == 0 ? _readFromBuffer : _readFromBufferUnaligned;
        
        if (!BitConverter.IsLittleEndian && _flipBits)
        {
            for (int i = 0; i < length; ++i)
            {
                arr[i] = FlipBits(ftn(bytes + i * elementSize));
            }
        }
        else
        {
            for (int i = 0; i < length; ++i)
            {
                arr[i] = ftn(bytes + i * elementSize);
            }
        }

        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<TElementType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Count;
        int elementSize = _elementSize;
        if (!hasReadLength)
        {
            length = ReadArrayLength(bytes, maxSize, out bytesRead);
            if (length > output.Count || length > 0 && output.Array == null)
            {
                if (maxSize >= bytesRead)
                    bytesRead += length * elementSize;
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        bytes += bytesRead;
        int size = bytesRead + length * elementSize;

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        delegate*<byte*, TElementType> ftn = (nint)bytes % _alignSize == 0 ? _readFromBuffer : _readFromBufferUnaligned;
        TElementType[] arr = output.Array!;
        int ofs = output.Offset;
        
        if (!BitConverter.IsLittleEndian && _flipBits)
        {
            for (int i = 0; i < length; ++i)
            {
                arr[ofs + i] = FlipBits(ftn(bytes + i * elementSize));
            }
        }
        else
        {
            for (int i = 0; i < length; ++i)
            {
                arr[ofs + i] = ftn(bytes + i * elementSize);
            }
        }
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<TElementType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Length;
        int elementSize = _elementSize;
        if (!hasReadLength)
        {
            length = ReadArrayLength(bytes, maxSize, out bytesRead);
            if (length > output.Length)
            {
                if (maxSize >= bytesRead)
                    bytesRead += length * elementSize;
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        bytes += bytesRead;
        int size = bytesRead + length * elementSize;

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        delegate*<byte*, TElementType> ftn = (nint)bytes % _alignSize == 0 ? _readFromBuffer : _readFromBufferUnaligned;

        if (!BitConverter.IsLittleEndian && _flipBits)
        {
            for (int i = 0; i < length; ++i)
            {
                output[i] = FlipBits(ftn(bytes + i * elementSize));
            }
        }
        else
        {
            for (int i = 0; i < length; ++i)
            {
                output[i] = ftn(bytes + i * elementSize);
            }
        }

        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<TElementType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    output.Add(default);
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
                        output.Add(default);
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

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        bytes += bytesRead;
        int elementSize = _elementSize;
        int arrSize = length * elementSize;
        int size = bytesRead + arrSize;

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        TElementType[]? arr = null;
        int arrOffset = 0;
        if (setInsteadOfAdding && output is TElementType[] arr1)
        {
            arr = arr1;
        }
        else if (output is List<TElementType> list)
        {
            if (!setInsteadOfAdding)
                arrOffset = list.Count;
            if (list.Capacity < arrOffset + length)
                list.Capacity = arrOffset + length;

            if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                arr = null;
        }

        delegate*<byte*, TElementType> ftn = (nint)bytes % _alignSize == 0 ? _readFromBuffer : _readFromBufferUnaligned;
        bytesRead = size;
        if (arr != null)
        {
            length += arrOffset;
            for (int i = arrOffset; i < length; ++i)
            {
                arr[i] = ftn(bytes + i * elementSize);
            }

            if (BitConverter.IsLittleEndian || !_flipBits)
                return length - arrOffset;

            for (int i = arrOffset; i < length; ++i)
            {
                arr[i] = FlipBits(arr[i]);
            }
            return length - arrOffset;
        }

        if (setInsteadOfAdding)
        {
            if (BitConverter.IsLittleEndian || !_flipBits)
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = ftn(bytes);
                    bytes += elementSize;
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = FlipBits(ftn(bytes));
                    bytes += elementSize;
                }
            }
        }
        else if (BitConverter.IsLittleEndian || !_flipBits)
        {
            for (int i = 0; i < length; ++i)
            {
                output.Add(ftn(bytes));
                bytes += elementSize;
            }
        }
        else
        {
            for (int i = 0; i < length; ++i)
            {
                output.Add(FlipBits(ftn(bytes)));
                bytes += elementSize;
            }
        }

        return length;
    }


    /// <inheritdoc />
    public override TElementType[]? ReadObject(Stream stream, out int bytesRead)
    {
        if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
            return null;

        if (length == 0)
            return Array.Empty<TElementType>();

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        TElementType[] arr = new TElementType[length];
        int elementSize = _elementSize;
        int arrSize = length * elementSize;
        delegate*<Span<byte>, TElementType> ftn = _readFromBufferSpan;
        if (arrSize <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(arrSize);
            try
            {
                int readCt = stream.Read(buffer, 0, arrSize);
                if (readCt != arrSize)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (arrSize <= _maxBufferSize)
        {
            byte[] buffer = new byte[arrSize];
            int readCt = stream.Read(buffer, 0, arrSize);
            if (readCt != arrSize)
            {
                bytesRead += readCt;
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
            }

            if (!BitConverter.IsLittleEndian && _flipBits)
            {
                for (int i = 0; i < length; ++i)
                {
                    arr[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    arr[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                }
            }
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int readCt = stream.Read(buffer, 0, sizeToCopy);
                if (readCt != sizeToCopy)
                {
                    bytesRead += bytesLeft - arrSize + readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                int stInd = (arrSize - bytesLeft) / elementSize;
                int elementsToCopy = sizeToCopy / elementSize;
                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < elementsToCopy; ++i)
                    {
                        arr[stInd + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < elementsToCopy; ++i)
                    {
                        arr[stInd + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }
        bytesRead += arrSize;
        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] ArraySegment<TElementType> output, out int bytesRead, bool hasReadLength = true)
    {
        int elementSize = _elementSize;
        int length = output.Count;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Count || length > 0 && output.Array == null)
            {
                SerializationHelper.TryAdvanceStream(stream, Configuration, ref bytesRead, length * elementSize);
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        int arrSize = length * elementSize;
        TElementType[] arr = output.Array!;
        int ofs = output.Offset;
        delegate*<Span<byte>, TElementType> ftn = _readFromBufferSpan;
        if (arrSize <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(arrSize);
            try
            {
                int readCt = stream.Read(buffer, 0, arrSize);
                if (readCt != arrSize)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[ofs + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[ofs + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (arrSize <= _maxBufferSize)
        {
            byte[] buffer = new byte[arrSize];
            int readCt = stream.Read(buffer, 0, arrSize);
            if (readCt != arrSize)
            {
                bytesRead += readCt;
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
            }

            if (!BitConverter.IsLittleEndian && _flipBits)
            {
                for (int i = 0; i < length; ++i)
                {
                    arr[ofs + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    arr[ofs + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                }
            }
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int readCt = stream.Read(buffer, 0, sizeToCopy);
                if (readCt != sizeToCopy)
                {
                    bytesRead += bytesLeft - arrSize + readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                int stInd = ofs + (arrSize - bytesLeft) / elementSize;
                int elemToCopy = sizeToCopy / elementSize;
                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < elemToCopy; ++i)
                    {
                        arr[stInd + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < elemToCopy; ++i)
                    {
                        arr[stInd + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] scoped Span<TElementType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Length;
        int elementSize = _elementSize;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Length)
            {
                SerializationHelper.TryAdvanceStream(stream, Configuration, ref bytesRead, length * elementSize);
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        int arrSize = length * elementSize;
        delegate*<Span<byte>, TElementType> ftn = _readFromBufferSpan;
        if (arrSize <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(arrSize);
            try
            {
                int readCt = stream.Read(buffer, 0, arrSize);
                if (readCt != arrSize)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (arrSize <= _maxBufferSize)
        {
            byte[] buffer = new byte[arrSize];
            int readCt = stream.Read(buffer, 0, arrSize);
            if (readCt != arrSize)
            {
                bytesRead += readCt;
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
            }

            if (!BitConverter.IsLittleEndian && _flipBits)
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                }
            }
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int readCt = stream.Read(buffer, 0, sizeToCopy);
                if (readCt != sizeToCopy)
                {
                    bytesRead += bytesLeft - arrSize + readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                int stInd = (arrSize - bytesLeft) / elementSize;
                int elemToCopy = sizeToCopy / elementSize;
                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < elemToCopy; ++i)
                    {
                        output[stInd + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < elemToCopy; ++i)
                    {
                        output[stInd + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] IList<TElementType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    output.Add(default);
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
                        output.Add(default);
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

        Configuration.AssertCanCreateArrayOfType(typeof(TElementType), length, this);

        int elementSize = _elementSize;
        int arrSize = length * elementSize;
        int readCt;
        TElementType[]? arr = null;
        int arrOffset = 0;
        if (setInsteadOfAdding && output is TElementType[] arr1)
        {
            arr = arr1;
        }
        else if (output is List<TElementType> list)
        {
            if (!setInsteadOfAdding)
                arrOffset = list.Count;
            if (list.Capacity < arrOffset + length)
                list.Capacity = arrOffset + length;

            if (!Accessor.TryGetUnderlyingArray(list, out arr) || (list.Count < arrOffset + length && !list.TrySetUnderlyingArray(arr, arrOffset + length)))
                arr = null;
        }

        delegate*<Span<byte>, TElementType> ftn = _readFromBufferSpan;
        if (arr != null)
        {
            if (arrSize <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(arrSize);
                try
                {
                    readCt = stream.Read(buffer, 0, arrSize);
                    if (readCt != arrSize)
                    {
                        bytesRead += readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    if (!BitConverter.IsLittleEndian && _flipBits)
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            arr[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            arr[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                        }
                    }
                }
                finally
                {
                    DefaultSerializer.ArrayPool.Return(buffer);
                }
            }
            else if (arrSize <= _maxBufferSize)
            {
                byte[] buffer = new byte[arrSize];
                readCt = stream.Read(buffer, 0, arrSize);
                if (readCt != arrSize)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        arr[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
            }
            else
            {
                byte[] buffer = new byte[_maxBufferSize];
                int bytesLeft = arrSize;
                do
                {
                    int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                    readCt = stream.Read(buffer, 0, sizeToCopy);
                    if (readCt != sizeToCopy)
                    {
                        bytesRead += bytesLeft - arrSize + readCt;
                        throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                    }

                    int stInd = arrOffset + (arrSize - bytesLeft) / elementSize;
                    int elemToCopy = sizeToCopy / elementSize;
                    if (!BitConverter.IsLittleEndian && _flipBits)
                    {
                        for (int i = 0; i < elemToCopy; ++i)
                        {
                            arr[stInd + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < elemToCopy; ++i)
                        {
                            arr[stInd + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                        }
                    }

                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += arrSize;
            return length;
        }

        if (arrSize <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(arrSize);
            try
            {
                readCt = stream.Read(buffer, 0, arrSize);
                if (readCt != arrSize)
                {
                    bytesRead += readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (setInsteadOfAdding)
                {
                    if (!BitConverter.IsLittleEndian && _flipBits)
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                        }
                    }
                }
                else if (BitConverter.IsLittleEndian || !_flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output.Add(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output.Add(FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize))));
                    }
                }
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (arrSize <= _maxBufferSize)
        {
            byte[] buffer = new byte[arrSize];
            readCt = stream.Read(buffer, 0, arrSize);
            if (readCt != arrSize)
            {
                bytesRead += readCt;
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
            }

            if (setInsteadOfAdding)
            {
                if (!BitConverter.IsLittleEndian && _flipBits)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                    }
                }
            }
            else if (BitConverter.IsLittleEndian || !_flipBits)
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize))));
                }
            }
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int elementsToCopy = sizeToCopy / elementSize;
                readCt = stream.Read(buffer, 0, sizeToCopy);
                if (readCt != sizeToCopy)
                {
                    bytesRead += bytesLeft - arrSize + readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                if (setInsteadOfAdding)
                {
                    int stInd = (arrSize - bytesLeft) / elementSize;
                    if (!BitConverter.IsLittleEndian && _flipBits)
                    {
                        for (int i = 0; i < elementsToCopy; ++i)
                        {
                            output[stInd + i] = FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < elementsToCopy; ++i)
                        {
                            output[stInd + i] = ftn(buffer.AsSpan(i * elementSize, elementSize));
                        }
                    }
                }
                else if (BitConverter.IsLittleEndian || !_flipBits)
                {
                    for (int i = 0; i < elementsToCopy; ++i)
                    {
                        output.Add(ftn(buffer.AsSpan(i * elementSize, elementSize)));
                    }
                }
                else
                {
                    for (int i = 0; i < elementsToCopy; ++i)
                    {
                        output.Add(FlipBits(ftn(buffer.AsSpan(i * elementSize, elementSize))));
                    }
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;

        return length;
    }
}
