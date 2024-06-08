using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Data;
internal class PassthroughReadStream : Stream
{
    private long _startInd;
    private long _length;
    private long _bytesRead;
    public long Count { get; }
    public Stream UnderlyingStream { get; }
    public override bool CanRead => UnderlyingStream.CanRead;
    public override bool CanSeek => UnderlyingStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position
    {
        get => _bytesRead;
        set
        {
            if (value is < 0 or > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (_startInd == -1)
            {
                _startInd = UnderlyingStream.Position - _bytesRead;
            }

            UnderlyingStream.Position = value + _bytesRead;
            _bytesRead = (int)value;
        }
    }
    public override int ReadTimeout
    {
        get => UnderlyingStream.ReadTimeout;
        set => UnderlyingStream.ReadTimeout = value;
    }
    public override int WriteTimeout
    {
        get => UnderlyingStream.WriteTimeout;
        set => UnderlyingStream.WriteTimeout = value;
    }
    public override bool CanTimeout => UnderlyingStream.CanTimeout;
    public PassthroughReadStream(Stream stream, long count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        Count = count;
        UnderlyingStream = stream ?? throw new ArgumentNullException(nameof(stream));
        _bytesRead = 0;

        try
        {
            _startInd = stream.Position;
        }
        catch (NotSupportedException)
        {
            _startInd = -1;
        }

        try
        {
            _length = stream.Length;
        }
        catch (NotSupportedException)
        {
            _length = -1;
        }

        Length = _startInd < 0 ? Count : Math.Min(Count, UnderlyingStream.Length - _startInd);
    }
    public override void SetLength(long value)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override void WriteByte(byte value)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override void EndWrite(IAsyncResult asyncResult)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamIAsyncResultNotSupported);
    }
    public override int EndRead(IAsyncResult asyncResult)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamIAsyncResultNotSupported);
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException(Properties.Exceptions.PassthroughStreamWriteNotSupported);
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        long allowedToRead = Math.Min(Count - _bytesRead, buffer.Length);
        if (allowedToRead <= 0)
            return 0;

        int actuallyRead = await UnderlyingStream.ReadAsync(buffer.Slice(0, (int)allowedToRead), cancellationToken);
        _bytesRead += actuallyRead;
        return actuallyRead;
    }
    public override int Read(Span<byte> buffer)
    {
        long allowedToRead = Math.Min(Count - _bytesRead, buffer.Length);
        if (allowedToRead <= 0)
            return 0;

        int actuallyRead = UnderlyingStream.Read(buffer.Slice(0, (int)allowedToRead));
        _bytesRead += actuallyRead;
        return actuallyRead;
    }
#endif
    public override int ReadByte()
    {
        int b = UnderlyingStream.ReadByte();
        if (b >= 0)
            ++_bytesRead;

        return b;
    }

    [Obsolete]
    public override object? InitializeLifetimeService() => UnderlyingStream.InitializeLifetimeService();
    public override int GetHashCode() => UnderlyingStream.GetHashCode();
    public override bool Equals(object? obj) => UnderlyingStream.Equals(obj);
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public override string? ToString() => UnderlyingStream.ToString();
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        long allowedToRead = Math.Min(Count - _bytesRead, count);
        if (allowedToRead <= 0)
            return 0;

        int actuallyRead = await UnderlyingStream.ReadAsync(buffer, offset, (int)allowedToRead, cancellationToken);
        _bytesRead += actuallyRead;
        return actuallyRead;
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        long allowedToRead = Math.Min(Count - _bytesRead, count);
        if (allowedToRead <= 0)
            return 0;

        int actuallyRead = UnderlyingStream.Read(buffer, offset, (int)allowedToRead);
        _bytesRead += actuallyRead;
        return actuallyRead;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (_startInd < 0)
                {
                    // will probably throw NotSupportedException again.
                    _startInd = UnderlyingStream.Position - _bytesRead;
                }

                if (offset > Count)
                {
                    throw new IOException(Properties.Exceptions.PassthroughStreamSeekOverflow);
                }

                long newPos = UnderlyingStream.Seek(_startInd + offset, SeekOrigin.Begin);
                _bytesRead = newPos - _startInd;
                break;

            case SeekOrigin.Current:
                if (offset == 0)
                    return _bytesRead;

                if (offset > 0 && offset + _bytesRead > Count
                    || offset < 0 && offset + _bytesRead < 0)
                {
                    throw new IOException(Properties.Exceptions.PassthroughStreamSeekOverflow);
                }

                UnderlyingStream.Seek(offset, SeekOrigin.Current);
                _bytesRead += offset;
                break;

            case SeekOrigin.End:
                if (_startInd < 0)
                {
                    // will probably throw NotSupportedException again.
                    _startInd = UnderlyingStream.Position - _bytesRead;
                }
                if (_length < 0)
                {
                    // will probably throw NotSupportedException again.
                    _length = UnderlyingStream.Length;
                }

                if (offset > Count)
                {
                    throw new IOException(Properties.Exceptions.PassthroughStreamSeekOverflow);
                }

                if (Count + _startInd == _length)
                {
                    UnderlyingStream.Seek(offset, SeekOrigin.End);
                    _bytesRead = Count - offset;
                    break;
                }

                newPos = UnderlyingStream.Seek(offset + (_length - (Count + _startInd)), SeekOrigin.End);
                _bytesRead = newPos - _startInd;
                break;
        }

        return _bytesRead;
    }
    public override Task FlushAsync(CancellationToken cancellationToken) => UnderlyingStream.FlushAsync(cancellationToken);
    public override void Close()
    {
        UnderlyingStream.Close();
        base.Close();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            UnderlyingStream.Dispose();
        base.Dispose(disposing);
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        await UnderlyingStream.DisposeAsync();
        await base.DisposeAsync();
    }
#endif
    public override void Flush()
    {
        UnderlyingStream.Flush();
    }
}
