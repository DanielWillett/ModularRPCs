using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.WebSockets;

/// <summary>
/// Local side of a connection to the server hosted on this machine.
/// </summary>
/// <remarks>Can be awaited to wait on the connection to close (useful for ASP.NET).</remarks>
public class WebSocketServersideLocalRpcConnection : WebSocketLocalRpcConnection, IModularRpcAuthoritativeParentConnection
{
    private bool _isClosed;
    private readonly WebSocketServersideLocalRpcConnectionAwaiter _awaiter;
    public WebSocketServersideRemoteRpcConnection Remote { get; internal set; } = null!;
    public override bool IsClosed => _isClosed;
    protected internal override bool CanReconnect => false;
    protected internal override WebSocket WebSocket => Remote.WebSocketIntl;
    protected internal override SemaphoreSlim Semaphore => Remote.Semaphore;
    internal WebSocketServersideLocalRpcConnection(IRpcRouter router, IRpcSerializer serializer, WebSocketEndpoint endpoint, int bufferSize)
        : base(router, serializer, endpoint, bufferSize, false, default)
    {
        _awaiter = new WebSocketServersideLocalRpcConnectionAwaiter(this);
        _isClosed = true;
    }
    public Task InitializeConnectionAsync(CancellationToken token = default)
    {
        Remote.IsClosed = false;
        _isClosed = false;
        TryStartListening();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get an awaiter that completes when the connection closes.
    /// </summary>
    public WebSocketServersideLocalRpcConnectionAwaiter GetAwaiter() => _awaiter;

    /// <summary>
    /// Force the underlying connection to reconnect. Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override Task Reconnect(CancellationToken token = default)
    {
        throw new NotSupportedException();
    }

    public override ValueTask DisposeAsync()
    {
        return CloseAsync();
    }
    public override async ValueTask CloseAsync(CancellationToken token = default)
    {
        try
        {
            await Remote.CloseAsync(token);
        }
        finally
        {
            _awaiter.Complete();
        }
    }
    IModularRpcRemoteConnection IModularRpcLocalConnection.Remote => Remote;

    public class WebSocketServersideLocalRpcConnectionAwaiter : ICriticalNotifyCompletion
    {
        private object? _continuations;
        public WebSocketServersideLocalRpcConnection Connection { get; }
        public WebSocketServersideLocalRpcConnectionAwaiter(WebSocketServersideLocalRpcConnection instance)
        {
            Connection = instance;
            _continuations = null;
        }
        public bool IsCompleted { get; internal set; }
        public void GetResult()
        {
            if (!IsCompleted)
                throw new RpcGetResultUsageException();
        }
        internal void Complete()
        {
            IsCompleted = true;
            object? continuations = _continuations;
            switch (continuations)
            {
                case null:
                    return;
                
                case Action continuation:
                    continuation();
                    return;

                case ConcurrentBag<Action> bag:
                    List<Exception>? exceptions = null;
                    while (bag.TryTake(out Action? continuation))
                    {
                        try
                        {
                            continuation();
                        }
                        catch (Exception ex)
                        {
                            (exceptions ??= []).Add(ex);
                        }
                    }

                    if (exceptions is { Count: > 0 })
                        throw new AggregateException(exceptions);

                    return;
            }
        }
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => ((INotifyCompletion)this).OnCompleted(continuation);
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation();
                return;
            }

            object? originalValue = Interlocked.CompareExchange(ref _continuations, continuation, null);
            if (originalValue == null)
                return;
            
            if (originalValue is ConcurrentBag<Action> bag)
            {
                bag.Add(continuation);
                return;
            }

            if (originalValue is not Action oldContinuation)
                return;

            bag = [ continuation, oldContinuation ];
            object oldValue = Interlocked.Exchange(ref _continuations, bag);
            if (ReferenceEquals(oldContinuation, continuation))
                return;

            if (oldValue is ConcurrentBag<Action> newBag)
            {
                while (newBag.TryTake(out Action? newAction))
                {
                    if (!ReferenceEquals(oldContinuation, newAction))
                        bag.Add(newAction);
                }

                return;
            }

            if (oldValue is not Action oldCont2)
                return;

            bag.Add(oldCont2);
        }
    }
}