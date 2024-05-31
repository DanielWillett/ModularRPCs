using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketEndpoint(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null) : IModularRpcRemoteEndpoint
{
    internal Action<ClientWebSocketOptions>? ConfigureOptions = configureOptions;
    public int StreamSendBufferSize { get; set; } = 4096;
    public int ReceiveBufferSize { get; set; } = 4096;
    public Uri Uri { get; } = uri;
    public async Task<IModularRpcRemoteConnection> RequestConnectionAsync(IRpcRouter router, IRpcConnectionLifetime connectionLifetime, IRpcSerializer serializer, CancellationToken token = default)
    {
        ClientWebSocket ws = new ClientWebSocket();

        ConfigureOptions?.Invoke(ws.Options);

        await ws.ConnectAsync(Uri, token).ConfigureAwait(false);
        WebSocketClientsideRemoteRpcConnection remote = new WebSocketClientsideRemoteRpcConnection(this, ws, StreamSendBufferSize);
        _ = new WebSocketClientsideLocalRpcConnection(router, serializer, remote, ReceiveBufferSize);

        await connectionLifetime.TryAddNewConnection(remote, token);

        return remote;
    }
}
