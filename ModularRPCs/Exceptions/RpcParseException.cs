using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown while reading a message when a buffer or stream runs out, or data is unable to be read.
/// </summary>
[Serializable]
public class RpcParseException : RpcException
{
    /// <summary>
    /// Generic error code used by <see cref="RpcParseException"/> to identify error types.
    /// </summary>
    public int ErrorCode { get; set; } = -1;

    /// <inheritdoc />
    public RpcParseException() : base(Properties.Exceptions.RpcParseException) { }

    /// <inheritdoc />
    public RpcParseException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcParseException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcParseException(SerializationInfo info, StreamingContext context) : base(info, context)
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