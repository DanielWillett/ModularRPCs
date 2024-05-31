using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Responsible for reading the initial message header. Depending on the first byte this may be converted to <see cref="RpcOverhead"/>.
/// </summary>
public struct PrimitiveRpcOverhead
{
    public const int MinimumLength = 18;
    private RpcOverhead? _overhead;
    private uint _len;
    public byte CodeId { get; }
    public uint Size { get; }
    public uint SizeCheck { get; }
    public ulong MessageId { get; }
    public byte SubMessageId { get; }
    public uint OverheadSize => _len;
    public RpcOverhead? FullOverhead => _overhead;
    public PrimitiveRpcOverhead(byte codeId, uint size, uint sizeCheck, ulong messageId, byte subMessageId)
    {
        CodeId = codeId;
        Size = size;
        SizeCheck = sizeCheck;
        MessageId = messageId;
        SubMessageId = subMessageId;
        _len = MinimumLength;
    }
    public static unsafe PrimitiveRpcOverhead ReadFromBytes(IModularRpcRemoteConnection remote, IRpcSerializer serializer, byte* rawData, uint maxSize)
    {
        if (maxSize < 18)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };
        
        bool isLittleEndian = BitConverter.IsLittleEndian;

        byte codeId = rawData[0];
        uint size = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in rawData[1]))
            : (uint)rawData[1] << 24 | (uint)rawData[2] << 16 | (uint)rawData[3] << 8 | rawData[4];
        
        uint size2 = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in rawData[5]))
            : (uint)rawData[5] << 24 | (uint)rawData[6] << 16 | (uint)rawData[7] << 8 | rawData[8];

        ulong messageId = isLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in rawData[9]))
            : ((ulong)((uint)rawData[9] << 24 | (uint)rawData[10] << 16 | (uint)rawData[11] << 8 | rawData[12]) << 32) | ((uint)rawData[13] << 24 | (uint)rawData[14] << 16 | (uint)rawData[15] << 8 | rawData[16]);

        byte subMsgId = rawData[17];

        PrimitiveRpcOverhead ovh = new PrimitiveRpcOverhead(codeId, size, size2, messageId, subMsgId);

        if (codeId != RpcOverhead.OvhCodeId)
            return ovh;

        RpcOverhead overhead = RpcOverhead.ReadFromBytes(remote, serializer, rawData + 18, maxSize - 18, in ovh);
        ovh._overhead = overhead;
        ovh._len += overhead.OverheadSize;
        overhead.OverheadSize = ovh._len;
        return ovh;
    }
    public static PrimitiveRpcOverhead ReadFromStream(IModularRpcRemoteConnection remote, IRpcSerializer serializer, Stream stream)
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        PrimitiveRpcOverhead ovh;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> headerSpan = stackalloc byte[18];
        int rdCt = stream.Read(headerSpan);
#else
        
        byte[] headerSpan = DefaultSerializer.ArrayPool.Rent(18);
        try
        {
            int rdCt = stream.Read(headerSpan, 0, 18);
#endif

        if (rdCt != 18)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        bool isLittleEndian = BitConverter.IsLittleEndian;

        byte codeId = headerSpan[0];
        uint size = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref headerSpan[1])
            : (uint)headerSpan[1] << 24 | (uint)headerSpan[2] << 16 | (uint)headerSpan[3] << 8 | headerSpan[4];

        uint size2 = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref headerSpan[5])
            : (uint)headerSpan[5] << 24 | (uint)headerSpan[6] << 16 | (uint)headerSpan[7] << 8 | headerSpan[8];

        ulong messageId = isLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ref headerSpan[9])
            : ((ulong)((uint)headerSpan[9] << 24 | (uint)headerSpan[10] << 16 | (uint)headerSpan[11] << 8 | headerSpan[12]) << 32) | ((uint)headerSpan[13] << 24 | (uint)headerSpan[14] << 16 | (uint)headerSpan[15] << 8 | headerSpan[16]);

        byte subMsgId = headerSpan[17];

        ovh = new PrimitiveRpcOverhead(codeId, size, size2, messageId, subMsgId);
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        }
        finally
        {
            DefaultSerializer.ArrayPool.Return(headerSpan);
        }
#endif

        if (ovh.CodeId != RpcOverhead.OvhCodeId)
            return ovh;

        RpcOverhead overhead = RpcOverhead.ReadFromStream(remote, serializer, stream, in ovh);
        ovh._overhead = overhead;
        ovh._len += overhead.OverheadSize;
        overhead.OverheadSize = ovh._len;
        return ovh;
    }

    public override string ToString()
    {
        if (_overhead != null)
            return _overhead.Rpc.ToString();

        return CodeId switch
        {
            DefaultRpcRouter.OvhCodeIdVoidRtnSuccess  => "void-rtn",
            DefaultRpcRouter.OvhCodeIdValueRtnSuccess => "val-rtn",
            DefaultRpcRouter.OvhCodeIdException       => "err-rtn",
            _ => CodeId.ToString(CultureInfo.InvariantCulture) + "-rtn"
        };
    }
}
