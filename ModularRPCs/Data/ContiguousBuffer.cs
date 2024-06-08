using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Net.Sockets;
using System.Threading;

namespace DanielWillett.ModularRpcs.Data;

/// <summary>
/// Allows reading messages when their payloads could be sequentially read into the same buffer (like from a <see cref="NetworkStream"/>), or where the entire message may not be received at one time.
/// </summary>
/// <remarks>This buffer only supports one sequential read 'thread' at a time.</remarks>
public sealed class ContiguousBuffer : IContiguousBufferProgressUpdateDispatcher, IRefSafeLoggable
{
    private int _disposed;
    private byte[]? _pendingData;
    private uint _pendingLength;
    private PrimitiveRpcOverhead? _pendingOverhead;
    private object? _logger;
    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

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
    public PrimitiveRpcOverhead? PendingOverhead
    {
        get => _pendingOverhead;
        private set => _pendingOverhead = value;
    }

    /// <summary>
    /// If there is a currently pending message, meaning a message that is in progress of being received.
    /// </summary>
    public bool HasPendingMessage => PendingOverhead != null;

    /// <inheritdoc />
    public event ContiguousBufferProgressUpdate? BufferProgressUpdated;

    /// <summary>
    /// The connection that created this <see cref="ContiguousBuffer"/>.
    /// </summary>
    public IModularRpcLocalConnection Connection { get; }

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
    public unsafe void ProcessBuffer(uint amtReceived, IRpcSerializer serializer, ContiguousBufferCallback callback, uint offset = 0)
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
                        bool isNewMsg = _pendingData == null || !_pendingOverhead.HasValue;
                        if (isNewMsg)
                        {
                            if (amtReceived < PrimitiveRpcOverhead.MinimumLength)
                            {
                                string msg = string.Format(Properties.Exceptions.ContiguousBufferMessageTooShort, PrimitiveRpcOverhead.MinimumLength);
                                this.LogWarning(msg);
                                BufferProgressUpdated?.Invoke(null, 0, 0);
                                error = new ContiguousBufferParseException(msg) { ErrorCode = 1 };
                                goto reset;
                            }

                            _pendingOverhead = PrimitiveRpcOverhead.ReadFromBytes(Connection.Remote, serializer, bytes, amtReceived);
                            if (_pendingOverhead.Value.Size != _pendingOverhead.Value.SizeCheck)
                            {
                                string msg = string.Format(Properties.Exceptions.ContiguousBufferSizeHashMismatch, _pendingOverhead.Value.ToString());
                                this.LogWarning(msg);
                                _pendingOverhead = default;
                                BufferProgressUpdated?.Invoke(null, 0, 0);
                                error = new ContiguousBufferParseException(msg) { ErrorCode = 2 };
                                goto reset;
                            }
                        }

                        PrimitiveRpcOverhead ovh = _pendingOverhead!.Value;
                        uint size = ovh.Size;
                        uint expectedSize = size + ovh.OverheadSize;
                        if (expectedSize > MaxMessageSize)
                        {
                            string msg = string.Format(Properties.Exceptions.ContiguousBufferOversizedMessage, _pendingOverhead, expectedSize, MaxMessageSize);
                            this.LogWarning(msg);
                            _pendingOverhead = default;
                            BufferProgressUpdated?.Invoke(in _pendingOverhead, 0, 0);
                            error = new ContiguousBufferParseException(msg) { ErrorCode = 3 };
                            goto reset;
                        }

