using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ReflectionTools;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Subclass of <see cref="ArrayBinaryTypeParser{T}"/> to take some boilerplate away from writing array parsers for primitive unmanaged types that should be flipped on big-endian machines.
/// Supports arrays, <see cref="IList{T}"/>,
/// <see cref="IReadOnlyList{T}"/>, <see cref="IEnumerable{T}"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>,
/// <see cref="ArraySegment{T}"/>, <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> pointers (with <see cref="TypedReference"/>'s), 
/// and <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>
/// </summary>
/// <typeparam name="TValueType">The element type to parse.</typeparam>
public unsafe class UnmanagedValueTypeBinaryArrayTypeParser<TValueType> : ArrayBinaryTypeParser<TValueType> where TValueType : unmanaged
{
    protected readonly SerializationConfiguration Configuration;
    private readonly int _maxBufferSize;
    public UnmanagedValueTypeBinaryArrayTypeParser(SerializationConfiguration config)
    {
        Configuration = config;
        Configuration.Lock();
        _maxBufferSize = Configuration.MaximumBufferSize / sizeof(TValueType) * sizeof(TValueType);
    }

    private static void FlipBits(byte* bytes, int hdrSize, int size)
    {
        bytes += hdrSize;
        byte* end = bytes + size;
        while (bytes < end)
        {
            for (int i = 0; i < sizeof(TValueType) / 2; ++i)
            {
                byte b = bytes[i];
                int ind = sizeof(TValueType) - i - 1;
                bytes[i] = bytes[ind];
                bytes[ind] = b;
            }

            bytes += sizeof(TValueType);
        }
    }
    private static void FlipBits([InstantHandle] byte[] bytes, int index, int size)
    {
        for (; index < size; index += sizeof(TValueType))
        {
            for (int i = 0; i < sizeof(TValueType) / 2; ++i)
            {
                byte b = bytes[i];
                int ind = sizeof(TValueType) - i - 1;
                bytes[i] = bytes[ind];
                bytes[ind] = b;
            }
        }
    }
    private static TValueType FlipBits(TValueType toFlip)
    {
        byte* ptr = (byte*)&toFlip;
        for (int i = 0; i < sizeof(TValueType) / 2; ++i)
        {
            byte b = ptr[i];
            int ind = sizeof(TValueType) - i - 1;
            ptr[i] = ptr[ind];
            ptr[ind] = b;
        }

        return toFlip;
    }
    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ArraySegment<TValueType> value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value.Array == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        value.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] scoped ReadOnlySpan<TValueType> value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        int length = value.Length;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        value.CopyTo(new Span<TValueType>(bytes + hdrSize, length));

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IList<TValueType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        switch (value)
        {
            case TValueType[] b:
                b.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));
                break;

            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                underlying.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));
                break;

            default:
                bytes += hdrSize;
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                    }
                }
                bytes -= hdrSize;
                break;
        }

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyList<TValueType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        switch (value)
        {
            case TValueType[] b:
                b.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));
                break;

            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                underlying.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));
                break;

            default:
                bytes += hdrSize;
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                    }
                }
                bytes -= hdrSize;
                break;
        }

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ICollection<TValueType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytes += hdrSize;
        using IEnumerator<TValueType> enumerator = value.GetEnumerator();
        int i = 0;
        if ((nint)bytes % sizeof(TValueType) == 0)
        {
            while (i < length && enumerator.MoveNext())
            {
                *(TValueType*)(bytes + i++ * sizeof(TValueType)) = enumerator.Current;
            }
        }
        else
        {
            while (i < length && enumerator.MoveNext())
            {
                Unsafe.WriteUnaligned(bytes + i++ * sizeof(TValueType), enumerator.Current);
            }
        }

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        if (i == length)
            return i * sizeof(TValueType) + hdrSize;

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

        return i * sizeof(TValueType) + newHdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyCollection<TValueType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytes += hdrSize;
        using IEnumerator<TValueType> enumerator = value.GetEnumerator();
        int i = 0;
        if ((nint)bytes % sizeof(TValueType) == 0)
        {
            while (i < length && enumerator.MoveNext())
            {
                *(TValueType*)(bytes + i++ * sizeof(TValueType)) = enumerator.Current;
            }
        }
        else
        {
            while (i < length && enumerator.MoveNext())
            {
                Unsafe.WriteUnaligned(bytes + i++ * sizeof(TValueType), enumerator.Current);
            }
        }

        bytes -= hdrSize;

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        if (i == length)
            return i * sizeof(TValueType) + hdrSize;

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

        return i * sizeof(TValueType) + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IEnumerable<TValueType>? value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, bytes, maxSize);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, bytes, maxSize);
        }
        if (value is ICollection<TValueType> collection)
        {
            return WriteObject(collection, bytes, maxSize);
        }
        if (value is IReadOnlyCollection<TValueType> collection2)
        {
            return WriteObject(collection2, bytes, maxSize);
        }

        int actualCount = 0;
        using (IEnumerator<TValueType> enumerator = value.GetEnumerator())
        {
            if ((nint)bytes % sizeof(TValueType) == 0)
            {
                while (enumerator.MoveNext())
                {
                    if (maxSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    *(TValueType*)(bytes + actualCount) = enumerator.Current;
                    actualCount += sizeof(TValueType);
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    if (maxSize < actualCount + 1)
                        throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

                    Unsafe.WriteUnaligned(bytes + actualCount, enumerator.Current);
                    actualCount += sizeof(TValueType);
                }
            }
        }

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, 0, actualCount);

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
        SerializationHelper.WriteStandardArrayHeader(bytes, (uint)newHdrSize, ref index, actualCount / sizeof(TValueType), false, this);
        return actualCount + newHdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ArraySegment<TValueType> value, Stream stream)
    {
        if (value.Array == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        if (BitConverter.IsLittleEndian)
        {
            stream.Write(MemoryMarshal.Cast<TValueType, byte>(value));
        }
        else
#endif
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                value.AsSpan().CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
                if (!BitConverter.IsLittleEndian)
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
            value.AsSpan().CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
            if (!BitConverter.IsLittleEndian)
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
                value.AsSpan((size - bytesLeft) / sizeof(TValueType), sizeToCopy / sizeof(TValueType)).CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan()));
                if (!BitConverter.IsLittleEndian)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] scoped ReadOnlySpan<TValueType> value, Stream stream)
    {
        int length = value.Length;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        if (BitConverter.IsLittleEndian)
        {
            stream.Write(MemoryMarshal.Cast<TValueType, byte>(value));
        }
        else
#endif
        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {
                value.CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
                if (!BitConverter.IsLittleEndian)
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
            value.CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
            if (!BitConverter.IsLittleEndian)
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
                value.Slice((size - bytesLeft) / sizeof(TValueType), sizeToCopy / sizeof(TValueType)).CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan()));
                if (!BitConverter.IsLittleEndian)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IList<TValueType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        switch (value)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            case TValueType[] b when BitConverter.IsLittleEndian:
                stream.Write(MemoryMarshal.Cast<TValueType, byte>(b));
                break;

            case List<TValueType> l when BitConverter.IsLittleEndian && Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                stream.Write(MemoryMarshal.Cast<TValueType, byte>(underlying.AsSpan(0, length)));
                break;
