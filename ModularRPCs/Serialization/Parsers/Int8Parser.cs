using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif

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
        public override int WriteObject(sbyte[]? value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, 0, true, this);
            }

            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

            fixed (sbyte* ptr = value)
            {
                Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(ReadOnlySpan<sbyte> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(bytes + hdrSize), ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[0])), (uint)length);

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IList<sbyte> value, byte* bytes, uint maxSize)
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
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

            switch (value)
            {
                case sbyte[] b:
                    fixed (sbyte* ptr = b)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    fixed (sbyte* ptr = underlying)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = unchecked((byte)value[i]);
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IReadOnlyList<sbyte> value, byte* bytes, uint maxSize)
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
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

            switch (value)
            {
                case sbyte[] b:
                    fixed (sbyte* ptr = b)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                    fixed (sbyte* ptr = underlying)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = unchecked((byte)value[i]);
                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(sbyte[]? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

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
                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[0])), (uint)length);
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
                Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[0])), (uint)length);
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[length - bytesLeft])), (uint)sizeToCopy);
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
#endif

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(ReadOnlySpan<sbyte> value, Stream stream)
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
                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[0])), (uint)length);
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
                Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[0])), (uint)length);
                stream.Write(buffer, 0, length);
            }
            else
            {
                byte[] buffer = new byte[DefaultSerializer.MaxBufferSize];
                int bytesLeft = length;
                do
                {
                    int sizeToCopy = Math.Min(DefaultSerializer.MaxBufferSize, bytesLeft);
                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref Unsafe.AsRef(in value[length - bytesLeft])), (uint)sizeToCopy);
                    stream.Write(buffer, 0, sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }
#endif
            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IList<sbyte> value, Stream stream)
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
        public override int WriteObject(IReadOnlyList<sbyte> value, Stream stream)
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
                                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref b[0]), (uint)length);
                                    stream.Write(buffer, 0, length);
                                    break;

                                case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                    Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref underlying[0]), (uint)length);
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
                                Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref b[0]), (uint)length);
                                stream.Write(buffer, 0, length);
                                break;

                            case List<sbyte> l when Accessor.TryGetUnderlyingArray(l, out sbyte[] underlying):
                                Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.As<sbyte, byte>(ref underlying[0]), (uint)length);
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
            fixed (sbyte* ptr = arr)
            {
                Buffer.MemoryCopy(bytes + index, ptr, maxSize - index, length);
            }

            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, Span<sbyte> output, out int bytesRead, bool hasReadLength = true)
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
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref output[0]), ref Unsafe.AsRef<byte>(bytes), (uint)length);
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, IList<sbyte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(0);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    while (measuredCount > output.Count)
                        output.Add(0);

                    length = measuredCount;
                }
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
                fixed (void* ptr = &arr[arrOffset])
                {
                    Buffer.MemoryCopy(bytes, ptr, maxSize - bytesRead, length);
                }

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
        public override int ReadObject(Stream stream, Span<sbyte> output, out int bytesRead, bool hasReadLength = true)
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
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref output[0]), ref buffer[0], (uint)length);
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
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref output[0]), ref buffer[0], (uint)length);
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

                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref output[length - bytesLeft]), ref buffer[0], (uint)sizeToCopy);
                    bytesLeft -= sizeToCopy;
                } while (bytesLeft > 0);
            }

            bytesRead += length;
#endif
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, IList<sbyte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                        output.Add(0);
                }
            }
            else
            {
                bytesRead = 0;
                if (setInsteadOfAdding && measuredCount != -1)
                {
                    while (measuredCount > output.Count)
                        output.Add(0);

                    length = measuredCount;
                }
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