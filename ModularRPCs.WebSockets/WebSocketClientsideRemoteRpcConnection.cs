using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Exceptions;

namespace DanielWillett.ModularRpcs.WebSockets;
internal class WebSocketClientsideRemoteRpcConnection : IModularRpcClientsideConnection, IModularRpcRemoteConnection
{
    internal readonly ClientWebSocket WebSocket;
    private readonly byte[] _buffer = new byte[4096];
    public bool IsClosed { get; private set; }
    public WebSocketEndpoint EndPoint { get; }
    public WebSocketClientsideLocalRpcConnection Local { get; internal set; } = null!;
    public WebSocketClientsideRemoteRpcConnection(WebSocketEndpoint endPoint, ClientWebSocket webSocket)
    {
        WebSocket = webSocket;
        EndPoint = endPoint;
        IsClosed = webSocket.State != WebSocketState.Open;
    }
    public async ValueTask SendDataAsync(Stream? streamData, ArraySegment<byte> rawData, CancellationToken token = default)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Count > 0)
        {
            await WebSocket.SendAsync(rawData, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
        }
        else if (streamData != null)
        {
            int index = 0;
            while (true)
            {
                int rdCt = index + await streamData.ReadAsync(_buffer, index, _buffer.Length - index, token).ConfigureAwait(false);
                if (rdCt == 0)
                    break;
                
                if (rdCt < _buffer.Length)
                {
                    await WebSocket.SendAsync(_buffer[..rdCt], WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                    break;
                }

                int newCt = _buffer.Length / 2;

                await WebSocket.SendAsync(_buffer[..newCt], WebSocketMessageType.Binary, false, token).ConfigureAwait(false);
                int dataLen = _buffer.Length - newCt;
                Buffer.BlockCopy(_buffer, newCt, _buffer, 0, dataLen);
                index = dataLen;
            }
        }
        else throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);
    }
    public async ValueTask DisposeAsync()
    {
        Local.DisposeIntl();
        IsClosed = true;
        if (WebSocket.State == WebSocketState.Open)
        {
            try
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        }
        WebSocket.Dispose();
    }
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        Local.DisposeIntl();
        IsClosed = true;
        return new ValueTask(WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token));
    }
    IModularRpcRemoteEndPoint IModularRpcRemoteConnection.EndPoint => EndPoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
}
