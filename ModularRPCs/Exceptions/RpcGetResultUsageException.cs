using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;
public class RpcGetResultUsageException : RpcException
{
    public RpcGetResultUsageException() : base(Properties.Exceptions.RpcGetResultUsageException) { }
    public RpcGetResultUsageException(string message) : base(message) { }
    public RpcGetResultUsageException(string message, Exception inner) : base(message, inner) { }
    protected RpcGetResultUsageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}