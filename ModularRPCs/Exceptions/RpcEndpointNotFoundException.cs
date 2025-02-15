using DanielWillett.ModularRpcs.Abstractions;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown by a <see cref="IRpcInvocationPoint"/> when it fails to find the call site for the RPC, i.e. the method it's supposed to be calling.
/// </summary>
[Serializable]
public class RpcEndpointNotFoundException : RpcException
{
    /// <summary>
    /// The invocation data that couldn't be identified.
    /// </summary>
    public IRpcInvocationPoint? InvocationPoint { get; }

    /// <inheritdoc />
    public RpcEndpointNotFoundException() { }

    /// <inheritdoc />
    public RpcEndpointNotFoundException(IRpcInvocationPoint invocationPoint) : base(string.Format(Properties.Exceptions.RpcEndpointNotFoundException, invocationPoint.ToString()))
    {
        InvocationPoint = invocationPoint;
    }

    /// <inheritdoc />
    public RpcEndpointNotFoundException(IRpcInvocationPoint invocationPoint, string message) : base(message)
    {
        InvocationPoint = invocationPoint;
    }

    /// <inheritdoc />
    public RpcEndpointNotFoundException(IRpcInvocationPoint invocationPoint, string message, Exception inner) : base(message, inner)
    {
        InvocationPoint = invocationPoint;
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcEndpointNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        InvocationPoint = null;
    }
}