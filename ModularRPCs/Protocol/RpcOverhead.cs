using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Protocol;
public class RpcOverhead
{
    private readonly uint _size2Check;
    internal const int MinimumSize = 23;

    /// <summary>
    /// The size in bytes of the message this <see cref="RpcOverhead"/> represents, not including the overhead itself.
    /// </summary>
    public uint MessageSize { get; }
    
    /// <summary>
    /// The size of overhead header this <see cref="RpcOverhead"/> would create.
    /// </summary>
    public int OverheadSize { get; private set; }

    /// <summary>
    /// Unique (to the sender) id of this message.
    /// </summary>
    public ulong MessageId { get; }

    /// <summary>
    /// Sub-message id within the unique <see cref="MessageId"/>. Ex. responses have the same message id but will have a different sub-message id.
    /// </summary>
    public byte SubMessageId { get; }

    /// <summary>
    /// The RPC invocation target to be called.
    /// </summary>
    public IRpcInvocationPoint Rpc { get; }

    /// <summary>
    /// The remote side of the connection representing the remote client or server that sent the RPC.
    /// </summary>
    public IModularRpcRemoteConnection SendingConnection { get; }

    /// <summary>
    /// The local size of the connection representing this client or server that the RPC is meant for.
    /// </summary>
    public IModularRpcLocalConnection ReceivingConnection { get; }
    
    /// <summary>
    /// Various bitwise settings for an RPC call.
    /// </summary>
    public RpcFlags Flags { get; }
    internal RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, RpcFlags flags, uint messageSize, ulong messageId, byte subMsgId)
        : this(sendingConnection, rpc, flags, messageSize, messageSize, messageId, subMsgId, CalculateOverheadSize(rpc)) { }
    private RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, RpcFlags flags, uint messageSize, uint size2Check, ulong messageId, byte subMsgId, int overheadSize)
    {
        MessageId = messageId;
        SubMessageId = subMsgId;
        SendingConnection = sendingConnection;
        ReceivingConnection = sendingConnection.Local;
        MessageSize = messageSize;
        Rpc = rpc;
        Flags = flags;
        _size2Check = size2Check;
        OverheadSize = overheadSize;
    }
    private static int CalculateOverheadSize(IRpcInvocationPoint rpc)
    {
        int size = 19;

        size += rpc is { CanCache: true, EndpointId: not null } ? sizeof(uint) : rpc.Size;

        return size;
    }
    internal bool CheckSizeHashValid() => _size2Check == MessageSize;
    public static RpcOverhead ReadFromStream(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byte[] bytes = new byte[19];

        int byteCt = stream.Read(bytes, 0, 19);
#else
        Span<byte> bytes = stackalloc byte[19];

        int byteCt = stream.Read(bytes);
#endif
        if (byteCt < 19)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        RpcFlags flags = isLittleEndian ? (RpcFlags)(bytes[0] | bytes[1] << 8) : (RpcFlags)(bytes[0] << 8 | bytes[1]);
        int index = sizeof(RpcFlags);

        uint size = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref bytes[index])
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        uint size2 = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref bytes[index])
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        ulong messageId = isLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(ref bytes[index])
            : ((ulong)((uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3]) << 32) | ((uint)bytes[index + 4] << 24 | (uint)bytes[index + 5] << 16 | (uint)bytes[index + 6] << 8 | bytes[index + 7]);

        byte subMsgId = bytes[index + 8];

        index += 9;

        IRpcInvocationPoint? endPoint;
        if ((flags & RpcFlags.HasFullEndpoint) == 0)
        {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, 4);
#else
            byteCt = stream.Read(bytes[..4]);
#endif
            if (byteCt < 4)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            uint endpointId = isLittleEndian
                ? Unsafe.ReadUnaligned<uint>(ref bytes[index])
                : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

            index += sizeof(uint);
            endPoint = sendingConnection.Local.Router.FindSavedRpcEndpoint(endpointId);
            if (endPoint == null)
            {
                string err = string.Format(Properties.Exceptions.RpcOverheadParseExceptionUnknownRpcDescriptor, endpointId);
                throw new RpcOverheadParseException(err) { ErrorCode = 3 };
            }

            object? identifier = null;
            if ((flags & RpcFlags.EndpointCodeIncludesIdentifier) != 0)
            {
                identifier = RpcEndpoint.ReadIdentifierFromStream(serializer, stream, out int bytesReadIdentifier);
                index += bytesReadIdentifier;
            }
            if (!ReferenceEquals(endPoint.Identifier, identifier))
            {
                endPoint = endPoint.CloneWithIdentifier(serializer, identifier);
            }
        }
        else
        {
            endPoint = RpcEndpoint.ReadFromStream(serializer, sendingConnection.Local.Router, stream, out int bytesRead);
            index += bytesRead;
        }

        return new RpcOverhead(sendingConnection, endPoint, flags, size, size2, messageId, subMsgId, index);
    }
    public static unsafe RpcOverhead ReadFromBytes(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, byte* bytes, uint maxCt)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

        if (maxCt < 19)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        RpcFlags flags = isLittleEndian ? (RpcFlags)(*bytes | bytes[1] << 8) : (RpcFlags)(*bytes << 8 | bytes[1]);
        int index = sizeof(RpcFlags);

        uint size = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes + index)
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        uint size2 = isLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes + index)
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        ulong messageId = isLittleEndian
            ? Unsafe.ReadUnaligned<ulong>(bytes + index)
            : ((ulong)((uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3]) << 32) | ((uint)bytes[index + 4] << 24 | (uint)bytes[index + 5] << 16 | (uint)bytes[index + 6] << 8 | bytes[index + 7]);

        byte subMsgId = bytes[index + 8];

        index += 9;

        IRpcInvocationPoint? endPoint;
        if ((flags & RpcFlags.HasFullEndpoint) == 0)
        {
            if (maxCt < index + 4)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            uint endpointId = isLittleEndian
                ? Unsafe.ReadUnaligned<uint>(bytes + index)
                : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

            index += sizeof(uint);
            endPoint = sendingConnection.Local.Router.FindSavedRpcEndpoint(endpointId);
            if (endPoint == null)
            {
                string err = string.Format(Properties.Exceptions.RpcOverheadParseExceptionUnknownRpcDescriptor, endpointId);
                throw new RpcOverheadParseException(err) { ErrorCode = 3 };
            }

            object? identifier = null;
            if ((flags & RpcFlags.EndpointCodeIncludesIdentifier) != 0)
            {
                identifier = RpcEndpoint.ReadIdentifierFromBytes(serializer, bytes, maxCt - (uint)index, out int bytesReadIdentifier);
                index += bytesReadIdentifier;
            }
            if (!ReferenceEquals(endPoint.Identifier, identifier))
            {
                endPoint = endPoint.CloneWithIdentifier(serializer, identifier);
            }
        }
        else
        {
            endPoint = RpcEndpoint.ReadFromBytes(serializer, sendingConnection.Local.Router, bytes + index, maxCt - (uint)index, out int bytesRead);
            index += bytesRead;
        }

        return new RpcOverhead(sendingConnection, endPoint, flags, size, size2, messageId, subMsgId, index);
    }
}