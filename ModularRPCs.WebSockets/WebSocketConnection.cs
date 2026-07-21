using DanielWillett.ModularRpcs.Connections;
using System.Net.WebSockets;
using System.Threading;
using DanielWillett.ModularRpcs.Data;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Shared functionality between serverside and clientside web socket connections.
/// </summary>
/// <typeparam name="TWebSocket">The type of websocket to store. Either <see cref="System.Net.WebSockets.WebSocket"/> or <see cref="System.Net.WebSockets.ClientWebSocket"/>.</typeparam>
public abstract partial class WebSocketConnection<TWebSocket> : RpcConnection, ITransmitter, IListener, IMessageBufferHandler
    where TWebSocket : WebSocket
{
    private protected TWebSocket WebSocket;

    /// <inheritdoc />
    public override ITransmitter Transmitter => this;

    /// <inheritdoc />
    public override IListener Listener => this;

    /// <inheritdoc />
    public override bool IsLoopback => false;

    private protected WebSocketConnection(TWebSocket webSocket, WebSocketConnectionFactory factory, int bufferSize) : base(factory)
    {
        WebSocket = webSocket;
        Semaphore = new SemaphoreSlim(1, 1);

        _bufferSize = bufferSize;

        Buffer = new MessageBuffer(this, bufferSize);
    }

    /// <inheritdoc />
    public void HandleMessage()
    {
        
    }
}