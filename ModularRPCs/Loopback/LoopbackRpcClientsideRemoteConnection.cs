using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideRemoteConnection : IModularRpcRemoteConnection, IModularRpcClientsideConnection
{
    public LoopbackRpcServersideRemoteConnection Server { get; }
    public LoopbackRpcClientsideLocalConnection Local { get; internal set; }
    public LoopbackEndPoint EndPoint { get; }
    public bool IsClosed { get; internal set; }
    internal LoopbackRpcClientsideRemoteConnection(LoopbackEndPoint endPoint, IRpcRouter router, LoopbackRpcServersideRemoteConnection server)
    {
        if (endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedClientsideEndpoint, nameof(endPoint));
        EndPoint = endPoint;
        IsClosed = true;
        Local = new LoopbackRpcClientsideLocalConnection(this, router);
        server.Client = this;
        Server = server;
    }

    IModularRpcRemoteEndPoint IModularRpcRemoteConnection.EndPoint => EndPoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    ValueTask IModularRpcRemoteConnection.SendDataAsync(Stream? streamData, ArraySegment<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Count <= 0 && streamData == null)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        return Server.Local.Router.HandleReceivedData(Server.Local, streamData, rawData, token);
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        Local.IsClosed = true;
        IsClosed = true;
        return default;
    }
}