using System;
using System.Reflection;
using System.Runtime.Serialization;
using DanielWillett.ReflectionTools;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when a parameter in an RPC is unserializable.
/// </summary>
[Serializable]
public class RpcInjectionException : Exception
{

    /// <inheritdoc />
    public RpcInjectionException() : base(Properties.Exceptions.RpcInjectionException) { }
    public RpcInjectionException(ParameterInfo parameter, MethodBase method) : base(
        string.Format(
            Properties.Exceptions.RpcInjectionExceptionInfo,
            parameter.Name,
            Accessor.ExceptionFormatter.Format(parameter.ParameterType),
            Accessor.ExceptionFormatter.Format(method))
        ) { }

    /// <inheritdoc />
    public RpcInjectionException(string message) : base(message) { }

    /// <inheritdoc />
    public RpcInjectionException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcInjectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}