#endif
            default:
                if (size <= DefaultSerializer.MaxArrayPoolSize)
                {
                    byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
                    try
                    {
                        switch (value)
                        {
                            case TValueType[] b:
                                Buffer.BlockCopy(b, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;

                            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                                Buffer.BlockCopy(underlying, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;

                            default:
                                fixed (byte* bytes = buffer)
                                {
                                    if ((nint)bytes % sizeof(TValueType) == 0)
                                    {
                                        for (int i = 0; i < length; ++i)
                                        {
                                            *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < length; ++i)
                                        {
                                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                                        }
                                    }
                                }
                                if (!BitConverter.IsLittleEndian)
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;
                        }
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(buffer);
                    }
                }
                else if (size <= _maxBufferSize)
                {
                    byte[] buffer = new byte[size];
                    switch (value)
                    {
                        case TValueType[] b:
                            Buffer.BlockCopy(b, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            if (!BitConverter.IsLittleEndian)
#endif
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;

                        case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                            Buffer.BlockCopy(underlying, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            if (!BitConverter.IsLittleEndian)
#endif
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;

                        default:
                            fixed (byte* bytes = buffer)
                            {
                                if ((nint)bytes % sizeof(TValueType) == 0)
                                {
                                    for (int i = 0; i < length; ++i)
                                    {
                                        *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < length; ++i)
                                    {
                                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                                    }
                                }
                            }
                            if (!BitConverter.IsLittleEndian)
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;
                    }
                }
                else
                {
                    byte[] buffer = new byte[_maxBufferSize];
                    int bytesLeft = size;
                    do
                    {
                        int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                        int stInd = (size - bytesLeft) / sizeof(TValueType);
                        switch (value)
                        {
                            case TValueType[] b:
                                Buffer.BlockCopy(b, stInd * sizeof(TValueType), buffer, 0, sizeToCopy);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;

                            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                                Buffer.BlockCopy(underlying, stInd * sizeof(TValueType), buffer, 0, sizeToCopy);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;

                            default:
                                int elemToCopy = sizeToCopy / sizeof(TValueType);
                                fixed (byte* bytes = buffer)
                                {
                                    if ((nint)bytes % sizeof(TValueType) == 0)
                                    {
                                        for (int i = 0; i < elemToCopy; ++i)
                                        {
                                            *(TValueType*)(bytes + i * sizeof(TValueType)) = value[stInd + i];
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < elemToCopy; ++i)
                                        {
                                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[stInd + i]);
                                        }
                                    }
                                }
                                if (!BitConverter.IsLittleEndian)
                                    FlipBits(buffer, 0, sizeToCopy);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;
                        }
                        bytesLeft -= sizeToCopy;
                    } while (bytesLeft > 0);
                }

                break;
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyList<TValueType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        switch (value)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            case TValueType[] b when BitConverter.IsLittleEndian:
                stream.Write(MemoryMarshal.Cast<TValueType, byte>(b));
                break;

            case List<TValueType> l when BitConverter.IsLittleEndian && Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                stream.Write(MemoryMarshal.Cast<TValueType, byte>(underlying.AsSpan(0, length)));
                break;
#endif
            default:
                if (size <= DefaultSerializer.MaxArrayPoolSize)
                {
                    byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
                    try
                    {
                        switch (value)
                        {
                            case TValueType[] b:
                                Buffer.BlockCopy(b, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;

                            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                                Buffer.BlockCopy(underlying, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;

                            default:
                                fixed (byte* bytes = buffer)
                                {
                                    if ((nint)bytes % sizeof(TValueType) == 0)
                                    {
                                        for (int i = 0; i < length; ++i)
                                        {
                                            *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < length; ++i)
                                        {
                                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                                        }
                                    }
                                }
                                if (!BitConverter.IsLittleEndian)
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, size);
                                break;
                        }
                    }
                    finally
                    {
                        DefaultSerializer.ArrayPool.Return(buffer);
                    }
                }
                else if (size <= _maxBufferSize)
                {
                    byte[] buffer = new byte[size];
                    switch (value)
                    {
                        case TValueType[] b:
                            Buffer.BlockCopy(b, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            if (!BitConverter.IsLittleEndian)
#endif
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;

                        case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                            Buffer.BlockCopy(underlying, 0, buffer, 0, size);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                            if (!BitConverter.IsLittleEndian)
#endif
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;

                        default:
                            fixed (byte* bytes = buffer)
                            {
                                if ((nint)bytes % sizeof(TValueType) == 0)
                                {
                                    for (int i = 0; i < length; ++i)
                                    {
                                        *(TValueType*)(bytes + i * sizeof(TValueType)) = value[i];
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < length; ++i)
                                    {
                                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[i]);
                                    }
                                }
                            }
                            if (!BitConverter.IsLittleEndian)
                                FlipBits(buffer, 0, size);
                            stream.Write(buffer, 0, size);
                            break;
                    }
                }
                else
                {
                    byte[] buffer = new byte[_maxBufferSize];
                    int bytesLeft = size;
                    do
                    {
                        int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                        int stInd = (size - bytesLeft) / sizeof(TValueType);
                        switch (value)
                        {
                            case TValueType[] b:
                                Buffer.BlockCopy(b, stInd * sizeof(TValueType), buffer, 0, sizeToCopy);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;

                            case List<TValueType> l when Accessor.TryGetUnderlyingArray(l, out TValueType[] underlying):
                                Buffer.BlockCopy(underlying, stInd * sizeof(TValueType), buffer, 0, sizeToCopy);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
                                if (!BitConverter.IsLittleEndian)
#endif
                                    FlipBits(buffer, 0, size);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;

                            default:
                                int elemToCopy = sizeToCopy / sizeof(TValueType);
                                fixed (byte* bytes = buffer)
                                {
                                    if ((nint)bytes % sizeof(TValueType) == 0)
                                    {
                                        for (int i = 0; i < elemToCopy; ++i)
                                        {
                                            *(TValueType*)(bytes + i * sizeof(TValueType)) = value[stInd + i];
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < elemToCopy; ++i)
                                        {
                                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), value[stInd + i]);
                                        }
                                    }
                                }
                                if (!BitConverter.IsLittleEndian)
                                    FlipBits(buffer, 0, sizeToCopy);
                                stream.Write(buffer, 0, sizeToCopy);
                                break;
                        }
                        bytesLeft -= sizeToCopy;
                    } while (bytesLeft > 0);
                }

                break;
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] ICollection<TValueType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, stream);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {

                fixed (byte* bytes = buffer)
                {
                    using IEnumerator<TValueType> enumerator = value.GetEnumerator();
                    int i = 0;
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                            ++i;
                        }
                    }
                    else
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                            ++i;
                        }
                    }
                }
                if (!BitConverter.IsLittleEndian)
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
            fixed (byte* bytes = buffer)
            {
                using IEnumerator<TValueType> enumerator = value.GetEnumerator();
                int i = 0;
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    while (i < length && enumerator.MoveNext())
                    {
                        *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                        ++i;
                    }
                }
                else
                {
                    while (i < length && enumerator.MoveNext())
                    {
                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                        ++i;
                    }
                }
            }
            if (!BitConverter.IsLittleEndian)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            using IEnumerator<TValueType> enumerator = value.GetEnumerator();
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int elemToCopy = sizeToCopy / sizeof(TValueType);
                fixed (byte* bytes = buffer)
                {
                    int i = 0;
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        while (i < elemToCopy && enumerator.MoveNext())
                        {
                            *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                            ++i;
                        }
                    }
                    else
                    {
                        while (i < elemToCopy && enumerator.MoveNext())
                        {
                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                            ++i;
                        }
                    }
                }
                if (!BitConverter.IsLittleEndian)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IReadOnlyCollection<TValueType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, stream);
        }

        int length = value.Count;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

        if (length == 0)
            return hdrSize;

        if (size <= DefaultSerializer.MaxArrayPoolSize)
        {
            byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
            try
            {

                fixed (byte* bytes = buffer)
                {
                    using IEnumerator<TValueType> enumerator = value.GetEnumerator();
                    int i = 0;
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                            ++i;
                        }
                    }
                    else
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                            ++i;
                        }
                    }
                }
                if (!BitConverter.IsLittleEndian)
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
            fixed (byte* bytes = buffer)
            {
                using IEnumerator<TValueType> enumerator = value.GetEnumerator();
                int i = 0;
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    while (i < length && enumerator.MoveNext())
                    {
                        *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                        ++i;
                    }
                }
                else
                {
                    while (i < length && enumerator.MoveNext())
                    {
                        Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                        ++i;
                    }
                }
            }
            if (!BitConverter.IsLittleEndian)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[_maxBufferSize];
            using IEnumerator<TValueType> enumerator = value.GetEnumerator();
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(buffer.Length, bytesLeft);
                int elemToCopy = sizeToCopy / sizeof(TValueType);
                fixed (byte* bytes = buffer)
                {
                    int i = 0;
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        while (i < elemToCopy && enumerator.MoveNext())
                        {
                            *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                            ++i;
                        }
                    }
                    else
                    {
                        while (i < elemToCopy && enumerator.MoveNext())
                        {
                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                            ++i;
                        }
                    }
                }
                if (!BitConverter.IsLittleEndian)
                    FlipBits(buffer, 0, sizeToCopy);
                stream.Write(buffer, 0, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject([InstantHandle] IEnumerable<TValueType>? value, Stream stream)
    {
        if (value == null)
        {
            return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
        }
        if (value is IList<TValueType> list)
        {
            return WriteObject(list, stream);
        }
        if (value is IReadOnlyList<TValueType> list2)
        {
            return WriteObject(list2, stream);
        }

        IEnumerator<TValueType> enumerator = value.GetEnumerator();
        try
        {
            int length = 0;
            while (enumerator.MoveNext())
                checked { ++length; }

            int size = length * sizeof(TValueType);
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);
            if (length == 0)
                return hdrSize;

            ResetOrReMake(ref enumerator, value);

            if (size <= DefaultSerializer.MaxArrayPoolSize)
            {
                byte[] buffer = DefaultSerializer.ArrayPool.Rent(size);
                try
                {
                    fixed (byte* bytes = buffer)
                    {
                        int i = 0;
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            while (i < length && enumerator.MoveNext())
                            {
                                *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                                ++i;
                            }
                        }
                        else
                        {
                            while (i < length && enumerator.MoveNext())
                            {
                                Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                                ++i;
                            }
                        }
                    }
                    if (!BitConverter.IsLittleEndian)
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
                fixed (byte* bytes = buffer)
                {
                    int i = 0;
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                            ++i;
                        }
                    }
                    else
                    {
                        while (i < length && enumerator.MoveNext())
                        {
                            Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                            ++i;
                        }
                    }
                }
                if (!BitConverter.IsLittleEndian)
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
                    int elemToCopy = sizeToCopy / sizeof(TValueType);
                    fixed (byte* bytes = buffer)
                    {
                        int i = 0;
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            while (i < elemToCopy && enumerator.MoveNext())
                            {
                                *(TValueType*)(bytes + i * sizeof(TValueType)) = enumerator.Current;
                                ++i;
                            }
                        }
                        else
                        {
                            while (i < elemToCopy && enumerator.MoveNext())
                            {
                                Unsafe.WriteUnaligned(bytes + i * sizeof(TValueType), enumerator.Current);
                                ++i;
                            }
                        }
                    }
                    if (!BitConverter.IsLittleEndian)
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
    public override TValueType[]? ReadObject(byte* bytes, uint maxSize, out int bytesRead)
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
            return Array.Empty<TValueType>();
        }

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        TValueType[] arr = new TValueType[length];
        int size = (int)index + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes + index, length).CopyTo(arr);
        if (BitConverter.IsLittleEndian)
            return arr;

        for (int i = 0; i < length; ++i)
        {
            arr[i] = FlipBits(arr[i]);
        }
        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] ArraySegment<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Count;
        if (!hasReadLength)
        {
            length = ReadArrayLength(bytes, maxSize, out bytesRead);
            if (length > output.Count || length > 0 && output.Array == null)
            {
                if (maxSize >= bytesRead)
                    bytesRead += length * sizeof(TValueType);
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        bytes += bytesRead;
        int size = bytesRead + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes, length).CopyTo(output.AsSpan());
        if (BitConverter.IsLittleEndian)
            return length;

        TValueType[] arr = output.Array!;
        int ofs = output.Offset;
        length += ofs;
        for (int i = ofs; i < length; ++i)
        {
            arr[i] = FlipBits(arr[i]);
        }
        return length - ofs;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] scoped Span<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Length;
        if (!hasReadLength)
        {
            length = ReadArrayLength(bytes, maxSize, out bytesRead);
            if (length > output.Length)
            {
                if (maxSize >= bytesRead)
                    bytesRead += length * sizeof(TValueType);
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        bytes += bytesRead;
        int size = bytesRead + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes, length).CopyTo(output);
        if (BitConverter.IsLittleEndian)
            return length;

        for (int i = 0; i < length; ++i)
        {
            output[i] = FlipBits(output[i]);
        }

        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, [InstantHandle] IList<TValueType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        bytes += bytesRead;
        int arrSize = length * sizeof(TValueType);
        int size = bytesRead + arrSize;

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        TValueType[]? arr = null;
        int arrOffset = 0;
        if (setInsteadOfAdding && output is TValueType[] arr1)
        {
            arr = arr1;
        }
        else if (output is List<TValueType> list)
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
            new Span<TValueType>(bytes, length).CopyTo(arr.AsSpan(arrOffset, length));
            if (!BitConverter.IsLittleEndian)
            {
                length += arrOffset;
                for (int i = arrOffset; i < length; ++i)
                {
                    output[i] = FlipBits(output[i]);
                }

                length -= arrOffset;
            }
            bytesRead = size;
            return length;
        }

        bytesRead = size;
        if (setInsteadOfAdding)
        {
            if (BitConverter.IsLittleEndian)
            {
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = *(TValueType*)bytes;
                        bytes += sizeof(TValueType);
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = Unsafe.ReadUnaligned<TValueType>(bytes);
                        bytes += sizeof(TValueType);
                    }
                }
            }
            else
            {
                if ((nint)bytes % sizeof(TValueType) == 0)
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = FlipBits(*(TValueType*)bytes);
                        bytes += sizeof(TValueType);
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                    {
                        output[i] = FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes));
                        bytes += sizeof(TValueType);
                    }
                }
            }
        }
        else if (BitConverter.IsLittleEndian)
        {
            if ((nint)bytes % sizeof(TValueType) == 0)
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(*(TValueType*)bytes);
                    bytes += sizeof(TValueType);
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(Unsafe.ReadUnaligned<TValueType>(bytes));
                    bytes += sizeof(TValueType);
                }
            }
        }
        else
        {
            if ((nint)bytes % sizeof(TValueType) == 0)
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(FlipBits(*(TValueType*)bytes));
                    bytes += sizeof(TValueType);
                }
            }
            else
            {
                for (int i = 0; i < length; ++i)
                {
                    output.Add(FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes)));
                    bytes += sizeof(TValueType);
                }
            }
        }

        return length;
    }


    /// <inheritdoc />
    public override TValueType[]? ReadObject(Stream stream, out int bytesRead)
    {
        if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
            return null;

        if (length == 0)
            return Array.Empty<TValueType>();

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        TValueType[] arr = new TValueType[length];
        int arrSize = length * sizeof(TValueType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        int rdCt = stream.Read(MemoryMarshal.Cast<TValueType, byte>(arr));
        bytesRead += rdCt;
        if (rdCt != arrSize)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
#else
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

                Buffer.BlockCopy(buffer, 0, arr, 0, arrSize);
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

            Buffer.BlockCopy(buffer, 0, arr, 0, arrSize);
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

                Buffer.BlockCopy(buffer, 0, arr, arrSize - bytesLeft, sizeToCopy);
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }
        bytesRead += arrSize;
#endif
        if (BitConverter.IsLittleEndian)
            return arr;

        for (int i = 0; i < length; ++i)
        {
            arr[i] = FlipBits(arr[i]);
        }

        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] ArraySegment<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Count;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Count || length > 0 && output.Array == null)
            {
                SerializationHelper.TryAdvanceStream(stream, Configuration, ref bytesRead, length * sizeof(TValueType));
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        int arrSize = length * sizeof(TValueType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        int rdCt = stream.Read(MemoryMarshal.Cast<TValueType, byte>(output.AsSpan(0, length)));
        bytesRead += rdCt;
        if (rdCt != arrSize)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

#else
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

                buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.AsSpan(0, length)));
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

            buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.AsSpan(0, length)));
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

                buffer.AsSpan(0, sizeToCopy).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.AsSpan(arrSize - bytesLeft, sizeToCopy / sizeof(TValueType))));
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;
#endif
        if (BitConverter.IsLittleEndian)
            return length;

        TValueType[] arr = output.Array!;
        int ofs = output.Offset;
        length += ofs;
        for (int i = ofs; i < length; ++i)
        {
            arr[i] = FlipBits(arr[i]);
        }
        return length - ofs;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] scoped Span<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Length;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Length)
            {
                SerializationHelper.TryAdvanceStream(stream, Configuration, ref bytesRead, length * sizeof(TValueType));
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        int arrSize = length * sizeof(TValueType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        int rdCt = stream.Read(MemoryMarshal.Cast<TValueType, byte>(output[..length]));
        bytesRead += rdCt;
        if (rdCt != arrSize)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

#else
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

                buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.Slice(0, length)));
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

            buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.Slice(0, length)));
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

                buffer.AsSpan(0, sizeToCopy).CopyTo(MemoryMarshal.Cast<TValueType, byte>(output.Slice((arrSize - bytesLeft) / sizeof(TValueType), sizeToCopy / sizeof(TValueType))));
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;
#endif
        if (BitConverter.IsLittleEndian)
            return length;

        for (int i = 0; i < length; ++i)
        {
            output[i] = FlipBits(output[i]);
        }
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, [InstantHandle] IList<TValueType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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

        Configuration.AssertCanCreateArrayOfType(typeof(TValueType), length, this);

        int arrSize = length * sizeof(TValueType);
        int readCt;
        TValueType[]? arr = null;
        int arrOffset = 0;
        if (setInsteadOfAdding && output is TValueType[] arr1)
        {
            arr = arr1;
        }
        else if (output is List<TValueType> list)
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
            readCt = stream.Read(MemoryMarshal.Cast<TValueType, byte>(arr.AsSpan(arrOffset, length)));
            bytesRead += readCt;
            if (readCt != arrSize)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
