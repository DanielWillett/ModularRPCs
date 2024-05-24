using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideRemoteConnection : IModularRpcRemoteConnection, IModularRpcServersideConnection
{
    public LoopbackRpcServersideLocalConnection Local { get; internal set; }
    public LoopbackRpcClientsideRemoteConnection Client { get; internal set; } = null!;
    public LoopbackEndpoint Endpoint { get; }
    public bool IsClosed { get; internal set; }
    internal LoopbackRpcServersideRemoteConnection(LoopbackEndpoint endPoint, IRpcRouter router, IRpcSerializer serializer)
    {
        if (!endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedServersideEndpoint, nameof(endPoint));
        IsClosed = true;
        Endpoint = endPoint;
        Local = new LoopbackRpcServersideLocalConnection(this, router);
    }

    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        return Local.Router.ReceiveData(Client, serializer, rawData, token);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        return Local.Router.ReceiveData(Client, serializer, streamData, token);
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