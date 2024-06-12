using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ReflectionTools;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Thrown when a parameter in an RPC is unserializable.
/// </summary>
[Serializable]
public class RpcInvocationException : Exception
{
    private readonly bool _isRemote;
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
    public override string StackTrace => _isRemote || RemoteStackTrace == null ? base.StackTrace : RemoteStackTrace;

    /// <summary>
    /// Optional list of inner exceptions. This will only have a value if there's more than one, otherwise the inner exception will be in <see cref="Exception.InnerException"/>.
    /// </summary>
    public RpcInvocationException[]? InnerExceptions { get; }

    /// <inheritdoc />
    public RpcInvocationException() : base(Properties.Exceptions.RpcInvocationException)
    {
        _isRemote = false;
    }

    /// <inheritdoc />
    public RpcInvocationException(Exception inner) : base(Properties.Exceptions.RpcInvocationException, inner)
    {
        _isRemote = false;
    }

    /// <summary>
    /// Create a new <see cref="RpcInvocationException"/> around the given <see cref="IRpcInvocationPoint"/>.
    /// </summary>
    public RpcInvocationException(IRpcInvocationPoint? invocationPoint, object remoteExceptionType, string? remoteMessage, string? remoteStackTrace, RpcInvocationException? inner, RpcInvocationException[]? inners)
        : base(string.Format(
            Properties.Exceptions.RpcInvocationExceptionWithInvocationPointMessage,
            invocationPoint,
            remoteExceptionType is Type t
                ? Accessor.ExceptionFormatter.Format(t)
                : (remoteExceptionType?.ToString() ?? Accessor.ExceptionFormatter.Format(typeof(Exception))),
            remoteMessage ?? string.Empty)
            , inner)
    {
        _isRemote = true;
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
        if (InnerException == null)
            InnerExceptions = inners;
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
        
        int ct = info.GetInt32("InnerExceptionCt");
        if (ct == 1)
            return;

        InnerExceptions = new RpcInvocationException[ct];
        for (int i = 0; i < ct; i++)
        {
            InnerExceptions[i] = (RpcInvocationException?)info.GetValue("InnerException_" + i.ToString(CultureInfo.InvariantCulture), typeof(RpcInjectionException))
                                 ?? throw new SerializationException($"Inner exception {i} is null.");
        }
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
        int ct = InnerException == null ? InnerExceptions?.Length ?? 0 : 1;
        if (ct == 1)
            return;

        for (int i = 0; i < ct; i++)
        {
            info.AddValue("InnerException_" + i.ToString(CultureInfo.InvariantCulture), InnerExceptions![i]);
        }
    }
    public override string ToString()
    {
        if (!_isRemote)
            return base.ToString();

        string? message = RemoteMessage ?? Message;
        string str = string.IsNullOrEmpty(message) ? RemoteExceptionTypeName ?? string.Empty : RemoteExceptionTypeName + ": " + message;
        if (InnerException != null)
        {
            str = str + " ---> " + InnerException + Environment.NewLine + "   --- End of stack trace from previous location where exception was thrown ---";
        }
        string? stackTrace = RemoteStackTrace;
        if (stackTrace != null)
            str = str + Environment.NewLine + stackTrace;
        return str;
    }
}