#else
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

                    buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(arr.AsSpan(arrOffset, length)));
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

                buffer.AsSpan(0, arrSize).CopyTo(MemoryMarshal.Cast<TValueType, byte>(arr.AsSpan(arrOffset, length)));
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

                    buffer.AsSpan(0, sizeToCopy).CopyTo(MemoryMarshal.Cast<TValueType, byte>(arr.AsSpan((arrOffset + (arrSize - bytesLeft)) / sizeof(TValueType), sizeToCopy / sizeof(TValueType))));
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += arrSize;
#endif
            if (BitConverter.IsLittleEndian)
                return length;

            length += arrOffset;
            for (int i = arrOffset; i < length; ++i)
            {
                output[i] = FlipBits(output[i]);
            }
            return length - arrOffset;
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

                fixed (byte* ptr = buffer)
                {
                    byte* bytes = ptr;
                    if (setInsteadOfAdding)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            if ((nint)bytes % sizeof(TValueType) == 0)
                            {
                                for (int i = 0; i < length; ++i)
                                {
                                    output[i] = *(TValueType*)bytes;
                                    bytes += sizeof(TValueType);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < length; ++i)
                                {
                                    output[i] = Unsafe.ReadUnaligned<TValueType>(bytes);
                                    bytes += sizeof(TValueType);
                                }
                            }
                        }
                        else
                        {
                            if ((nint)bytes % sizeof(TValueType) == 0)
                            {
                                for (int i = 0; i < length; ++i)
                                {
                                    output[i] = FlipBits(*(TValueType*)bytes);
                                    bytes += sizeof(TValueType);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < length; ++i)
                                {
                                    output[i] = FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes));
                                    bytes += sizeof(TValueType);
                                }
                            }
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output.Add(*(TValueType*)bytes);
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output.Add(Unsafe.ReadUnaligned<TValueType>(bytes));
                                bytes += sizeof(TValueType);
                            }
                        }
                    }
                    else
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output.Add(FlipBits(*(TValueType*)bytes));
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output.Add(FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes)));
                                bytes += sizeof(TValueType);
                            }
                        }
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

            fixed (byte* ptr = buffer)
            {
                byte* bytes = ptr;
                if (setInsteadOfAdding)
                {
                    if (BitConverter.IsLittleEndian)
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output[i] = *(TValueType*)bytes;
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output[i] = Unsafe.ReadUnaligned<TValueType>(bytes);
                                bytes += sizeof(TValueType);
                            }
                        }
                    }
                    else
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output[i] = FlipBits(*(TValueType*)bytes);
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < length; ++i)
                            {
                                output[i] = FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes));
                                bytes += sizeof(TValueType);
                            }
                        }
                    }
                }
                else if (BitConverter.IsLittleEndian)
                {
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output.Add(*(TValueType*)bytes);
                            bytes += sizeof(TValueType);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output.Add(Unsafe.ReadUnaligned<TValueType>(bytes));
                            bytes += sizeof(TValueType);
                        }
                    }
                }
                else
                {
                    if ((nint)bytes % sizeof(TValueType) == 0)
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output.Add(FlipBits(*(TValueType*)bytes));
                            bytes += sizeof(TValueType);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < length; ++i)
                        {
                            output.Add(FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes)));
                            bytes += sizeof(TValueType);
                        }
                    }
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
                int elementsToCopy = sizeToCopy / sizeof(TValueType);
                readCt = stream.Read(buffer, 0, sizeToCopy);
                if (readCt != sizeToCopy)
                {
                    bytesRead += bytesLeft - arrSize + readCt;
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                }

                fixed (byte* ptr = buffer)
                {
                    byte* bytes = ptr;
                    if (setInsteadOfAdding)
                    {
                        int stInd = (arrSize - bytesLeft) / sizeof(TValueType);

                        if (BitConverter.IsLittleEndian)
                        {
                            if ((nint)bytes % sizeof(TValueType) == 0)
                            {
                                for (int i = 0; i < elementsToCopy; ++i)
                                {
                                    output[stInd + i] = *(TValueType*)bytes;
                                    bytes += sizeof(TValueType);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < elementsToCopy; ++i)
                                {
                                    output[stInd + i] = Unsafe.ReadUnaligned<TValueType>(bytes);
                                    bytes += sizeof(TValueType);
                                }
                            }
                        }
                        else
                        {
                            if ((nint)bytes % sizeof(TValueType) == 0)
                            {
                                for (int i = 0; i < elementsToCopy; ++i)
                                {
                                    output[stInd + i] = FlipBits(*(TValueType*)bytes);
                                    bytes += sizeof(TValueType);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < elementsToCopy; ++i)
                                {
                                    output[stInd + i] = FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes));
                                    bytes += sizeof(TValueType);
                                }
                            }
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < elementsToCopy; ++i)
                            {
                                output.Add(*(TValueType*)bytes);
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < elementsToCopy; ++i)
                            {
                                output.Add(Unsafe.ReadUnaligned<TValueType>(bytes));
                                bytes += sizeof(TValueType);
                            }
                        }
                    }
                    else
                    {
                        if ((nint)bytes % sizeof(TValueType) == 0)
                        {
                            for (int i = 0; i < elementsToCopy; ++i)
                            {
                                output.Add(FlipBits(*(TValueType*)bytes));
                                bytes += sizeof(TValueType);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < elementsToCopy; ++i)
                            {
                                output.Add(FlipBits(Unsafe.ReadUnaligned<TValueType>(bytes)));
                                bytes += sizeof(TValueType);
                            }
                        }
                    }
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;

        return length;
    }
}
