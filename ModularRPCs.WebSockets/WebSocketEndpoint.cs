using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketEndpoint(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null) : IModularRpcRemoteEndPoint
{
    public Uri Uri { get; } = uri;
    public async Task<IModularRpcLocalConnection> RequestConnectionAsync(IRpcRouter router, CancellationToken token = default)
    {
        ClientWebSocket ws = new ClientWebSocket
        {
            Options =
            {
                UseDefaultCredentials = true,
            }
        };

        configureOptions?.Invoke(ws.Options);

        await ws.ConnectAsync(Uri, token).ConfigureAwait(false);

        WebSocketClientsideRemoteRpcConnection remote = new WebSocketClientsideRemoteRpcConnection(this, ws);

        return new WebSocketClientsideLocalRpcConnection(router, remote);
    }
}
