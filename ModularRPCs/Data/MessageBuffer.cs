using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;

namespace DanielWillett.ModularRpcs.Data;

/// <summary>
/// Keeps track of incoming ModularRPC messages that may or may not be fragmented.
/// </summary>
public class MessageBuffer : IRefSafeLoggable
{
    private object? _logger;

    private readonly IMessageBufferHandler _handler;
    private readonly IRpcSerializer _serializer;

    private byte[] _buffer;
    private int _headerProgress;

    private MessageInfo _pendingMessage;

    /// <summary>
    /// Buffer used for message output.
    /// </summary>
    public ArraySegment<byte> Buffer => new ArraySegment<byte>(_buffer, _headerProgress, _buffer.Length - _headerProgress);

    public MessageBuffer(IMessageBufferHandler handler, IRpcSerializer serializer, int bufferSize)
    {
        if (bufferSize < PrimitiveRpcOverhead.MinimumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        }

        _handler = handler;
        _serializer = serializer;
        _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Resets the current buffer, usually after a reconnect has occurred.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Reset()
    {
        ResetCurrentMessage();
        Unsafe.InitBlock(ref _buffer[0], 0, (uint)_buffer.Length);
        GC.Collect(-1, GCCollectionMode.Optimized, blocking: false);
    }

    private void ResetCurrentMessage()
    {
        _pendingMessage = default;
        _headerProgress = 0;
    }

    /// <summary>
    /// Handle bytes written to <see cref="Buffer"/>.
    /// </summary>
    /// <param name="amount">The amount of bytes that were written, starting at the first index in the array.</param>
    /// <param name="endOfMessage">If known, whether or not this packet is the last packet in the message.</param>
    /// <exception cref="ObjectDisposedException"/>
    public unsafe void HandleIncomingData(int amount, bool? endOfMessage = null)
    {
        fixed (byte* ptr = _buffer)
        {
            try
            {
                HandleIncomingData(ptr, amount, endOfMessage);
            }
            catch (OverflowException ex)
            {
                string msg = string.Format(Properties.Exceptions.ContiguousBufferOverflow, _pendingMessage.Size, _pendingMessage.Overhead.ToString());
                Reset();
                // this.LogError(msg);
                throw new ContiguousBufferParseException(msg, ex) { ErrorCode = 4 };
            }
            catch (OutOfMemoryException ex)
            {
                string msg = string.Format(Properties.Exceptions.ContiguousBufferOverflow, _pendingMessage.Size, _pendingMessage.Overhead.ToString());
                Reset();
                // this.LogError(msg);
                throw new ContiguousBufferParseException(msg, ex) { ErrorCode = 5 };
            }
            catch (Exception ex)
            {
                string msg = string.Format(Properties.Exceptions.ContiguousBufferException, _pendingMessage.Overhead.ToString());
                // this.LogError(msg);
                throw new ContiguousBufferParseException(msg, ex);
            }
        }
    }

    private unsafe void HandleIncomingData(byte* ptr, int size, bool? endOfMessage)
    {
        size += _headerProgress;

        if (size < PrimitiveRpcOverhead.MinimumLength)
        {

        }

        bool isNewMessage = _pendingMessage.DataProgress == null;
        if (isNewMessage)
        {

        }
    }

    ref object? IRefSafeLoggable.Logger => ref _logger;
    LoggerType IRefSafeLoggable.LoggerType { get; set; }

    private struct MessageInfo
    {
        public DateTime Date;
        public byte[] DataProgress;
        public PrimitiveRpcOverhead Overhead;

        public long Size => Overhead.Size;
    }
}

/// <summary>
/// Object passed to <see cref="MessageBuffer"/> to get notified when a full message is read.
/// </summary>
public interface IMessageBufferHandler
{
    /// <summary>
    /// Invoked by <see cref="MessageBuffer"/> when a full message is received.
    /// </summary>
    void HandleIncomingMessage();
}