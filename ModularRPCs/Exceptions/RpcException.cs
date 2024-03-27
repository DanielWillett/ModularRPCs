using System;
using System.Runtime.Serialization;

namespace DanielWillett.ModularRpcs.Exceptions;

/// <summary>
/// Base exception for all RPC errors.
/// </summary>
public class RpcException : Exception
{
    public RpcException() { }
    public RpcException(string message) : base(message) { }
    public RpcException(string message, Exception inner) : base(message, inner) { }
    protected RpcException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}