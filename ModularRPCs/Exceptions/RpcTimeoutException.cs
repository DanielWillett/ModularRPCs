using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

[Serializable]
public class RpcTimeoutException : RpcException
{
    public TimeSpan Timeout { get; }
    public RpcTimeoutException(TimeSpan timeout) : base(string.Format(Properties.Exceptions.RpcTimeoutException, timeout.ToString("g")))
    {
        Timeout = timeout;
    }
    public RpcTimeoutException(TimeSpan timeout, string message) : base(message)
    {
        Timeout = timeout;
    }

    public RpcTimeoutException(TimeSpan timeout, string message, Exception inner) : base(message, inner)
    {
        Timeout = timeout;
    }
#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    protected RpcTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Timeout = new TimeSpan((long)info.GetValue("Timeout", typeof(long)));
    }

#if NET8_0_OR_GREATER
    [Obsolete]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Timeout", Timeout.Ticks);
        base.GetObjectData(info, context);
    }
}