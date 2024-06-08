using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Base class for the sending portion of a <see cref="WebSocket"/> connection.
/// </summary>
public abstract class WebSocketRemoteRpcConnection<TLocalConnection> : IModularRpcRemoteConnection where TLocalConnection : WebSocketLocalRpcConnection, IModularRpcLocalConnection
{
    internal WebSocket WebSocketIntl;
    internal SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private readonly int _bufferSize;
    private byte[]? _buffer;
    public WebSocketEndpoint Endpoint { get; }
    public abstract bool IsClosed { get; }
    public TLocalConnection Local { get; internal set; } = null!;
    public IRpcConnectionLifetime Lifetime { get; }
    protected internal WebSocketRemoteRpcConnection(WebSocket webSocket, WebSocketEndpoint endpoint, IRpcConnectionLifetime lifetime, int bufferSize)
    {
        _bufferSize = bufferSize;
        Lifetime = lifetime;
        WebSocketIntl = webSocket;
        Endpoint = endpoint;
    }
    public ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        byte[] arr = rawData.ToArray();

        Task task = SendDataArrayIntl(new ArraySegment<byte>(arr), token);
        return new ValueTask(task);
    }
    public ValueTask SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
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
            await WebSocketIntl.SendAsync(arr, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
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
                        await WebSocketIntl.SendAsync(new ArraySegment<byte>(_buffer, 0, 1), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);

                    break;
                }

                if (byteCt == ctToRead)
                {
                    --byteCt;
                    hasEndByte = true;
                }
                else hasEndByte = false;

                ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, 0, byteCt + (hasEndByte ? 1 : 0));

                await WebSocketIntl.SendAsync(segment, WebSocketMessageType.Binary, !hasEndByte && byteCt < ctToRead, token).ConfigureAwait(false);
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

    public abstract ValueTask CloseAsync(CancellationToken token = default);
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
}
