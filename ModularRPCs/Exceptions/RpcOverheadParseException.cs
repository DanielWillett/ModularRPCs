using System;
using System.Runtime.Serialization;
using DanielWillett.ModularRpcs.Protocol;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown by a <see cref="RpcOverhead.ReadFromBytes"/> or <see cref="RpcOverhead.ReadFromStream"/> when it fails to parse a proper overhead, likely due to bad data.
/// </summary>
[Serializable]
public class RpcOverheadParseException : RpcException
{
    /// <summary>
    /// Generic error code used by <see cref="RpcOverhead"/> to identify error types.
    /// </summary>
    public int ErrorCode { get; internal set; } = -1;

    /// <inheritdoc />
    public RpcOverheadParseException() : base(Properties.Exceptions.RpcOverheadParseException) { }

    /// <inheritdoc />
    public RpcOverheadParseException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcOverheadParseException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcOverheadParseException(SerializationInfo info, StreamingContext context) : base(info, context)
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