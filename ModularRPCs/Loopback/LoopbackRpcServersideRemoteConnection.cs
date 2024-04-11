using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideRemoteConnection : IModularRpcRemoteConnection, IModularRpcServersideConnection
{
    public LoopbackRpcServersideLocalConnection Local { get; internal set; } = null!;
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
    ValueTask IModularRpcRemoteConnection.SendDataAsync(Stream? streamData, ArraySegment<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Count <= 0 && streamData == null)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        return Client.Local.Router.HandleReceivedData(Client.Local, streamData, rawData, token);
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