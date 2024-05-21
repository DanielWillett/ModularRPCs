using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;

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
    /// The remote side of the connections representing the remote client or server that sent the RPC.
    /// If this RPC is being broadcasted, <see cref="SendingConnections"/> will have a value instead.
    /// </summary>
    public IModularRpcRemoteConnection? SendingConnection { get; internal set; }

    /// <summary>
    /// The local size of the connection representing this client or server that the RPC is meant for.
    /// </summary>
    public IModularRpcLocalConnection ReceivingConnection { get; }

    /// <summary>
    /// The remote side of the connections representing the remote client or server that sent the RPC.
    /// If this RPC is being broadcasted, this will have multiple connections, otherwise it'll be null.
    /// </summary>
    /// <remarks>This will never have a value when it's received since recipients won't know about other connections.</remarks>
    public IModularRpcRemoteConnection[]? SendingConnections { get; internal set; }

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
        byte[] bytes = DefaultSerializer.ArrayPool.Rent(19);

        int byteCt = stream.Read(bytes, 0, 19);
#else
        Span<byte> bytes = stackalloc byte[19];

        int byteCt = stream.Read(bytes);
#endif
        if (byteCt != 19)
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

        RpcFlags flags = isLittleEndian ? (RpcFlags)Unsafe.ReadUnaligned<ushort>(bytes) : (RpcFlags)(*bytes << 8 | bytes[1]);
        int index = sizeof(ushort);

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
    internal static unsafe int WriteToBytes(IRpcSerializer serializer, IRpcRouter router, RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo, byte* bytes, int maxCt, uint dataCt, ulong msgId, byte subMsgId)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

        const int size = 19;
        if (maxCt < size)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        RpcFlags flags = default;
        if (callMethodInfo.KnownId == 0)
        {
            flags |= RpcFlags.HasFullEndpoint;
        }
        else if (callMethodInfo.HasIdentifier)
        {
            flags |= RpcFlags.EndpointCodeIncludesIdentifier;
        }

        if (isLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, (ushort)flags);
        }
        else
        {
            bytes[1] = unchecked( (byte)(ushort) flags );
            *bytes   = unchecked( (byte)((ushort)flags >>> 8) );
        }

        bytes += 2;

        for (int i = 0; i < 2; ++i)
        {
            if (isLittleEndian)
            {
                Unsafe.WriteUnaligned(bytes, dataCt);
            }
            else
            {
                bytes[3] = unchecked( (byte) dataCt );
                bytes[2] = unchecked( (byte)(dataCt >>> 8)  );
                bytes[1] = unchecked( (byte)(dataCt >>> 16) );
                *bytes   = unchecked( (byte)(dataCt >>> 24) );
            }

            bytes += 4;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, msgId);
        }
        else
        {
            bytes[7] = unchecked( (byte) msgId );
            bytes[6] = unchecked( (byte)(msgId >>> 8)  );
            bytes[5] = unchecked( (byte)(msgId >>> 16) );
            bytes[4] = unchecked( (byte)(msgId >>> 24) );
            bytes[3] = unchecked( (byte)(msgId >>> 32) );
            bytes[2] = unchecked( (byte)(msgId >>> 40) );
            bytes[1] = unchecked( (byte)(msgId >>> 48) );
            *bytes   = unchecked( (byte)(msgId >>> 56) );
        }

        bytes += 8;

        *bytes = subMsgId;
        ++bytes;

        if ((flags & RpcFlags.HasFullEndpoint) != 0)
            return size;

        if (maxCt < size + 4)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        uint enpId = callMethodInfo.KnownId;
        if (isLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, enpId);
        }
        else
        {
            bytes[3] = unchecked( (byte) enpId );
            bytes[2] = unchecked( (byte)(enpId >>> 8)  );
            bytes[1] = unchecked( (byte)(enpId >>> 16) );
            *bytes   = unchecked( (byte)(enpId >>> 24) );
        }

        //bytes += 4;
        return size + 4;
    }
}