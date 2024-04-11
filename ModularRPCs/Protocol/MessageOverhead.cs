using System;
using System.Runtime.CompilerServices;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;

namespace DanielWillett.ModularRpcs.Protocol;
public class RpcOverhead
{
    private readonly uint _size2Check;
    internal const int MinimumSize = 7;

    /// <summary>
    /// The size in bytes of the message this <see cref="RpcOverhead"/> represents, not including the overhead itself.
    /// </summary>
    public uint MessageSize { get; private set; }
    
    /// <summary>
    /// The size of overhead header this <see cref="RpcOverhead"/> would create.
    /// </summary>
    public int OverheadSize { get; private set; }

    /// <summary>
    /// The RPC invocation target to be called.
    /// </summary>
    public IRpcInvocationPoint Rpc { get; }

    private RpcOverhead(IRpcInvocationPoint rpc, uint messageSize, uint size2Check, int overheadSize) : this(rpc, messageSize, size2Check)
    {
        OverheadSize = overheadSize;
    }
    private RpcOverhead(IRpcInvocationPoint rpc, uint messageSize, uint size2Check)
    {
        MessageSize = messageSize;
        Rpc = rpc;
        _size2Check = size2Check;
    }
    internal RpcOverhead(IRpcInvocationPoint rpc, uint messageSize)
    {
        MessageSize = messageSize;
        _size2Check = messageSize;
        Rpc = rpc;
    }

    internal bool CheckSizeHashValid() => _size2Check == MessageSize;
    internal static unsafe RpcOverhead ReadFromBytes(IModularRpcLocalConnection connection, byte* bytes, uint maxCt)
    {
        bool isLittleEndian = BitConverter.IsLittleEndian;

        if (maxCt <= sizeof(ushort) + sizeof(int) * 2)
            throw new RpcOverheadParseException { ErrorCode = 1 };

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

        IRpcInvocationPoint? endPoint;
        if ((flags & ModularRpcFlags.HasFullEndpoint) == 0)
        {
            uint endpointId = isLittleEndian
                ? Unsafe.ReadUnaligned<uint>(bytes + index)
                : (uint)bytes[index] << 24 | (uint)bytes[index + 1] << 16 | (uint)bytes[index + 2] << 8 | bytes[index + 3];

            index += sizeof(uint);
            endPoint = connection.Router.FindSavedRpcEndpoint(endpointId);
            if (endPoint == null)
            {
                throw new RpcOverheadParseException(
                    string.Format(Properties.Exceptions.RpcOverheadParseExceptionUnknownRpcDescriptor, endpointId))
                {
                    ErrorCode = 2
                };
            }
        }
        else
        {
            endPoint = RpcEndpoint.FromBytes(connection.Router, bytes + index, (uint)(maxCt - index), out int bytesRead);
            index += bytesRead;
        }

        return new RpcOverhead(endPoint, size, size2, index);
    }
}