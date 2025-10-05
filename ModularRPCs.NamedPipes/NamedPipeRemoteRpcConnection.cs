using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.NamedPipes;

/// <summary>
/// The base class for a remote connection over a named pipe.
/// </summary>
/// <typeparam name="TLocalConnection">The local connection type.</typeparam>
/// <typeparam name="TPipeStream">The pipe stream type.</typeparam>
public abstract class NamedPipeRemoteRpcConnection<TLocalConnection, TPipeStream> : IModularRpcRemoteConnection, IDisposable
    where TLocalConnection : NamedPipeLocalRpcConnection<TLocalConnection, TPipeStream>, IModularRpcLocalConnection
    where TPipeStream : PipeStream
{
    internal TPipeStream? PipeStream;
    private protected readonly SemaphoreSlim Semaphore;

#nullable disable
    /// <inheritdoc cref="IModularRpcRemoteConnection.Local" />
    public TLocalConnection Local { get; internal set; }
#nullable restore

    /// <inheritdoc cref="IModularRpcRemoteConnection.Endpoint" />
    public NamedPipeEndpoint Endpoint { get; }

    /// <inheritdoc />
    public bool IsClosed => PipeStream is not { IsConnected: true };

    private protected NamedPipeRemoteRpcConnection(NamedPipeEndpoint endpoint)
    {
        Endpoint = endpoint;
        Semaphore = new SemaphoreSlim(1, 1);
    }

    internal virtual void TryStartAutoReconnecting() { }

    /// <inheritdoc />
    public async ValueTask SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            AssertAbleToWrite(out TPipeStream pipeStream);
            try
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                await streamData.CopyToAsync(pipeStream, token).ConfigureAwait(false);
#else
                await streamData.CopyToAsync(pipeStream).ConfigureAwait(false);
#endif
            }
            catch (ObjectDisposedException)
            {
                TryStartAutoReconnecting();
            }
            catch (Exception ex)
            {
                Local.LogError(ex, Properties.Resources.LogErrorWritingToPipeStream);
                TryStartAutoReconnecting();
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (!canTakeOwnership)
        {
            rawData = rawData.ToArray();
        }

        await Semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await SendDataIntl(rawData, token).ConfigureAwait(false);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        return new ValueTask(WaitAndSendAsync(rawData.ToArray(), token));

        async Task WaitAndSendAsync(byte[] rawData, CancellationToken token)
        {
            await Semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await SendDataIntl(rawData.AsMemory(), token).ConfigureAwait(false);
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }

    private void AssertAbleToWrite(out TPipeStream pipeStream)
    {
        pipeStream = PipeStream!;
        if (pipeStream is not { IsConnected: true })
            throw new RpcException(DanielWillett.ModularRpcs.Properties.Exceptions.RpcConnectionClosedException);
    }

    private async Task SendDataIntl(ReadOnlyMemory<byte> memory, CancellationToken token)
    {
        AssertAbleToWrite(out TPipeStream pipeStream);
        try
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            await pipeStream.WriteAsync(memory, token).ConfigureAwait(false);
#else
            if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> array))
            {
                await pipeStream.WriteAsync(array.Array!, array.Offset, array.Count, token).ConfigureAwait(false);
            }
            else
            {
                byte[] copy = memory.ToArray();
                await pipeStream.WriteAsync(copy, 0, copy.Length, token).ConfigureAwait(false);
            }
#endif
        }
        catch (ObjectDisposedException)
        {
            TryStartAutoReconnecting();
        }
        catch (Exception ex)
        {
            Local.LogError(ex, Properties.Resources.LogErrorWritingToPipeStream);
            TryStartAutoReconnecting();
        }
    }

    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    bool IModularRpcRemoteConnection.IsLoopback => false;

    /// <inheritdoc />
    public
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        async
#endif
        ValueTask CloseAsync(CancellationToken token = default)
    {
        TPipeStream? stream = Interlocked.Exchange(ref PipeStream, null);
        if (stream is NamedPipeServerStream server)
        {
            try
            {
                server.Disconnect();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
                goto rtn;
            }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Local.LogWarning(ex, Properties.Resources.LogWarningDisconnectingPipeStream);
            }
        }

        if (stream == null)
            goto rtn;

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        await stream.DisposeAsync().ConfigureAwait(false);
        rtn: return;
#else
        stream.Dispose();
        rtn: return default;
#endif
    }

    ~NamedPipeRemoteRpcConnection()
    {
        Dispose(false);
    }

    private protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        TPipeStream? pipeStream = Interlocked.Exchange(ref PipeStream, null);
        pipeStream?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }
}
