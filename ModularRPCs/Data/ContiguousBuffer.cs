using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Data;

/// <summary>
/// Allows reading messages when their payloads could be sequentially read into the same buffer (like from a <see cref="NetworkStream"/>).
/// </summary>
public sealed class ContiguousBuffer
{
    private int _disposed;
    private byte[]? _pendingData;
    private uint _pendingLength;

    /// <summary>
    /// Set using <see cref="LoggingExtensions.SetLogger(ContiguousBuffer, ILogger)"/>. 
    /// </summary>
    internal object? Logger;

    /// <summary>
    /// Max allowed message size in bytes, defaults to 128 MiB.
    /// </summary>
    public int MaxMessageSize { get; set; } = 134217728;

    /// <summary>
    /// Number of bytes needed to trigger manual garbage collection after message cleanup, defaults to 16 KB.
    /// </summary>
    public int GCTriggerSizeThreshold { get; set; } = 16384;

    /// <summary>
    /// The buffer to read data to before calling <see cref="ProcessBuffer"/>.
    /// </summary>
    public byte[] Buffer { get; private set; }

    /// <summary>
    /// The currently pending message, if any.
    /// </summary>
    public RpcOverhead? PendingOverhead { get; private set; }

    /// <summary>
    /// If there is a currently pending message, meaning a message that is in progress of being received.
    /// </summary>
    public bool HasPendingMessage => PendingOverhead != null;

    /// <summary>
    /// The connection that created this <see cref="ContiguousBuffer"/>.
    /// </summary>
    public IModularRpcLocalConnection Connection { get; }

    /// <summary>
    /// Invoked when a large download has a buffer progress update. Used to track large downloads.
    /// </summary>
    /// <remarks>Not all implementations of <see cref="IModularRpcConnection"/> will support this.</remarks>
    public event ContiguousBufferProgressUpdate? BufferProgressUpdated;

    public ContiguousBuffer(IModularRpcLocalConnection connection, int bufferSize) : this(connection, new byte[bufferSize]) { }
    public ContiguousBuffer(IModularRpcLocalConnection connection, byte[] buffer)
    {
        Buffer = buffer;
        Connection = connection;
    }

