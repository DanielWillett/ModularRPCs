using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcClientsideRemoteConnection : IModularRpcRemoteConnection, IModularRpcClientsideConnection
{
    public LoopbackRpcServersideRemoteConnection Server { get; }
    public LoopbackRpcClientsideLocalConnection Local { get; internal set; }
    public LoopbackEndpoint Endpoint { get; }
    public bool IsClosed { get; internal set; }
    internal LoopbackRpcClientsideRemoteConnection(LoopbackEndpoint endPoint, IRpcRouter router, LoopbackRpcServersideRemoteConnection server)
    {
        if (endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedClientsideEndpoint, nameof(endPoint));
        Endpoint = endPoint;
        IsClosed = true;
        Local = new LoopbackRpcClientsideLocalConnection(this, router);
        server.Client = this;
        Server = server;
    }

    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    unsafe ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        RpcOverhead overhead;
        fixed (byte* ptr = rawData)
        {
            overhead = RpcOverhead.ReadFromBytes(Server, serializer, ptr, (uint)rawData.Length);
        }

        return Local.Router.InvokeInvocationPoint(overhead.Rpc, overhead, serializer, rawData.Length == overhead.OverheadSize ? default : rawData.Slice(overhead.OverheadSize), token);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        RpcOverhead overhead = RpcOverhead.ReadFromStream(Server, serializer, streamData);

        return Local.Router.InvokeInvocationPoint(overhead.Rpc, overhead, serializer, streamData, token);
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        Local.IsClosed = true;
        IsClosed = true;
        return default;
    }
}