using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using UnityEngine;

namespace DanielWillett.ModularRpcs.Serialization.Parsers;
public class UnityColor32Parser : BinaryTypeParser<Color32>
{
    public override bool IsVariableSize => false;
    public override int MinimumSize => 4;
    public override unsafe int WriteObject(Color32 value, byte* bytes, uint maxSize)
    {
        if (maxSize < 4)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionIBinaryTypeParser, nameof(UnityColor32Parser))) { ErrorCode = 1 };

        *bytes = value.a;
        bytes[1] = value.r;
        bytes[2] = value.g;
        bytes[3] = value.b;

        return 4;
    }
    public override int WriteObject(Color32 value, Stream stream)
    {
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(4);
        try
        {
#else
        Span<byte> span = stackalloc byte[4];
#endif

        span[0] = value.a;
        span[1] = value.r;
        span[2] = value.g;
        span[3] = value.b;

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        stream.Write(span, 0, 4);
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
        return 4;
    }
    public override unsafe Color32 ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 4)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutIBinaryTypeParser, nameof(UnityColor32Parser))) { ErrorCode = 1 };

        Color32 v4 = default;

        v4.a = *bytes;
        v4.r = bytes[1];
        v4.g = bytes[2];
        v4.b = bytes[3];

        bytesRead = 4;
        return v4;
    }
    public override Color32 ReadObject(Stream stream, out int bytesRead)
    {
        Color32 v4 = default;
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        byte[] span = DefaultSerializer.ArrayPool.Rent(4);
        try
        {
            int ct = stream.Read(span, 0, 4);
#else
        Span<byte> span = stackalloc byte[4];
        int ct = stream.Read(span);
#endif
            
        bytesRead = ct;
        if (ct != 4)
            throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionStreamRunOutIBinaryTypeParser, nameof(UnityColor32Parser))) { ErrorCode = 2 };

        v4.a = span[0];
        v4.r = span[1];
        v4.g = span[2];
        v4.b = span[3];

#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER || NETFRAMEWORK
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(span);
        }
#endif

        return v4;
    }
    public unsafe class Many : UnmanagedConvValueTypeBinaryArrayTypeParser<Color32>
    {
        public Many(SerializationConfiguration config) : base(config, 16, sizeof(float), false, &WriteToBufferIntl, &WriteToBufferIntl,
            &WriteToBufferSpanIntl, &ReadFromBufferIntl, &ReadFromBufferIntl, &ReadFromBufferSpanIntl)
        {

        }
        private static void WriteToBufferIntl(byte* ptr, Color32 value)
        {
            *ptr = value.a;
            ptr[1] = value.r;
            ptr[2] = value.g;
            ptr[3] = value.b;
        }
        private static void WriteToBufferSpanIntl(Span<byte> span, Color32 value)
        {
            span[0] = value.a;
            span[1] = value.r;
            span[2] = value.g;
            span[3] = value.b;
        }
        private static Color32 ReadFromBufferIntl(byte* ptr)
        {
            Color32 v4 = default;
            v4.a = *ptr;
            v4.r = ptr[1];
            v4.g = ptr[2];
            v4.b = ptr[3];
            return v4;
        }
        private static Color32 ReadFromBufferSpanIntl(Span<byte> span)
        {
            Color32 v4 = default;
            v4.a = span[0];
            v4.r = span[1];
            v4.g = span[2];
            v4.b = span[3];
            return v4;
        }
    }
}