    /// <summary>
    /// Process <paramref name="amtReceived"/> bytes that should've already been read into <see cref="Buffer"/>.
    /// </summary>
    /// <param name="amtReceived">Number of bytes read into <see cref="Buffer"/>.</param>
    /// <param name="callback">Synchronous function to call for each RPC call.</param>
    /// <param name="offset">Offset of data read into <see cref="Buffer"/>, usually 0.</param>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="ContiguousBufferParseException">Any other error occurs while separating messages.</exception>
    public unsafe void ProcessBuffer(uint amtReceived, IRpcSerializer serializer, Action<Memory<byte>, RpcOverhead> callback, uint offset = 0)
    {
        if (_disposed != 0)
            throw new ObjectDisposedException(nameof(ContiguousBuffer));
        ContiguousBufferParseException? error = null;
        lock (Buffer)
        {
            while (true)
            {
                try
                {
                    bool hadMuchData;
                    fixed (byte* bytes = &Buffer[offset])
                    {
                        bool isNewMsg = _pendingData == null || PendingOverhead == null;
                        if (isNewMsg)
                        {
                            if (amtReceived < RpcOverhead.MinimumSize)
                            {
                                // todo localization
                                string msg = $"Received message less than {RpcOverhead.MinimumSize} bytes long!";
                                LogWarning(msg);
                                BufferProgressUpdated?.Invoke(0, 0);
                                error = new ContiguousBufferParseException(msg) { ErrorCode = 1 };
                                goto reset;
                            }
                            
                            PendingOverhead = RpcOverhead.ReadFromBytes(Connection.Remote, serializer, bytes, amtReceived);
                            if (!PendingOverhead.CheckSizeHashValid())
                            {
                                string msg = $"Mismatch in size hash of \"{PendingOverhead}\"!";
                                LogWarning(msg);
                                PendingOverhead = default;
                                BufferProgressUpdated?.Invoke(0, 0);
                                error = new ContiguousBufferParseException(msg) { ErrorCode = 2 };
                                goto reset;
                            }
                        }

                        uint size = PendingOverhead!.MessageSize;
                        uint expectedSize = (uint)(size + PendingOverhead.OverheadSize);
                        if (expectedSize > MaxMessageSize)
                        {
                            string msg = $"Incoming message \"{PendingOverhead}\" (including header data) has a size of {expectedSize} B which is greater than the max ({MaxMessageSize} B).";
                            LogWarning(msg);
                            PendingOverhead = default;
                            BufferProgressUpdated?.Invoke(0, 0);
                            error = new ContiguousBufferParseException(msg) { ErrorCode = 3 };
                            goto reset;
                        }

                        if (isNewMsg)
                        {
                            if (expectedSize == amtReceived) // new single packet, process all
                            {
                                BufferProgressUpdated?.Invoke(amtReceived, amtReceived);
                                callback(new Memory<byte>(Buffer, checked((int)offset), checked((int)amtReceived)), PendingOverhead);
                                goto reset;
                            }

                            if (amtReceived < expectedSize) // starting a new packet that continues past the current data, copy to full buffer and return
                            {
                                _pendingData = new byte[expectedSize];
                                fixed (byte* ptr = _pendingData)
                                    System.Buffer.MemoryCopy(bytes, ptr, expectedSize, amtReceived);
                                _pendingLength = amtReceived;
                                BufferProgressUpdated?.Invoke(amtReceived, expectedSize);
                                return;
                            }

                            BufferProgressUpdated?.Invoke(expectedSize, expectedSize);
                            // multiple messages in one.
                            callback(new Memory<byte>(Buffer, checked((int)offset), checked((int)expectedSize)), PendingOverhead);
                            _pendingData = null;
                            PendingOverhead = null;
                            _pendingLength = 0;
                            amtReceived -= expectedSize;
                            offset = expectedSize;
                            continue;
                        }

                        // this data will complete the pending packet
                        uint ttlSize = _pendingLength + amtReceived;
                        if (ttlSize == expectedSize)
                        {
                            fixed (byte* ptr = &_pendingData![_pendingLength])
                                System.Buffer.MemoryCopy(bytes, ptr, amtReceived, amtReceived);
                            BufferProgressUpdated?.Invoke(expectedSize, expectedSize);
                            callback(new Memory<byte>(_pendingData, 0, checked((int)expectedSize)), PendingOverhead);
                            goto reset;
                        }
                        // continue the data for another packet
                        if (ttlSize < expectedSize)
                        {
                            fixed (byte* ptr = &_pendingData![_pendingLength])
                                System.Buffer.MemoryCopy(bytes, ptr, amtReceived, amtReceived);
                            _pendingLength += amtReceived;
                            BufferProgressUpdated?.Invoke(ttlSize, expectedSize);
                            break;
                        }

                        // end off the current message, start the next one
                        uint remaining = expectedSize - _pendingLength;
                        fixed (byte* ptr = &_pendingData![_pendingLength])
                            System.Buffer.MemoryCopy(bytes, ptr, remaining, remaining);
                        BufferProgressUpdated?.Invoke(expectedSize, expectedSize);
                        callback(new Memory<byte>(_pendingData, 0, checked((int)expectedSize)), PendingOverhead);
                        hadMuchData = _pendingData.Length > GCTriggerSizeThreshold;
                        _pendingData = null;
                        PendingOverhead = default;
                        _pendingLength = 0;
                        if (hadMuchData)
                            GC.Collect();
                        amtReceived -= remaining;
                        offset = remaining;
                        continue;
                    }

                    reset:
                    hadMuchData = _pendingData != null && _pendingData.Length > GCTriggerSizeThreshold;
                    _pendingData = null;
                    PendingOverhead = default;
                    _pendingLength = 0;
                    if (hadMuchData)
                        GC.Collect();
                    break;
                }
                catch (OverflowException ex)
                {
                    LogError($"Overflow exception hit trying to allocate {PendingOverhead?.MessageSize} B for message {PendingOverhead}.");
                    _pendingData = null;
                    PendingOverhead = default;
                    Buffer = null!;
                    GC.Collect();
                    throw new ContiguousBufferParseException(Properties.Exceptions.ContiguousBufferParseException, ex) { ErrorCode = 4 };
                }
                catch (OutOfMemoryException ex)
                {
                    LogError($"Out of Memory trying to allocate {PendingOverhead?.MessageSize} B for message {PendingOverhead}.");
                    throw new ContiguousBufferParseException(Properties.Exceptions.ContiguousBufferParseException, ex) { ErrorCode = 5 };
                }
                catch (Exception ex)
                {
                    LogError($"Exception reading message {PendingOverhead}.");
                    throw new ContiguousBufferParseException(Properties.Exceptions.ContiguousBufferParseException, ex);
                }
            }
        }

        if (error != null)
            throw error;
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            return;

        Buffer = null!;
        if (_pendingData != null)
        {
            _pendingData = null;
            GC.Collect();
        }
        PendingOverhead = default;
    }
    ~ContiguousBuffer()
    {
        Dispose();
    }

    private void LogWarning(string text)
    {
        if (Logger != null)
            LogWarningExt(text);
        else
            Accessor.Logger?.LogWarning(nameof(ContiguousBuffer), text);
    }
    private void LogError(string text)
    {
        if (Logger != null)
            LogErrorExt(text);
        else
            Accessor.Logger?.LogError(nameof(ContiguousBuffer), null, text);
    }

    // separated it like this to avoid having a strict reliance on Microsoft.Extensions.Logging.Abstractions.dll
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogWarningExt(string text)
    {
        if (Logger is ILogger logger)
            logger.LogWarning(text);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogErrorExt(string text)
    {
        if (Logger is ILogger logger)
            logger.LogError(text);
    }
}

/// <summary>
/// Used to reperesent an update in the progress of a large download.
/// </summary>
/// <param name="bytesDownloaded">Number of bytes downloaded so far.</param>
/// <param name="totalBytes">Total number of bytes that need to be downloaded.</param>
public delegate void ContiguousBufferProgressUpdate(uint bytesDownloaded, uint totalBytes);