                        if (isNewMsg)
                        {
                            if (expectedSize == amtReceived) // new single packet, process all
                            {
                                BufferProgressUpdated?.Invoke(in _pendingOverhead, amtReceived, amtReceived);
                                callback(Buffer.AsMemory(checked( (int)offset ), checked( (int)expectedSize) ), false, in ovh);
                                goto reset;
                            }

                            if (amtReceived < expectedSize) // starting a new packet that continues past the current data, copy to full buffer and return
                            {
                                _pendingData = new byte[expectedSize];
                                fixed (byte* ptr = _pendingData)
                                    System.Buffer.MemoryCopy(bytes, ptr, expectedSize, amtReceived);
                                _pendingLength = amtReceived;
                                BufferProgressUpdated?.Invoke(in _pendingOverhead, amtReceived, expectedSize);
                                return;
                            }

                            BufferProgressUpdated?.Invoke(in _pendingOverhead, expectedSize, expectedSize);
                            // multiple messages in one.
                            callback(Buffer.AsMemory(checked( (int)offset ), checked( (int)expectedSize) ), false, in ovh);
                            _pendingData = null;
                            _pendingOverhead = null;
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
                            BufferProgressUpdated?.Invoke(in _pendingOverhead, expectedSize, expectedSize);
                            callback(_pendingData.AsMemory(0, checked( (int)expectedSize )), true, in ovh);
                            goto reset;
                        }
                        // continue the data for another packet
                        if (ttlSize < expectedSize)
                        {
                            fixed (byte* ptr = &_pendingData![_pendingLength])
                                System.Buffer.MemoryCopy(bytes, ptr, amtReceived, amtReceived);
                            _pendingLength += amtReceived;
                            BufferProgressUpdated?.Invoke(in _pendingOverhead, ttlSize, expectedSize);
                            break;
                        }

                        // end off the current message, start the next one
                        uint remaining = expectedSize - _pendingLength;
                        fixed (byte* ptr = &_pendingData![_pendingLength])
                            System.Buffer.MemoryCopy(bytes, ptr, remaining, remaining);
                        BufferProgressUpdated?.Invoke(in _pendingOverhead, expectedSize, expectedSize);
                        callback(_pendingData.AsMemory(0, checked( (int)expectedSize )), true, in ovh);
                        hadMuchData = _pendingData.Length > GCTriggerSizeThreshold;
                        _pendingData = null;
                        _pendingOverhead = default;
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
                    _pendingOverhead = default;
                    _pendingLength = 0;
                    if (hadMuchData)
                        GC.Collect();
                    break;
                }
                catch (OverflowException ex)
                {
                    string msg = string.Format(Properties.Exceptions.ContiguousBufferOverflow, _pendingOverhead.GetValueOrDefault().Size, _pendingOverhead.ToString());
                    _pendingData = null;
                    _pendingOverhead = default;
                    Buffer = null!;
                    GC.Collect();
                    this.LogError(msg);
                    throw new ContiguousBufferParseException(msg, ex) { ErrorCode = 4 };
                }
                catch (OutOfMemoryException ex)
                {
                    string msg = string.Format(Properties.Exceptions.ContiguousBufferOverflow, _pendingOverhead.GetValueOrDefault().Size, _pendingOverhead.ToString());
                    this.LogError(msg);
                    throw new ContiguousBufferParseException(msg, ex) { ErrorCode = 5 };
                }
                catch (Exception ex)
                {
                    string msg = string.Format(Properties.Exceptions.ContiguousBufferException, _pendingOverhead.ToString());
                    this.LogError(msg);
                    throw new ContiguousBufferParseException(msg, ex);
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
}

/// <summary>
/// Used to reperesent an update in the progress of a large download.
/// </summary>
/// <param name="overhead">The current RPC that is downloading. This will be <see langword="null"/> if this is a 'clear' invocation, meaning after the RPC is done a 0/0 event will be fired.</param>
/// <param name="bytesDownloaded">Number of bytes downloaded so far.</param>
/// <param name="totalBytes">Total number of bytes that need to be downloaded.</param>
public delegate void ContiguousBufferProgressUpdate(in PrimitiveRpcOverhead? overhead, uint bytesDownloaded, uint totalBytes);

/// <summary>
/// Handles each packet that's parsed from a set of binary data.
/// </summary>
/// <param name="overhead">Information about the RPC call.</param>
/// <param name="canTakeOwnership">If the backing storage for <paramref name="data"/> is safe to use outside the current stack frame. If this is <see langword="false"/>, data should be copied before context switching.</param>
/// <param name="data">Span of bytes, not including the overhead.</param>
public delegate void ContiguousBufferCallback(ReadOnlyMemory<byte> data, bool canTakeOwnership, in PrimitiveRpcOverhead overhead);