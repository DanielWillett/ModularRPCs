using DanielWillett.ModularRpcs.Connections;
using DanielWillett.ModularRpcs.Exceptions;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

partial class WebSocketConnection<TWebSocket>
{
    private byte[]? _buffer;
    private readonly int _bufferSize;

    /// <inheritdoc />
    public ValueTask SendDataAsync(
        ReadOnlySpan<byte> rawData,
        IProgress<long>? progressReport = null,
        CancellationToken token = default)
    {
        if (State != ConnectionState.Connected)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(ModularRpcs.Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        byte[] arr = rawData.ToArray();

        Task task = SendDataArrayIntl(new ArraySegment<byte>(arr), token);
        return new ValueTask(task);
    }

    /// <inheritdoc />
    public ValueTask SendDataAsync(
        ReadOnlyMemory<byte> rawData,
        bool canTakeOwnership,
        IProgress<long>? progressReport = null,
        CancellationToken token = default)
    {
        if (State != ConnectionState.Connected)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(ModularRpcs.Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        if (!canTakeOwnership || !MemoryMarshal.TryGetArray(rawData, out ArraySegment<byte> arraySegment))
        {
            arraySegment = new ArraySegment<byte>(rawData.ToArray());
        }

        return new ValueTask(SendDataArrayIntl(arraySegment, token));
    }

    /// <inheritdoc />
    public ValueTask SendDataAsync(
        Stream streamData,
        IProgress<long>? progressReport = null,
        CancellationToken token = default
    )
    {
        if (State != ConnectionState.Connected)
            throw new RpcConnectionClosedException();

        Task task = SendDataStreamIntl(streamData, token);
        return new ValueTask(task);
    }

    private async Task SendDataArrayIntl(ArraySegment<byte> arr, CancellationToken token)
    {
        await Semaphore!.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await WebSocket.SendAsync(arr, WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task SendDataStreamIntl(Stream stream, CancellationToken token)
    {
        await Semaphore!.WaitAsync(token).ConfigureAwait(false);
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
                        throw new InvalidOperationException(ModularRpcs.Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

                    if (hasEndByte)
                        await WebSocket.SendAsync(new ArraySegment<byte>(_buffer, 0, 1), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);

                    break;
                }

                bool saveLastByte = byteCt == ctToRead;

                ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, 0, byteCt - (saveLastByte ? 1 : 0) + (hasEndByte ? 1 : 0));

                bool isEnd = !saveLastByte && byteCt < ctToRead;
                await WebSocket.SendAsync(segment, WebSocketMessageType.Binary, !hasEndByte && byteCt < ctToRead, token).ConfigureAwait(false);

                hasSentOnce = true;

                if (saveLastByte)
                {
                    // ReSharper disable once UseIndexFromEndExpression (net461)
                    _buffer[0] = _buffer[_buffer.Length - 1];
                    hasEndByte = true;
                }

                if (isEnd)
                    break;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}