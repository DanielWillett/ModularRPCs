using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Base exception for all RPC errors.
/// </summary>
[Serializable]
public class RpcException : Exception
{

    /// <inheritdoc />
    public RpcException() { }

    /// <inheritdoc />
    public RpcException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}