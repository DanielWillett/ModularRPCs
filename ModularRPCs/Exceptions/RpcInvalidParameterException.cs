using System;
using System.Reflection;
using System.Runtime.Serialization;
using DanielWillett.ReflectionTools;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when a parameter in an RPC is unserializable.
/// </summary>
[Serializable]
public class RpcInvalidParameterException : Exception
{

    /// <inheritdoc />
    public RpcInvalidParameterException() : base(Properties.Exceptions.RpcInvalidParameterException) { }

    /// <inheritdoc />
    public RpcInvalidParameterException(int position, ParameterInfo parameter, MethodBase method, string message) : base(
        string.Format(Properties.Exceptions.RpcInvalidParameterExceptionInfo,
            position,
            Accessor.ExceptionFormatter.Format(parameter),
            Accessor.ExceptionFormatter.Format(method),
            message
        )
    )
    {

    }

    /// <inheritdoc />
    public RpcInvalidParameterException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcInvalidParameterException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcInvalidParameterException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}