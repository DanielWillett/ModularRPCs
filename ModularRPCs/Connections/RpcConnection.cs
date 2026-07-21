using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Connections;

/// <summary>
/// Default base class for most implementations of <see cref="IRpcConnection"/>.
/// </summary>
public abstract class RpcConnection : IRpcConnection
{
    private object? _logger;
    protected SemaphoreSlim? Semaphore;

    private ConcurrentDictionary<string, object>? _tags;

    /// <inheritdoc />
    public abstract bool IsLoopback { get; }

    /// <inheritdoc />
    public abstract ConnectionState State { get; }

    /// <inheritdoc />
    public abstract IListener Listener { get; }

    /// <inheritdoc />
    public abstract ITransmitter Transmitter { get; }

    /// <inheritdoc />
    public IConnectionFactory Factory { get; }

    /// <inheritdoc />
    public abstract RpcConnectionRelationship EndpointRelationship { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Tags
    {
        get
        {
            if (_tags != null)
                return _tags;

            ConcurrentDictionary<string, object> newDict = new ConcurrentDictionary<string, object>();
            return Interlocked.Exchange(ref _tags, newDict) ?? newDict;
        }
    }

    private protected RpcConnection(IConnectionFactory factory)
    {
        Factory = factory;
    }

    protected virtual void DisposeIntl() { }

    /// <inheritdoc />
    public abstract ValueTask DisconnectAsync(CancellationToken token = default);

    /// <inheritdoc />
    public abstract ValueTask ReconnectAsync(CancellationToken token = default);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        SemaphoreSlim? semaphore = Interlocked.Exchange(ref Semaphore, null);
        if (semaphore != null)
        {
            await semaphore.WaitAsync();
        }

        try
        {
            IListener listener = Listener;
            ITransmitter transmitter = Transmitter;

            DisposeIntl();

            if (!ReferenceEquals(listener, this))
            {
                if (listener is IAsyncDisposable listenerAsyncDisposable)
                {
                    await listenerAsyncDisposable.DisposeAsync();
                }
                else if (listener is IDisposable listenerDisposable)
                {
                    listenerDisposable.Dispose();
                }
            }

            if (!ReferenceEquals(listener, transmitter) && !ReferenceEquals(transmitter, this))
            {
                if (transmitter is IAsyncDisposable transmitterAsyncDisposable)
                {
                    await transmitterAsyncDisposable.DisposeAsync();
                }
                else if (transmitter is IDisposable transmitterDisposable)
                {
                    transmitterDisposable.Dispose();
                }
            }
        }
        finally
        {
            semaphore?.Release();
            semaphore?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SemaphoreSlim? semaphore = Interlocked.Exchange(ref Semaphore, null);

        bool waited = false;
        if (semaphore != null)
        {
            waited = semaphore.Wait(2000);
        }

        try
        {
            IListener listener = Listener;
            ITransmitter transmitter = Transmitter;

            DisposeIntl();

            if (!ReferenceEquals(listener, this) && listener is IDisposable listenerDisposable)
            {
                listenerDisposable.Dispose();
            }

            if (!ReferenceEquals(listener, transmitter)
                && !ReferenceEquals(transmitter, this)
                && transmitter is IDisposable transmitterDisposable)
            {
                transmitterDisposable.Dispose();
            }
        }
        finally
        {
            if (waited)
            {
                semaphore?.Release();
            }
            semaphore?.Dispose();
        }
    }

    /// <inheritdoc />
    public abstract override string ToString();

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
}