using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
        /// <inheritdoc />
        public override int WriteObject(byte[]? value, byte* bytes, uint maxSize)
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

            fixed (byte* ptr = value)
            {
                Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(ReadOnlySpan<byte> value, byte* bytes, uint maxSize)
        {
            uint index = 0;
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(bytes, maxSize, ref index, length, false, this);

            if (length == 0)
                return hdrSize;

            if (maxSize - hdrSize < length)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType())));

            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(bytes), ref Unsafe.AsRef(in value[0]), (uint)length);

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IList<byte> value, byte* bytes, uint maxSize)
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
                case byte[] b:
                    fixed (byte* ptr = b)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    fixed (byte* ptr = underlying)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = value[i];
                    break;
            }

            return hdrSize + length;
        }

        /// <inheritdoc />
        public override int WriteObject(IReadOnlyList<byte> value, byte* bytes, uint maxSize)
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
                case byte[] b:
                    fixed (byte* ptr = b)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                case List<byte> l when Accessor.TryGetUnderlyingArray(l, out byte[] underlying):
                    fixed (byte* ptr = underlying)
                    {
                        Buffer.MemoryCopy(ptr, bytes + hdrSize, maxSize - hdrSize, length);
                    }
                    break;

                default:
                    for (int i = 0; i < value.Count; ++i)
                        bytes[hdrSize + i] = value[i];
                    break;
            }

            return hdrSize + length;
        }

        /// <inheritdoc />
        public override int WriteObject(byte[]? value, Stream stream)
        {
            if (value == null)
            {
                return SerializationHelper.WriteStandardArrayHeader(stream, 0, true);
            }

            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

            stream.Write(value, 0, length);
            return length;
        }

        /// <inheritdoc />
        public override int WriteObject(ReadOnlySpan<byte> value, Stream stream)
        {
            int length = value.Length;
            int hdrSize = SerializationHelper.WriteStandardArrayHeader(stream, length, false);

            if (length == 0)
                return hdrSize;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            stream.Write(value);
#else
            // todo use temp buffer instead of full buffer
            byte[] buffer = length <= DefaultSerializer.MaxArrayPoolSize ? DefaultSerializer.ArrayPool.Rent(length) : new byte[length];
            try
            {
                Unsafe.CopyBlockUnaligned(ref buffer[0], ref Unsafe.AsRef(in value[0]), (uint)length);
                stream.Write(buffer, 0, length);
            }
            finally
            {
                if (length <= DefaultSerializer.MaxArrayPoolSize)
                    DefaultSerializer.ArrayPool.Return(buffer);
            }
#endif
            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IList<byte> value, Stream stream)
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
                    // todo use temp buffer instead of full buffer
                    byte[] buffer = length <= DefaultSerializer.MaxArrayPoolSize ? DefaultSerializer.ArrayPool.Rent(length) : new byte[length];
                    try
                    {
                        value.CopyTo(buffer, 0);
                        stream.Write(buffer, 0, length);
                    }
                    finally
                    {
                        if (length <= DefaultSerializer.MaxArrayPoolSize)
                            DefaultSerializer.ArrayPool.Return(buffer);
                    }

                    break;
            }

            return length + hdrSize;
        }

        /// <inheritdoc />
        public override int WriteObject(IReadOnlyList<byte> value, Stream stream)
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
                    // todo use temp buffer instead of full buffer
                    byte[] buffer = length <= DefaultSerializer.MaxArrayPoolSize ? DefaultSerializer.ArrayPool.Rent(length) : new byte[length];
                    try
                    {
                        if (value is IList<byte> list)
                            list.CopyTo(buffer, 0);
                        else
                        {
                            for (int i = 0; i < value.Count; ++i)
                                buffer[i] = value[i];
                        }
                        stream.Write(buffer, 0, length);
                    }
                    finally
                    {
                        if (length <= DefaultSerializer.MaxArrayPoolSize)
                            DefaultSerializer.ArrayPool.Return(buffer);
                    }

                    break;
            }

            return length + hdrSize;
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

            byte[] arr = new byte[length];
            bytesRead = (int)index + length;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            fixed (byte* ptr = arr)
            {
                Buffer.MemoryCopy(bytes, ptr, length, maxSize - index);
            }

            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, Span<byte> output, out int bytesRead, bool hasReadLength = true)
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

            bytes += bytesRead;
            bytesRead += length;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            Unsafe.CopyBlockUnaligned(ref output[0], ref Unsafe.AsRef<byte>(bytes), (uint)length);
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(byte* bytes, uint maxSize, IList<byte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    while (length > output.Count)
                        output.Add(0);
                }
            }

            if (length <= 0)
                return 0;

            int size = (length - 1) / 8 + 1;
            bytes += bytesRead;
            bytesRead += size;

            if (maxSize < bytesRead)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 1 };

            if (setInsteadOfAdding)
            {
                if (output is byte[] arr || output is List<byte> list && Accessor.TryGetUnderlyingArray(list, out arr))
                {
                    Unsafe.CopyBlockUnaligned(ref arr[0], ref Unsafe.AsRef<byte>(bytes), (uint)length);
                }
                else
                {
                    for (int i = 0; i < length; ++i)
                        output[i] = bytes[i];
                }
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

            byte[] arr = new byte[length];
            int rdCt = stream.Read(arr, 0, length);

            if (rdCt != length)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

            return arr;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, Span<byte> output, out int bytesRead, bool hasReadLength = true)
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            int rdCt = stream.Read(output);
            if (rdCt != length)
                throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

#else
            byte[] buffer = length <= DefaultSerializer.MaxArrayPoolSize ? DefaultSerializer.ArrayPool.Rent(length) : new byte[length];
            try
            {
                int readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };
                Unsafe.CopyBlockUnaligned(ref output[0], ref buffer[0], (uint)length); // todo use temp buffer instead of full buffer
            }
            finally
            {
                if (length <= DefaultSerializer.MaxArrayPoolSize)
                    DefaultSerializer.ArrayPool.Return(buffer);
            }
#endif
            return length;
        }

        /// <inheritdoc />
        public override int ReadObject(Stream stream, IList<byte> output, out int bytesRead, int measuredCount = -1, bool hasReadLength = false, bool setInsteadOfAdding = false)
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
                    while (length > output.Count)
                        output.Add(0);
                }
            }

            if (length == 0)
                return 0;

            int readCt;
            if (setInsteadOfAdding && output is byte[] arr || output is List<byte> list && Accessor.TryGetUnderlyingArray(list, out arr))
            {
                readCt = stream.Read(arr, 0, length);
                if (readCt != length)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

                return length;
            }

            // todo use temp buffer instead of full buffer
            byte[] buffer = length <= DefaultSerializer.MaxArrayPoolSize ? DefaultSerializer.ArrayPool.Rent(length) : new byte[length];
            try
            {
                readCt = stream.Read(buffer, 0, length);
                if (readCt != length)
                    throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, Accessor.ExceptionFormatter.Format(GetType()))) { ErrorCode = 2 };

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
                if (length <= DefaultSerializer.MaxArrayPoolSize)
                    DefaultSerializer.ArrayPool.Return(buffer);
            }

            return length;
        }
    }
}
