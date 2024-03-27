using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;
public class RpcFireAndForgetException : RpcException
{
    public RpcFireAndForgetException() : base(Properties.Exceptions.RpcFireAndForgetException) { }
    public RpcFireAndForgetException(string message) : base(message) { }
    public RpcFireAndForgetException(string message, Exception inner) : base(message, inner) { }
    protected RpcFireAndForgetException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}