using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NotSupportedException = System.NotSupportedException;

namespace DanielWillett.ModularRpcs.Data;

/// <summary>
/// Read-only passthrough stream that can prepend overhead data before starting to read from the underlying stream, while keeping a limit to the amount of data read from the underlying stream.
/// </summary>
public class OverheadStreamPrepender : Stream
{
    private int _ovhProgress;
    private long _dataRead;

    /// <inheritdoc />
    public override bool CanRead => UnderlyingStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => DataCount + Overhead.Count;

    /// <inheritdoc />
    public override long Position
    {
        get => _dataRead + _ovhProgress;
        set => throw new NotSupportedException(Properties.Exceptions.PassthroughStreamSeekNotSupported);
    }
    
    /// <summary>
    /// The original underlying stream.
    /// </summary>
    public Stream UnderlyingStream { get; }
    
    /// <summary>
    /// If the overhead has been fully read.
    /// </summary>
    public bool HasReadOverhead => _ovhProgress >= Overhead.Count;
    
    /// <summary>
    /// Overhead data that will be read first.
    /// </summary>
    public ArraySegment<byte> Overhead { get; }

    /// <summary>
    /// Total amount of bytes allowed to be read from <see cref="UnderlyingStream"/>.
    /// </summary>
    public long DataCount { get; }

    /// <summary>
    /// If <see cref="UnderlyingStream"/> won't be disposed when this stream is.
    /// </summary>
    public bool LeaveOpen { get; }
    public OverheadStreamPrepender(Stream stream, ArraySegment<byte> overhead, long dataCt, bool leaveOpen)
    {
        if (overhead.Array == null && overhead.Count > 0)
            throw new ArgumentNullException(nameof(overhead));

        UnderlyingStream = stream ?? throw new ArgumentNullException(nameof(stream));
        Overhead = overhead;
        DataCount = dataCt;
        LeaveOpen = leaveOpen;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        UnderlyingStream.Flush();
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return UnderlyingStream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int ReadByte()
    {
        if (_ovhProgress >= Overhead.Count)
        {
            if (_dataRead >= DataCount)
                return -1;

            int read = UnderlyingStream.ReadByte();
            if (read != -1)
                ++_dataRead;
            return read;
        }

        byte b = Overhead.Array![Overhead.Offset + _ovhProgress];
        ++_ovhProgress;
        return b;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        int ctRead = 0;
        if (_ovhProgress < Overhead.Count)
        {
            int ct = Math.Min(count, Overhead.Count - _ovhProgress);
            Buffer.BlockCopy(Overhead.Array!, Overhead.Offset + _ovhProgress, buffer, offset, ct);
            _ovhProgress += ct;
            count -= ct;
            ctRead = ct;
        }

        if (count == 0)
            return ctRead;

        long amtToRead = Math.Min(DataCount - _dataRead, count);

        if (amtToRead <= 0)
            return ctRead;

        int read = UnderlyingStream.Read(buffer, offset + ctRead, (int)amtToRead);
        _dataRead += read;
        return ctRead + read;
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int ctRead = 0;
        if (_ovhProgress < Overhead.Count)
        {
            int ct = Math.Min(count, Overhead.Count - _ovhProgress);
            Buffer.BlockCopy(Overhead.Array!, Overhead.Offset + _ovhProgress, buffer, offset, ct);
            _ovhProgress += ct;
            count -= ct;
            ctRead = ct;
        }

        if (count == 0)
            return ctRead;

        long amtToRead = Math.Min(DataCount - _dataRead, count);

        if (amtToRead <= 0)
            return ctRead;

        int read = await UnderlyingStream.ReadAsync(buffer, offset + ctRead, (int)amtToRead, cancellationToken);
        _dataRead += read;
        return ctRead + read;
    }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        int ctRead = 0;
        int count = buffer.Length;
        if (_ovhProgress < Overhead.Count)
        {
            int ct = Math.Min(buffer.Length, Overhead.Count - _ovhProgress);
            Overhead.Array.AsSpan(Overhead.Offset + _ovhProgress, ct).CopyTo(buffer.Slice(0, ct));
            _ovhProgress += ct;
            count -= ct;
            ctRead = ct;
        }

        if (count == 0)
            return ctRead;

        long amtToRead = Math.Min(DataCount - _dataRead, count);

        if (amtToRead <= 0)
            return ctRead;

        int read = UnderlyingStream.Read(buffer.Slice(ctRead, (int)amtToRead));
        _dataRead += read;
        return ctRead + read;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int ctRead = 0;
        int count = buffer.Length;
        if (_ovhProgress < Overhead.Count)
        {
            int ct = Math.Min(buffer.Length, Overhead.Count - _ovhProgress);
            Overhead.Array.AsSpan(Overhead.Offset + _ovhProgress, ct).CopyTo(buffer.Slice(0, ct).Span);
            _ovhProgress += ct;
            count -= ct;
            ctRead = ct;
        }

        if (count == 0)
            return ctRead;

        long amtToRead = Math.Min(DataCount - _dataRead, count);

        if (amtToRead <= 0)
            return ctRead;

        int read = await UnderlyingStream.ReadAsync(buffer.Slice(ctRead, (int)amtToRead), cancellationToken);
        _dataRead += read;
        return ctRead + read;
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        return !LeaveOpen ? UnderlyingStream.DisposeAsync() : default;
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
#endif

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !LeaveOpen)
            UnderlyingStream.Dispose();
    }
    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamIAsyncResultNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override int EndRead(IAsyncResult asyncResult)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamIAsyncResultNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamSeekNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override void SetLength(long value)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamSeekNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override void WriteByte(byte value)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }

    /// <summary>Not supported.</summary>
    /// <exception cref="NotSupportedException"/>
    public override void EndWrite(IAsyncResult asyncResult)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
}
