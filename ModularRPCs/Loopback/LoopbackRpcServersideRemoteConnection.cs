using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideRemoteConnection : IModularRpcRemoteConnection, IModularRpcServersideConnection
{
    public LoopbackRpcServersideLocalConnection Local { get; internal set; }
    public LoopbackRpcClientsideRemoteConnection Client { get; internal set; } = null!;
    public LoopbackEndPoint EndPoint { get; }
    public bool IsClosed { get; internal set; }
    internal LoopbackRpcServersideRemoteConnection(LoopbackEndPoint endPoint, IRpcRouter router)
    {
        if (!endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedServersideEndpoint, nameof(endPoint));
        IsClosed = true;
        EndPoint = endPoint;
        Local = new LoopbackRpcServersideLocalConnection(this, router);
    }

    IModularRpcRemoteEndPoint IModularRpcRemoteConnection.EndPoint => EndPoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    unsafe ValueTask IModularRpcRemoteConnection.SendDataAsync(ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        RpcOverhead overhead;
        fixed (byte* ptr = rawData)
        {
            overhead = RpcOverhead.ReadFromBytes(Client, ptr, (uint)rawData.Length);
        }

        return Client.Local.Router.HandleReceivedData(overhead, rawData, token);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        RpcOverhead overhead = RpcOverhead.ReadFromStream(Client, streamData);

        return Client.Local.Router.HandleReceivedData(overhead, streamData, token);
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        IsClosed = true;
        Local.IsClosed = true;
        Client.IsClosed = true;
        Client.Local.IsClosed = true;
        return default;
    }
}