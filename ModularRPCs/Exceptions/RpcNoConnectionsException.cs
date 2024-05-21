using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown by a <see cref="IRpcInvocationPoint"/> when it fails to find the call site for the RPC, i.e. the method it's supposed to be calling.
/// </summary>
[Serializable]
public class RpcNoConnectionsException : RpcException
{

    /// <inheritdoc />
    public RpcNoConnectionsException() : base(Properties.Exceptions.RpcNoConnectionsException) { }

    /// <inheritdoc />
    public RpcNoConnectionsException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcNoConnectionsException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcNoConnectionsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}