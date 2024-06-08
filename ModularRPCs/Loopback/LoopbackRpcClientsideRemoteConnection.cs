using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideRemoteConnection : IModularRpcRemoteConnection, IModularRpcClientsideConnection
{
    public LoopbackRpcServersideRemoteConnection Server { get; }
    public LoopbackRpcClientsideLocalConnection Local { get; internal set; }
    public LoopbackEndpoint Endpoint { get; }
    public bool IsClosed { get; internal set; }
    public IRpcConnectionLifetime? Lifetime { get; }
    internal LoopbackRpcClientsideRemoteConnection(LoopbackEndpoint endPoint, IRpcRouter router, IRpcConnectionLifetime? lifetime, LoopbackRpcServersideRemoteConnection server)
    {
        if (endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedClientsideEndpoint, nameof(endPoint));
        Endpoint = endPoint;
        Lifetime = lifetime;
        IsClosed = true;
        Local = new LoopbackRpcClientsideLocalConnection(this, router);
        server.Client = this;
        Server = server;
    }

    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        byte[] rtnBuffer = new byte[rawData.Length];
        rawData.CopyTo(rtnBuffer);
        return Local.Router.ReceiveData(Server, serializer, rtnBuffer, true, token);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        return Local.Router.ReceiveData(Server, serializer, streamData, token);
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public async ValueTask CloseAsync(CancellationToken token = default)
    {
        Local.IsClosed = true;
        IsClosed = true;
        if (Lifetime != null)
        {
            await Lifetime.TryRemoveConnection(this, CancellationToken.None);
        }
    }
}