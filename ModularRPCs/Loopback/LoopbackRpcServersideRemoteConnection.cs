using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
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
    public IRpcConnectionLifetime? Lifetime { get; }
    internal LoopbackRpcServersideRemoteConnection(LoopbackEndpoint endPoint, IRpcRouter router, IRpcConnectionLifetime? lifetime)
    {
        if (!endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedServersideEndpoint, nameof(endPoint));
        Lifetime = lifetime;
        IsClosed = true;
        Endpoint = endPoint;
        Local = new LoopbackRpcServersideLocalConnection(this, router);
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
        return Local.Router.ReceiveData(Client, serializer, rtnBuffer, true, token);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        return Local.Router.ReceiveData(Client, serializer, streamData, token);
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public async ValueTask CloseAsync(CancellationToken token = default)
    {
        IsClosed = true;
        Local.IsClosed = true;
        Client.IsClosed = true;
        Client.Local.IsClosed = true;
        if (Lifetime != null)
        {
            await Lifetime.TryRemoveConnection(this, CancellationToken.None);
        }
    }
}