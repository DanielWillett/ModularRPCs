using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;
/// <summary>
/// Thrown when data is attempted to be sent or received by a closed <see cref="IModularRpcConnection"/>
/// </summary>
[Serializable]
public class RpcConnectionClosedException : RpcException
{

    /// <inheritdoc />
    public RpcConnectionClosedException() : base(Properties.Exceptions.RpcConnectionClosedException) { }

    /// <inheritdoc />
    public RpcConnectionClosedException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcConnectionClosedException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcConnectionClosedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
