using System;
using System.Net.WebSockets;
using DanielWillett.ModularRpcs.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketClientsideLocalRpcConnection : WebSocketLocalRpcConnection, IModularRpcLocalConnection, IModularRpcClientsideConnection
{
    private readonly CancellationTokenSource _listenToken;
    protected override WebSocket WebSocket => Remote.WebSocket;
    public override bool IsClosed => Remote.IsClosed;
    internal WebSocketClientsideRemoteRpcConnection Remote { get; }
    internal WebSocketClientsideLocalRpcConnection(IRpcRouter router, WebSocketClientsideRemoteRpcConnection remote) : base(router)
    {
        _listenToken = new CancellationTokenSource();
        Remote = remote;
        Remote.Local = this;
    }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
    public ValueTask DisposeAsync()
    {
        return Remote.DisposeAsync();
    }
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        return Remote.CloseAsync(token);
    }
}