#if NET7_0_OR_GREATER
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class Int128Parser : BinaryTypeParser<Int128>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 16;
    public override unsafe int WriteObject(Int128 value, byte* bytes, uint maxSize)
    {
        if (maxSize < 16)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(Int128Parser))) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            bytes[15] = unchecked((byte)value);
            bytes[14] = unchecked((byte)(value >>> 8));
            bytes[13] = unchecked((byte)(value >>> 16));
            bytes[12] = unchecked((byte)(value >>> 24));
            bytes[11] = unchecked((byte)(value >>> 32));
            bytes[10] = unchecked((byte)(value >>> 40));
            bytes[9]  = unchecked((byte)(value >>> 48));
            bytes[8]  = unchecked((byte)(value >>> 56));
            bytes[7]  = unchecked((byte)(value >>> 64));
            bytes[6]  = unchecked((byte)(value >>> 72));
            bytes[5]  = unchecked((byte)(value >>> 80));
            bytes[4]  = unchecked((byte)(value >>> 88));
            bytes[3]  = unchecked((byte)(value >>> 96));
            bytes[2]  = unchecked((byte)(value >>> 104));
            bytes[1]  = unchecked((byte)(value >>> 112));
            *bytes    = unchecked((byte)(value >>> 120));
        }
        
        return 16;
    }
    public override int WriteObject(Int128 value, Stream stream)
    {
        Span<byte> span = stackalloc byte[16];
            
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(ref span[0], value);
        }
        else
        {
            span[15] = unchecked((byte)value);
            span[14] = unchecked((byte)(value >>> 8));
            span[13] = unchecked((byte)(value >>> 16));
            span[12] = unchecked((byte)(value >>> 24));
            span[11] = unchecked((byte)(value >>> 32));
            span[10] = unchecked((byte)(value >>> 40));
            span[9]  = unchecked((byte)(value >>> 48));
            span[8]  = unchecked((byte)(value >>> 56));
            span[7]  = unchecked((byte)(value >>> 64));
            span[6]  = unchecked((byte)(value >>> 72));
            span[5]  = unchecked((byte)(value >>> 80));
            span[4]  = unchecked((byte)(value >>> 88));
            span[3]  = unchecked((byte)(value >>> 96));
            span[2]  = unchecked((byte)(value >>> 104));
            span[1]  = unchecked((byte)(value >>> 112));
            span[0]  = unchecked((byte)(value >>> 120));
        }

        stream.Write(span);
        return 16;
    }
    public override unsafe Int128 ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(Int128Parser))) { ErrorCode = 1 };

        Int128 value;

        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<Int128>(bytes);
        }
        else
        {
            ulong upper = ((ulong)((uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3]) << 32) |
                          ((uint)bytes[4] << 24 | (uint)bytes[5] << 16 | (uint)bytes[6] << 8 | bytes[7]);
            ulong lower = ((ulong)((uint)bytes[8] << 24 | (uint)bytes[9] << 16 | (uint)bytes[10] << 8 | bytes[11]) << 32) |
                          ((uint)bytes[12] << 24 | (uint)bytes[13] << 16 | (uint)bytes[14] << 8 | bytes[15]);
            value = new Int128(upper, lower);
        }

        bytesRead = 16;
        return value;
    }
    public override Int128 ReadObject(Stream stream, out int bytesRead)
    {
        Span<byte> span = stackalloc byte[16];
        int ct = stream.Read(span);

        if (ct != 16)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(Int128Parser))) { ErrorCode = 2 };

        Int128 value;

        if (BitConverter.IsLittleEndian)
        {
            value = Unsafe.ReadUnaligned<Int128>(ref span[0]);
        }
        else
        {
            ulong upper = ((ulong)((uint)span[0] << 24 | (uint)span[1] << 16 | (uint)span[2] << 8 | span[3]) << 32) |
                          ((uint)span[4] << 24 | (uint)span[5] << 16 | (uint)span[6] << 8 | span[7]);
            ulong lower = ((ulong)((uint)span[8] << 24 | (uint)span[9] << 16 | (uint)span[10] << 8 | span[11]) << 32) |
                          ((uint)span[12] << 24 | (uint)span[13] << 16 | (uint)span[14] << 8 | span[15]);
            value = new Int128(upper, lower);
        }

        bytesRead = 16;
        return value;
    }
    public class Many(SerializationConfiguration config) : UnmanagedValueTypeBinaryArrayTypeParser<Int128>(config);
}
#endif