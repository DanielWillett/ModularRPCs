using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

[Serializable]
public class RpcGetResultUsageException : RpcException
{

    /// <inheritdoc />
    public RpcGetResultUsageException() : base(Properties.Exceptions.RpcGetResultUsageException) { }

    /// <inheritdoc />
    public RpcGetResultUsageException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcGetResultUsageException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcGetResultUsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}