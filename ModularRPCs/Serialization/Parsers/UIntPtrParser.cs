using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UIntPtrParser : BinaryTypeParser<nuint>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(nuint value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UIntPtrParser))) { ErrorCode = 1 };

        ulong v = value;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, v);
        }
        else
        {
            bytes[7] = unchecked((byte)v);
            bytes[6] = unchecked((byte)(v >>> 8));
            bytes[5] = unchecked((byte)(v >>> 16));
            bytes[4] = unchecked((byte)(v >>> 24));
            bytes[3] = unchecked((byte)(v >>> 32));
            bytes[2] = unchecked((byte)(v >>> 40));
            bytes[1] = unchecked((byte)(v >>> 48));
            *bytes   = unchecked((byte)(v >>> 56));
        }
        
        return 8;
    }
    public override int WriteObject(nuint value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
#else
        Span<byte> span = stackalloc byte[8];
#endif
            
        ulong v = value;
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], v);
        }
        else
        {
            span[7] = unchecked((byte)v);
            span[6] = unchecked((byte)(v >>> 8));
            span[5] = unchecked((byte)(v >>> 16));
            span[4] = unchecked((byte)(v >>> 24));
            span[3] = unchecked((byte)(v >>> 32));
            span[2] = unchecked((byte)(v >>> 40));
            span[1] = unchecked((byte)(v >>> 48));
            span[0] = unchecked((byte)(v >>> 56));
        }

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 8);
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
        return 8;
    }
    public override unsafe nuint ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UIntPtrParser))) { ErrorCode = 1 };

        ulong value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(bytes)
            : ((ulong)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) | ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);

        bytesRead = 8;
        if (UIntPtr.Size == 4)
            CheckUInt32Overflow(value);
        return (nuint)value;
    }
    public override nuint ReadObject(Stream stream, out int bytesRead)
    {
        ulong value;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(8);
        try
        {
            int ct = stream.Read(span, 0, 8);
#else
        Span<byte> span = stackalloc byte[8];
        int ct = stream.Read(span);
#endif

        bytesRead = ct;
        if (ct != 8)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UIntPtrParser))) { ErrorCode = 2 };
        
        value = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ref span[0])
            : ((ulong)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) | ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif
        if (UIntPtr.Size == 4)
            CheckUInt32Overflow(value);

        return (nuint)value;
    }
    private static void CheckUInt32Overflow(ulong value)
    {
        if (value > uint.MaxValue)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, nameof(UIntPtrParser))) { ErrorCode = 9 };
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<nuint>
    {
        public Many(SerializationConfiguration config) : base(config, sizeof(ulong), sizeof(ulong), true,
            &WriteToBufferIntl,
            &WriteToBufferUnalignedIntl,
            &WriteToBufferSpanIntl,
            UIntPtr.Size == 8 ? &ReadFromBufferIntl : &ReadFromBufferIntl32,
            UIntPtr.Size == 8 ? &ReadFromBufferUnalignedIntl : &ReadFromBufferUnalignedIntl32,
            UIntPtr.Size == 8 ? &ReadFromBufferSpanIntl : &ReadFromBufferSpanIntl32)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, nuint v)
        {
            *(nuint*)ptr = v;
        }
        private static void WriteToBufferUnalignedIntl(byte* ptr, nuint v)
        {
            Unsafe.WriteUnaligned(ptr, v);
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, nuint v)
        {
            ulong v64 = v;
            MemoryMarshal.Write(span, ref v64);
        }
        private static nuint ReadFromBufferIntl(byte* ptr)
        {
            return *(nuint*)ptr;
        }
        private static nuint ReadFromBufferIntl32(byte* ptr)
        {
            ulong v = *(ulong*)ptr;
            if (UIntPtr.Size == 4)
                CheckUInt32Overflow(v);
            return (nuint)v;
        }
        private static nuint ReadFromBufferUnalignedIntl(byte* ptr)
        {
            return Unsafe.ReadUnaligned<nuint>(ptr);
        }
        private static nuint ReadFromBufferUnalignedIntl32(byte* ptr)
        {
            ulong v = Unsafe.ReadUnaligned<ulong>(ptr);
            if (UIntPtr.Size == 4)
                CheckUInt32Overflow(v);
            return (nuint)v;
        }
        private static nuint ReadFromBufferSpanIntl(Span<byte> span)
        {
            return MemoryMarshal.Read<nuint>(span);
        }
        private static nuint ReadFromBufferSpanIntl32(Span<byte> span)
        {
            ulong v = MemoryMarshal.Read<ulong>(span);
            if (UIntPtr.Size == 4)
                CheckUInt32Overflow(v);
            return (nuint)v;
        }
    }
}