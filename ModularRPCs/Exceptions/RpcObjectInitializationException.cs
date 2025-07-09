using DanielWillett.ModularRpcs.Protocol;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when a proxy class is created that implements <see cref="IRpcObject{T}"/> and the identifier is not initialized in the base constructor.
/// </summary>
[Serializable]
public class RpcObjectInitializationException : RpcException
{
    /// <inheritdoc />
    public RpcObjectInitializationException(string message) : base(message) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcObjectInitializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
