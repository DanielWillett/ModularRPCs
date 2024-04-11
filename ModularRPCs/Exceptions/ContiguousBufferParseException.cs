using System;
using System.Runtime.Serialization;
using DanielWillett.ModularRpcs.Data;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown by a <see cref="ContiguousBuffer"/> when it fails to separate out a message segment, likely due to bad data.
/// </summary>
[Serializable]
public class ContiguousBufferParseException : RpcException
{
    /// <summary>
    /// Generic error code used by <see cref="ContiguousBuffer"/> to identify error types.
    /// </summary>
    public int ErrorCode { get; internal set; } = -1;

    /// <inheritdoc />
    public ContiguousBufferParseException() : base(Properties.Exceptions.ContiguousBufferParseException) { }

    /// <inheritdoc />
    public ContiguousBufferParseException(string message) : base(message) { }

    /// <inheritdoc />
    public ContiguousBufferParseException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected ContiguousBufferParseException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = info.GetInt32("ErrorCode");
    }
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("ErrorCode", ErrorCode);
    }
}