using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Async;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Protocol;
public class RpcOverhead
{
    internal const byte OvhCodeId = 1;
    private readonly uint _size2Check;
    internal const int MinimumSize = 23;

    protected internal InvocingRpcState State;

    /// <summary>
    /// The size in bytes of the message this <see cref="RpcOverhead"/> represents, not including the overhead itself.
    /// </summary>
    public uint MessageSize { get; }
    
    /// <summary>
    /// The size of overhead header this <see cref="RpcOverhead"/> would create.
    /// </summary>
    public uint OverheadSize { get; internal set; }

    /// <summary>
    /// Unique (to the sender) id of this message.
    /// </summary>
    public ulong MessageId { get; }

    /// <summary>
    /// Sub-message id within the unique <see cref="MessageId"/>. Ex. responses have the same message id but will have a different sub-message id.
    /// </summary>
    /// <remarks>Submessage ID 0 represents the 'first contact'. 1 is the response. More may be used later.</remarks>
    public byte SubMessageId { get; }

    /// <summary>
    /// The RPC invocation target to be called.
    /// </summary>
    [UsedImplicitly]
    public IRpcInvocationPoint Rpc { get; }

    /// <summary>
    /// The remote side of the connections representing the remote client or server that sent the RPC.
    /// If this RPC is being broadcasted, <see cref="SendingConnections"/> will have a value instead.
    /// </summary>
    [UsedImplicitly]
    public IModularRpcRemoteConnection? SendingConnection { get; internal set; }

    /// <summary>
    /// The local side of the connection representing this client or server that the RPC is meant for.
    /// </summary>
    [UsedImplicitly]
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
    [UsedImplicitly]
    public RpcFlags Flags { get; }


    internal RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, RpcFlags flags, uint messageSize, ulong messageId, byte subMsgId)
        : this(sendingConnection, rpc, flags, messageSize, messageSize, messageId, subMsgId, CalculateOverheadSize(rpc)) { }
    private RpcOverhead(IModularRpcRemoteConnection sendingConnection, IRpcInvocationPoint rpc, RpcFlags flags, uint messageSize, uint size2Check, ulong messageId, byte subMsgId, uint overheadSize)
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
    private static uint CalculateOverheadSize(IRpcInvocationPoint rpc)
    {
        uint size = PrimitiveRpcOverhead.MinimumLength + 2;

        size += rpc is { CanCache: true, EndpointId: not null } ? sizeof(uint) : rpc.Size;

        return size;
    }
    internal bool CheckSizeHashValid() => _size2Check == MessageSize;
    public static RpcOverhead ReadFromStream(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, Stream stream, in PrimitiveRpcOverhead primitiveOverhead)
    {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byte[] bytes = DefaultSerializer.ArrayPool.Rent(2);

        int byteCt = stream.Read(bytes, 0, 2);
#else
        Span<byte> bytes = stackalloc byte[2];

        int byteCt = stream.Read(bytes);
#endif
        if (byteCt != 2)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        RpcFlags flags = BitConverter.IsLittleEndian ? (RpcFlags)(bytes[0] | bytes[1] << 8) : (RpcFlags)(bytes[0] << 8 | bytes[1]);
        int index = 2;

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

            uint endpointId = BitConverter.IsLittleEndian
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

        return new RpcOverhead(sendingConnection, endPoint, flags, primitiveOverhead.Size, primitiveOverhead.SizeCheck, primitiveOverhead.MessageId, primitiveOverhead.SubMessageId, (uint)index);
    }
    public static unsafe RpcOverhead ReadFromBytes(IModularRpcRemoteConnection sendingConnection, IRpcSerializer serializer, byte* bytes, uint maxCt, in PrimitiveRpcOverhead primitiveOverhead)
    {
        if (maxCt < 2)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        RpcFlags flags = BitConverter.IsLittleEndian ? (RpcFlags)Unsafe.ReadUnaligned<ushort>(bytes) : (RpcFlags)(*bytes << 8 | bytes[1]);
        int index = 2;

        IRpcInvocationPoint? endPoint;
        if ((flags & RpcFlags.HasFullEndpoint) == 0)
        {
            if (maxCt < index + 4)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            uint endpointId = BitConverter.IsLittleEndian
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

        return new RpcOverhead(sendingConnection, endPoint, flags, primitiveOverhead.Size, primitiveOverhead.SizeCheck, primitiveOverhead.MessageId, primitiveOverhead.SubMessageId, (uint)index);
    }
    internal static unsafe int WriteToBytes(IRpcSerializer serializer, IRpcRouter router, RuntimeMethodHandle sourceMethodHandle, ref RpcCallMethodInfo callMethodInfo, byte* bytes, int maxCt, uint dataCt, ulong msgId, byte subMsgId)
    {
        const int size = 20;
        if (maxCt < size)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        *bytes = OvhCodeId;
        ++bytes;

        for (int i = 0; i < 2; ++i)
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(bytes, dataCt);
            }
            else
            {
                bytes[3] = unchecked((byte)dataCt);
                bytes[2] = unchecked((byte)(dataCt >>> 8));
                bytes[1] = unchecked((byte)(dataCt >>> 16));
                *bytes = unchecked((byte)(dataCt >>> 24));
            }

            bytes += 4;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, msgId);
        }
        else
        {
            bytes[7] = unchecked((byte)msgId);
            bytes[6] = unchecked((byte)(msgId >>> 8));
            bytes[5] = unchecked((byte)(msgId >>> 16));
            bytes[4] = unchecked((byte)(msgId >>> 24));
            bytes[3] = unchecked((byte)(msgId >>> 32));
            bytes[2] = unchecked((byte)(msgId >>> 40));
            bytes[1] = unchecked((byte)(msgId >>> 48));
            *bytes = unchecked((byte)(msgId >>> 56));
        }

        bytes += 8;

        *bytes = subMsgId;
        ++bytes;

        RpcFlags flags = default;
        if (callMethodInfo.KnownId == 0)
        {
            flags |= RpcFlags.HasFullEndpoint;
        }
        else if (callMethodInfo.HasIdentifier)
        {
            flags |= RpcFlags.EndpointCodeIncludesIdentifier;
        }
        if (callMethodInfo.IsFireAndForget)
        {
            flags |= RpcFlags.FireAndForget;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, (ushort)flags);
        }
        else
        {
            bytes[1] = unchecked( (byte)(ushort) flags );
            *bytes   = unchecked( (byte)((ushort)flags >>> 8) );
        }

        bytes += 2;

        if ((flags & RpcFlags.HasFullEndpoint) != 0)
            return size;

        if (maxCt < size + 4)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        uint enpId = callMethodInfo.KnownId;
        if (BitConverter.IsLittleEndian)
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

public struct InvocingRpcState
{
    public CombinedTokenSources CancelToken;
    public bool HasCancelToken;
    public DefaultRpcRouter.UniqueMessageKey Key;
}