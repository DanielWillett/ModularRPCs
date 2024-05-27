using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization;
public unsafe class UnmanagedValueTypeBinaryArrayTypeParser<TValueType> : ArrayBinaryTypeParser<TValueType> where TValueType : unmanaged
{
    private static readonly int MaxBufferSize = DefaultSerializer.MaxBufferSize / sizeof(TValueType) * sizeof(TValueType);
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
    private static void FlipBits(byte[] bytes, int index, int size)
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
    /// <inheritdoc />
    public override int WriteObject(ArraySegment<TValueType> value, byte* bytes, uint maxSize)
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
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

        value.AsSpan().CopyTo(new Span<TValueType>(bytes + hdrSize, length));

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject(ReadOnlySpan<TValueType> value, byte* bytes, uint maxSize)
    {
        uint index = 0;
        int length = value.Length;
        int size = length * sizeof(TValueType);
        int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

        if (length == 0)
            return hdrSize;

        if (maxSize - hdrSize < size)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

        value.CopyTo(new Span<TValueType>(bytes + hdrSize, length));

        if (!BitConverter.IsLittleEndian)
            FlipBits(bytes, hdrSize, size);

        return size + hdrSize;
    }

    /// <inheritdoc />
    public override int WriteObject(IList<TValueType> value, byte* bytes, uint maxSize)
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
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

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
    public override int WriteObject(IReadOnlyList<TValueType> value, byte* bytes, uint maxSize)
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
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

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
    public override int WriteObject(ArraySegment<TValueType> value, Stream stream)
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
        else if (size <= MaxBufferSize)
        {
            byte[] buffer = new byte[size];
            value.AsSpan().CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
            if (!BitConverter.IsLittleEndian)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
    public override int WriteObject(ReadOnlySpan<TValueType> value, Stream stream)
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
        else if (size <= MaxBufferSize)
        {
            byte[] buffer = new byte[size];
            value.CopyTo(MemoryMarshal.Cast<byte, TValueType>(buffer.AsSpan(0, size)));
            if (!BitConverter.IsLittleEndian)
                FlipBits(buffer, 0, size);
            stream.Write(buffer, 0, size);
        }
        else
        {
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = size;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
    public override int WriteObject(IList<TValueType> value, Stream stream)
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
                else if (size <= MaxBufferSize)
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
                    byte[] buffer = new byte[MaxBufferSize];
                    int bytesLeft = size;
                    do
                    {
                        int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
    public override int WriteObject(IReadOnlyList<TValueType> value, Stream stream)
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
                else if (size <= MaxBufferSize)
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
                    byte[] buffer = new byte[MaxBufferSize];
                    int bytesLeft = size;
                    do
                    {
                        int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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

        TValueType[] arr = new TValueType[length];
        int size = (int)index + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes + index, length).CopyTo(arr);

        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, ArraySegment<TValueType> output, out int bytesRead, bool hasReadLength = true)
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

        bytes += bytesRead;
        int size = bytesRead + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes, length).CopyTo(output.AsSpan());
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, Span<TValueType> output, out int bytesRead, bool hasReadLength = true)
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

        bytes += bytesRead;
        int size = bytesRead + length * sizeof(TValueType);

        if (maxSize < size)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

        bytesRead = size;
        new Span<TValueType>(bytes, length).CopyTo(output);
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(byte* bytes, uint maxSize, IList<TValueType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    output.Add(default);
            }
        }
        else
        {
            bytesRead = 0;
            if (setInsteadOfAdding && measuredCount != -1)
            {
                while (measuredCount > output.Count)
                    output.Add(default);

                length = measuredCount;
            }
        }

        if (length <= 0)
            return 0;

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

            bytesRead = size;
            return length;
        }

        bytesRead = size;
        if (setInsteadOfAdding)
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

        return length;
    }


    /// <inheritdoc />
    public override TValueType[]? ReadObject(Stream stream, out int bytesRead)
    {
        if (!SerializationHelper.ReadStandardArrayHeader(stream, out int length, out bytesRead, this))
            return null;

        if (length == 0)
            return Array.Empty<TValueType>();

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
        else if (arrSize <= MaxBufferSize)
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
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
        return arr;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, ArraySegment<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Count;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Count || length > 0 && output.Array == null)
            {
                SerializationHelper.TryAdvanceStream(stream, ref bytesRead, length * sizeof(TValueType));
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

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
        else if (arrSize <= MaxBufferSize)
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
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, Span<TValueType> output, out int bytesRead, bool hasReadLength = true)
    {
        int length = output.Length;
        if (!hasReadLength)
        {
            length = ReadArrayLength(stream, out bytesRead);
            if (length > output.Length)
            {
                SerializationHelper.TryAdvanceStream(stream, ref bytesRead, length * sizeof(TValueType));
                throw new ArgumentOutOfRangeException(nameof(output), string.Format(Properties.Exceptions.OutputListOutOfRangeIBinaryParser, Accessor.ExceptionFormatter.Format(GetType())));
            }
        }
        else bytesRead = 0;

        if (length == 0)
            return 0;

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
        else if (arrSize <= MaxBufferSize)
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
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
        return length;
    }

    /// <inheritdoc />
    public override int ReadObject(Stream stream, IList<TValueType> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    output.Add(default);
            }
        }
        else
        {
            bytesRead = 0;
            if (setInsteadOfAdding && measuredCount != -1)
            {
                while (measuredCount > output.Count)
                    output.Add(default);

                length = measuredCount;
            }
        }

        if (length == 0)
            return 0;

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
            else if (arrSize <= MaxBufferSize)
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
                byte[] buffer = new byte[MaxBufferSize];
                int bytesLeft = arrSize;
                do
                {
                    int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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

                fixed (byte* ptr = buffer)
                {
                    byte* bytes = ptr;
                    if (setInsteadOfAdding)
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
                }
            }
            finally
            {
                DefaultSerializer.ArrayPool.Return(buffer);
            }
        }
        else if (arrSize <= MaxBufferSize)
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
            }
        }
        else
        {
            byte[] buffer = new byte[MaxBufferSize];
            int bytesLeft = arrSize;
            do
            {
                int sizeToCopy = Math.Min(MaxBufferSize, bytesLeft);
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
                }
                bytesLeft -= sizeToCopy;
            } while (bytesLeft > 0);
        }

        bytesRead += arrSize;

        return length;
    }
}
