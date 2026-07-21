using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Connections;

namespace DanielWillett.ModularRpcs.WebSockets;

public partial class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    /// <inheritdoc />
    public override IListener Listener => this;

    /// <inheritdoc />
    public override ConnectionState State => TODO_IMPLEMENT_ME;

    /// <inheritdoc />
    internal WebSocketClientConnection(WebSocketConnectionFactory factory) : base(factory)
    {

    }

    /// <inheritdoc />
    public override RpcConnectionRelationship EndpointRelationship => TODO_IMPLEMENT_ME;

    /// <inheritdoc />
    public override ValueTask DisconnectAsync(CancellationToken token = default) => TODO_IMPLEMENT_ME;

    /// <inheritdoc />
    public override ValueTask ReconnectAsync(CancellationToken token = default) => TODO_IMPLEMENT_ME;

    /// <inheritdoc />
    public override string ToString() => TODO_IMPLEMENT_ME;
}
