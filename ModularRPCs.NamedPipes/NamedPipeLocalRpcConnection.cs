using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Data;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The base class for a local connection over a named pipe.
/// </summary>
/// <typeparam name="TSelf">The deriving type.</typeparam>
/// <typeparam name="TPipeStream">The pipe stream type.</typeparam>
public abstract class NamedPipeLocalRpcConnection<TSelf, TPipeStream> : IRefSafeLoggable, IModularRpcLocalConnection, IContiguousBufferProgressUpdateDispatcher
    where TSelf : NamedPipeLocalRpcConnection<TSelf, TPipeStream>
    where TPipeStream : PipeStream
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private object? _logger;
    private ConcurrentDictionary<string, object>? _tags;
    private readonly ContiguousBuffer _buffer;
    private readonly AsyncCallback _readCompletedCallback;
    private readonly ContiguousBufferCallback _processBufferCallback;
    private bool _isListening;

    /// <inheritdoc />
    public event ContiguousBufferProgressUpdate? BufferProgressUpdated
    {
        add => _buffer.BufferProgressUpdated += value;
        remove => _buffer.BufferProgressUpdated -= value;
    }

    /// <inheritdoc cref="IModularRpcLocalConnection.Remote" />
    public NamedPipeRemoteRpcConnection<TSelf, TPipeStream> Remote { get; }

    /// <inheritdoc />
    public bool IsClosed => Remote.IsClosed;

    /// <inheritdoc />
    public IRpcRouter Router { get; }

    /// <summary>
    /// This local connection's assigned <see cref="IRpcSerializer"/> implementation used to read messages.
    /// </summary>
    public IRpcSerializer Serializer { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Tags
    {
        get
        {
            if (_tags == null)
            {
                Interlocked.CompareExchange(ref _tags, new ConcurrentDictionary<string, object>(), null);
            }

            return _tags!;
        }
    }

    private protected NamedPipeLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, NamedPipeRemoteRpcConnection<TSelf, TPipeStream> remote, CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        Router = router;
        Remote = remote;
        Remote.Local = (TSelf)this;
        Serializer = serializer;
        _buffer = new ContiguousBuffer(this, remote.Endpoint.LocalBufferSize);
        _readCompletedCallback = ReadCompleted;
        _processBufferCallback = ProcessBufferMessageHandler;
    }

    internal void StartListening()
    {
        lock (_buffer)
        {
            if (_isListening)
                return;
            _isListening = true;

            TPipeStream? pipeStream = Remote.PipeStream;
            if (pipeStream == null)
            {
                return;
            }

            try
            {
                pipeStream.BeginRead(_buffer.Buffer, 0, _buffer.Buffer.Length, _readCompletedCallback, pipeStream);
            }
            catch (ObjectDisposedException)
            {
                TryStartAutoReconnecting();
            }
        }
    }

    private protected virtual void TryStartAutoReconnecting() { }

    private void ReadCompleted(IAsyncResult result)
    {
        lock (_buffer)
        {
            _isListening = false;

            TPipeStream pipeStream = (TPipeStream)result.AsyncState;

            int bytesRead = 0;
            try
            {
                bytesRead = pipeStream.EndRead(result);
            }
            catch (ObjectDisposedException)
            {
                bytesRead = 0;
            }
            catch (Exception ex)
            {
                this.LogError(ex, Properties.Resources.LogErrorReadingFromPipeStream);
            }

            if (!ReferenceEquals(Remote.PipeStream, pipeStream))
                return;

            if (bytesRead <= 0)
            {
                TryStartAutoReconnecting();
                return;
            }

            try
            {
                _buffer.ProcessBuffer((uint)bytesRead, Serializer, _processBufferCallback);
            }
            catch (Exception ex)
            {
                this.LogError(ex, Properties.Resources.LogErrorReadingFromPipeStream);
            }
            finally
            {
                StartListening();
            }
        }
    }

    private void ProcessBufferMessageHandler(ReadOnlyMemory<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead)
    {
        CancellationToken token;
        try
        {
            token = _cancellationTokenSource.Token;
        }
        catch (ObjectDisposedException)
        {
            throw new OperationCanceledException();
        }

        ValueTask vt = Router.ReceiveData(in overhead, ((IModularRpcLocalConnection)this).Remote, Serializer, data, canTakeOwnership, token);

        if (vt.IsCompleted)
            return;

        ValueTask vt2 = vt;
        Task.Run(async () =>
        {
            try
            {
                await vt2.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Failed to execute rpc read callback.");
            }
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public ValueTask CloseAsync(CancellationToken token = default)
    {
        return Remote.CloseAsync(token);
    }

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;
}
