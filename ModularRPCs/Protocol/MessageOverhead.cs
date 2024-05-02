using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
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
    internal RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, uint messageSize, ulong messageId, byte subMsgId)
        : this(sendingConnection, rpc, messageSize, messageSize, messageId, subMsgId, CalculateOverheadSize(rpc)) { }
    private RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, uint messageSize, uint size2Check, ulong messageId, byte subMsgId, int overheadSize)
    {
        MessageId = messageId;
        SubMessageId = subMsgId;
        SendingConnection = sendingConnection;
        ReceivingConnection = sendingConnection.Local;
        MessageSize = messageSize;
        Rpc = rpc;
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
    internal static RpcOverhead ReadFromStream(IModularRpcRemoteConnection sendingConnection, Stream stream)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

#if NETFRAMEWORK
        byte[] bytes = new byte[19];

        int byteCt = stream.Read(bytes, 0, 19);
#else
        Span<byte> bytes = stackalloc byte[19];

        int byteCt = stream.Read(bytes);
#endif
        if (byteCt < 19)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        ModularRpcFlags flags = isLittleEndian ? (ModularRpcFlags)(bytes[0] | bytes[1] << 8) : (ModularRpcFlags)(bytes[0] << 8 | bytes[1]);
        int index = sizeof(ModularRpcFlags);

        uint size = isLittleEndian
            ? bytes[index] | (uint)bytes[index + 1] << 8 | (uint)bytes[index + 2] << 16 | (uint)bytes[index + 3] << 24
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        uint size2 = isLittleEndian
            ? bytes[index] | (uint)bytes[index + 1] << 8 | (uint)bytes[index + 2] << 16 | (uint)bytes[index + 3] << 24
            : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(uint);

        ulong messageId = isLittleEndian
            ? bytes[index] | (ulong)bytes[index + 1] << 8 | (ulong)bytes[index + 2] << 16 | (ulong)bytes[index + 3] << 24
              | (ulong)bytes[index + 4] << 32 | (ulong)bytes[index + 5] << 40 | (ulong)bytes[index + 6] << 48 | (ulong)bytes[index + 7] << 56
            : (ulong)bytes[index] << 56 | (ulong)bytes[index + 1] << 48 | (ulong)bytes[index + 2] << 40 | (ulong)bytes[index + 3] << 32
              | (ulong)bytes[index + 4] << 24 | (ulong)bytes[index + 5] << 16 | (ulong)bytes[index + 6] << 8 | bytes[index + 7];

        byte subMsgId = bytes[index + 8];

        index += 9;

        IRpcInvocationPoint? endPoint;
        if ((flags & ModularRpcFlags.HasFullEndpoint) == 0)
        {
#if NETFRAMEWORK
            byteCt = stream.Read(bytes, 0, 4);
#else
            byteCt = stream.Read(bytes[..4]);
#endif
            if (byteCt < 4)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            uint endpointId = isLittleEndian
                ? bytes[index] | (uint)bytes[index + 1] << 8 | (uint)bytes[index + 2] << 16 | (uint)bytes[index + 3] << 24
                : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

            index += sizeof(uint);
            endPoint = sendingConnection.Local.Router.FindSavedRpcEndpoint(endpointId);
            if (endPoint == null)
            {
                string err = string.Format(Properties.Exceptions.RpcOverheadParseExceptionUnknownRpcDescriptor, endpointId);
                throw new RpcOverheadParseException(err) { ErrorCode = 3 };
            }
        }
        else
        {
            endPoint = RpcEndpoint.ReadFromStream(sendingConnection.Local.Router, stream, out int bytesRead);
            index += bytesRead;
        }

        return new RpcOverhead(sendingConnection, endPoint, size, size2, messageId, subMsgId, index);
    }
    internal static unsafe RpcOverhead ReadFromBytes(IModularRpcRemoteConnection sendingConnection, byte* bytes, uint maxCt)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

        if (maxCt < 19)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        ModularRpcFlags flags = isLittleEndian ? (ModularRpcFlags)(*bytes | bytes[1] << 8) : (ModularRpcFlags)(*bytes << 8 | bytes[1]);
        int index = sizeof(ModularRpcFlags);

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
            : (ulong)bytes[index] << 56 | (ulong)bytes[index + 1] << 48 | (ulong)bytes[index + 2] << 40 | (ulong)bytes[index + 3] << 32
              | (ulong)bytes[index + 4] << 24 | (ulong)bytes[index + 5] << 16 | (ulong)bytes[index + 6] << 8 | bytes[index + 7];

        byte subMsgId = bytes[index + 8];

        index += 9;

        IRpcInvocationPoint? endPoint;
        if ((flags & ModularRpcFlags.HasFullEndpoint) == 0)
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
        }
        else
        {
            endPoint = RpcEndpoint.ReadFromBytes(sendingConnection.Local.Router, bytes + index, (uint)(maxCt - index), out int bytesRead);
            index += bytesRead;
        }

        return new RpcOverhead(sendingConnection, endPoint, size, size2, messageId, subMsgId, index);
    }
}