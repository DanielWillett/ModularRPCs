using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Data;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Loopback;
public class LoopbackRpcServersideRemoteConnection : IModularRpcRemoteConnection, IModularRpcServersideConnection, IRefSafeLoggable
{
    private ContiguousBuffer? _buffer;
    private readonly ContiguousBufferCallback _callback;
    private object? _logger;
    
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }
    bool IModularRpcRemoteConnection.IsLoopback => AdvertiseLoopback;

    public LoopbackRpcServersideLocalConnection Local { get; internal set; }
    public LoopbackRpcClientsideRemoteConnection Client { get; internal set; } = null!;
    public LoopbackEndpoint Endpoint { get; }
    public bool IsClosed { get; internal set; }
    public IRpcConnectionLifetime? Lifetime { get; }
    public bool UseStreams { get; }
    public bool UseContiguousBuffer { get; set; }
    public bool AdvertiseLoopback { get; }
    internal LoopbackRpcServersideRemoteConnection(LoopbackEndpoint endPoint, IRpcRouter router, IRpcSerializer serializer, IRpcConnectionLifetime? lifetime, bool useStreams, bool advertiseLoopback = true)
    {
        if (!endPoint.IsServer)
            throw new ArgumentException(Properties.Exceptions.LoopbackRemoteConnectionExpectedServersideEndpoint, nameof(endPoint));
        Lifetime = lifetime;
        UseStreams = useStreams;
        IsClosed = true;
        Endpoint = endPoint;
        Local = new LoopbackRpcServersideLocalConnection(this, router, serializer);
        AdvertiseLoopback = advertiseLoopback;
        _callback = HandleContiguousBufferCallback;
    }

    IModularRpcRemoteEndpoint IModularRpcRemoteConnection.Endpoint => Endpoint;
    IModularRpcLocalConnection IModularRpcRemoteConnection.Local => Local;
    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        if (!UseContiguousBuffer && !UseStreams && canTakeOwnership)
        {
            return Client.Local.Router.ReceiveData(Client, Client.Local.Serializer, rawData, true, token);
        }

        return SendDataIntl(serializer, rawData.Span, token);
    }

    ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (rawData.Length <= 0)
            throw new InvalidOperationException(Properties.Exceptions.DidNotPassAnyDataToRpcSendDataAsync);

        return SendDataIntl(serializer, rawData, token);
    }

    private ValueTask SendDataIntl(IRpcSerializer serializer, ReadOnlySpan<byte> rawData, CancellationToken token)
    {
        if (UseContiguousBuffer && _buffer == null)
        {
            Interlocked.CompareExchange(ref _buffer, new ContiguousBuffer(Client.Local, 4096), null);
        }

        byte[] rtnBuffer = new byte[rawData.Length];
        rawData.CopyTo(rtnBuffer);

        if (!UseStreams)
        {
            if (!UseContiguousBuffer)
            {
                return Client.Local.Router.ReceiveData(Client, Client.Local.Serializer, rtnBuffer, true, token);
            }

            int bytesLeft = rtnBuffer.Length;
            while (bytesLeft > 0)
            {
                int numBytes = Math.Min(bytesLeft, _buffer!.Buffer.Length);
                Buffer.BlockCopy(rtnBuffer, 0, _buffer!.Buffer, 0, numBytes);
                _buffer.ProcessBuffer((uint)numBytes, serializer, _callback);
                bytesLeft -= numBytes;
            }

            return default;
        }

        using MemoryStream mem = new MemoryStream(rtnBuffer, false);

        if (!UseContiguousBuffer)
        {
            return Client.Local.Router.ReceiveData(Client, Client.Local.Serializer, mem, token);
        }

        while (true)
        {
            int read = mem.Read(_buffer!.Buffer, 0, _buffer.Buffer.Length);
            if (read == 0)
                break;

            _buffer.ProcessBuffer((uint)read, serializer, _callback);
        }

        return default;
    }

    async ValueTask IModularRpcRemoteConnection.SendDataAsync(IRpcSerializer serializer, Stream streamData, CancellationToken token)
    {
        if (IsClosed)
            throw new RpcConnectionClosedException();

        if (!UseContiguousBuffer)
        {
            await Client.Local.Router.ReceiveData(Client, Client.Local.Serializer, streamData, token).ConfigureAwait(false);
            return;
        }

        if (_buffer == null)
        {
            Interlocked.CompareExchange(ref _buffer, new ContiguousBuffer(Client.Local, 4096), null);
        }

        while (true)
        {
            int read = await streamData.ReadAsync(_buffer!.Buffer, 0, _buffer.Buffer.Length, token).ConfigureAwait(false);
            if (read == 0)
                break;

            _buffer.ProcessBuffer((uint)read, serializer, _callback);
        }
    }

    private void HandleContiguousBufferCallback(
        ReadOnlyMemory<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead)
    {
        ValueTask vt = Client.Local.Router.ReceiveData(in overhead, Client, Client.Local.Serializer, data, canTakeOwnership);

        if (vt.IsCompleted)
            return;

        Task.Run(async () =>
        {
            try
            {
                await vt.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Failed to execute rpc read callback.");
            }
        });
    }

    public ValueTask DisposeAsync() => CloseAsync();
    public async ValueTask CloseAsync(CancellationToken token = default)
    {
        IsClosed = true;
        Local.IsClosed = true;
        Client.IsClosed = true;
        Client.Local.IsClosed = true;
        Local.Router.CleanupConnection(this);
        if (Lifetime != null)
        {
            await Lifetime.TryRemoveConnection(this, CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public override string ToString() => "Loopback (Remote, Server)";
}