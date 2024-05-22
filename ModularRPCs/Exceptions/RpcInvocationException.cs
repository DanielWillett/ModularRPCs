using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ReflectionTools;
using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when a parameter in an RPC is unserializable.
/// </summary>
[Serializable]
public class RpcInvocationException : Exception
{
    /// <summary>
    /// The RPC that was invoked.
    /// </summary>
    public IRpcInvocationPoint? InvocationPoint { get; }

    /// <summary>
    /// Assembly qualified type name of the exception from the remote side.
    /// </summary>
    public string? RemoteExceptionTypeName { get; }

    /// <summary>
    /// Type of the exception from the remote side, if it was found.
    /// </summary>
    public Type? RemoteExceptionType { get; }

    /// <summary>
    /// Stack trace on the remote side.
    /// </summary>
    public string? RemoteStackTrace { get; }

    /// <summary>
    /// Message of the exception on the remote side.
    /// </summary>
    public string? RemoteMessage { get; }

    /// <inheritdoc />
    public RpcInvocationException() : base(Properties.Exceptions.RpcInvocationException) { }

    /// <summary>
    /// Create a new <see cref="RpcInvocationException"/> around the given <see cref="IRpcInvocationPoint"/>.
    /// </summary>
    public RpcInvocationException(IRpcInvocationPoint invocationPoint, object remoteExceptionType, string? remoteMessage, string? remoteStackTrace)
        : base(string.Format(
            Properties.Exceptions.RpcInvocationExceptionWithInvocationPointMessage,
            invocationPoint,
            remoteExceptionType is Type t
                ? Accessor.ExceptionFormatter.Format(t)
                : (remoteExceptionType?.ToString() ?? Accessor.ExceptionFormatter.Format(typeof(Exception))),
            remoteMessage ?? string.Empty)
        )
    {
        if (remoteExceptionType is Type exType)
        {
            RemoteExceptionType = exType;
            RemoteExceptionTypeName = exType.AssemblyQualifiedName;
        }
        else if (remoteExceptionType != null)
        {
            RemoteExceptionTypeName = remoteExceptionType.ToString();
            RemoteExceptionType = RemoteExceptionTypeName == null ? null : Type.GetType(RemoteExceptionTypeName, false, false);
        }

        RemoteMessage = remoteMessage;
        RemoteStackTrace = remoteStackTrace;

        InvocationPoint = invocationPoint;
    }

    /// <inheritdoc />
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcInvocationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        RemoteExceptionTypeName = (string?)info.GetValue("RemoteExceptionTypeName", typeof(string));
        RemoteExceptionType = (Type?)info.GetValue("RemoteExceptionType", typeof(Type));
        RemoteStackTrace = (string?)info.GetValue("RemoteStackTrace", typeof(string));
        RemoteMessage = (string?)info.GetValue("RemoteMessage", typeof(string));
    }

#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("RemoteExceptionTypeName", RemoteExceptionTypeName);
        info.AddValue("RemoteExceptionType", RemoteExceptionType);
        info.AddValue("RemoteStackTrace", RemoteStackTrace);
        info.AddValue("RemoteMessage", RemoteMessage);
    }
}