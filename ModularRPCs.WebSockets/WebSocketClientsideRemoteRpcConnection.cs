using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;
public class WebSocketClientsideRemoteRpcConnection : IModularRpcClientsideConnection, IModularRpcRemoteConnection
{
    internal readonly ClientWebSocket WebSocket;
    internal readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private readonly int _bufferSize;
    private byte[]? _buffer;
    private int _disp;
    public bool IsClosed { get; private set; }
    public WebSocketEndpoint Endpoint { get; }
    public WebSocketClientsideLocalRpcConnection Local { get; internal set; } = null!;
    internal WebSocketClientsideRemoteRpcConnection(WebSocketEndpoint endpoint, ClientWebSocket webSocket, int bufferSize = 4096)
    {
        _bufferSize = bufferSize;
        WebSocket = webSocket;
        Endpoint = endpoint;
        IsClosed = webSocket.State != WebSocketState.Open;
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        byte[] arr = rawData.ToArray();

        Task task = SendDataArrayIntl(new ArraySegment<byte>(arr), token);
        return new ValueTask(task);
    }
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        Task task = SendDataStreamIntl(streamData, token);
        return new ValueTask(task);
    }
    private async Task SendDataArrayIntl(ArraySegment<byte> arr, CancellationToken token)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WebSocket.SendAsync(arr, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }
    internal async Task Reconnect(CancellationToken token = default)
    {
        try
        {
            if (WebSocket.State is not WebSocketState.Closed and not WebSocketState.CloseReceived and not WebSocketState.CloseSent)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Reconnecting", token).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
        }

        await WebSocket.ConnectAsync(Endpoint.Uri, token).ConfigureAwait(false);
    }
    private async Task SendDataStreamIntl(Stream stream, CancellationToken token)
    {
        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            _buffer ??= new byte[_bufferSize];
            bool hasSentOnce = false;
            bool hasEndByte = false;
            while (true)
            {
                int ctToRead = hasEndByte ? _buffer.Length - 1 : _buffer.Length;
                int byteCt = await stream.ReadAsync(_buffer, hasEndByte ? 1 : 0, ctToRead, token).ConfigureAwait(false);
                if (byteCt == 0)
                {
                    if (!hasSentOnce)
                        throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);
                    
                    if (hasEndByte)
                        await WebSocket.SendAsync(new ArraySegment<byte>(_buffer, 0, 1), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                    
                    break;
                }

                if (byteCt == ctToRead)
                {
                    --byteCt;
                    hasEndByte = true;
                }
                else hasEndByte = false;

                ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, 0, byteCt + (hasEndByte ? 1 : 0));

                await WebSocket.SendAsync(segment, WebSocketMessageType.Binary, !hasEndByte && byteCt < ctToRead, token).ConfigureAwait(false);
                hasSentOnce = true;

                if (hasEndByte)
                {
                    // ReSharper disable once UseIndexFromEndExpression
                    _buffer[0] = _buffer[_buffer.Length - 1];
                }
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
    public async ValueTask DisposeAsync()
    {
        await Semaphore.WaitAsync(10000);
        try
        {
            if (Interlocked.Exchange(ref _disp, 1) != 0)
                return;
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
        finally
        {
            Semaphore.Release();
        }
    }
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        return DisposeAsync();
    }
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
}
