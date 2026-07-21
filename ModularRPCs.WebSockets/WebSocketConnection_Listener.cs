using DanielWillett.ModularRpcs.Connections;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Data;

namespace DanielWillett.ModularRpcs.WebSockets;

partial class WebSocketConnection<TWebSocket>
{
    private int _isListening;
    private CancellationTokenSource? _listenCancelToken;

    private MessageBuffer Buffer;

    /// <inheritdoc cref="IListener.State" />
    public ListenerState ListenerState
    {
        get => (ListenerState)_isListening;
        set
        {
            if ((ListenerState)_isListening == value)
                return;

            if (value == ListenerState.Listening)
            {
                StartListening();
            }
            else
            {
                StopListening();
            }
        }
    }

    private void StartListening()
    {
        if (Interlocked.Exchange(ref _isListening, 1) != 0)
        {
            return;
        }

        Task.Factory.StartNew(ListenTask, TaskCreationOptions.LongRunning);
    }

    private async Task ListenTask()
    {
        CancellationTokenSource? cts = _listenCancelToken;
        if (cts == null || cts.IsCancellationRequested)
        {
            CancellationTokenSource newCts = new CancellationTokenSource();
            CancellationTokenSource? oldCts = Interlocked.Exchange(ref _listenCancelToken, newCts);
            if (oldCts != null)
            {
                if (oldCts.IsCancellationRequested)
                {
                    oldCts.Dispose();
                    cts = newCts;
                }
                else
                {
                    cts = oldCts;
                }
            }
            else
                cts = newCts;

            if (cts == oldCts)
            {
                newCts.Dispose();
            }
        }

        bool doClose = false;
        while (!cts.IsCancellationRequested)
        {
            if (!await CheckConnected().ConfigureAwait(false))
            {
                break;
            }

            try
            {
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(Buffer.Buffer, cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    
                }

                Buffer.HandleIncomingData(result.Count, result.EndOfMessage);
            }
            catch (Exception e)
            {
                this.LogError(e, /* todo */"todo");
            }
        }

        _isListening = 0;

        if (!doClose)
            return;

        try
        {
            await DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogWarning(ex, "Error disposing connection after a fault.");
        }
    }

    private protected virtual ValueTask<bool> CheckConnected()
    {
        // overridden by the client connection to attempt to reconnect
        return new ValueTask<bool>(WebSocket.State == WebSocketState.Open);
    }

    private void StopListening()
    {
        if (Interlocked.Exchange(ref _isListening, 0) == 0)
        {
            return;
        }

        CancellationTokenSource? cancellationToken = Interlocked.Exchange(ref _listenCancelToken, null);

        if (cancellationToken != null)
        {
            cancellationToken.Cancel();
        }
        cancellationToken?.Dispose();
    }

    /// <inheritdoc />
    public IDisposable ReportProgress(IProgress<DownloadProgressReport> progressSink)
    {

    }

    /// <inheritdoc />
    public void HandleIncomingMessage()
    {

    }

    ListenerState IListener.State
    {
        get => ListenerState;
        set => ListenerState = value;
    }
}