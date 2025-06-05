using JetBrains.Annotations;
using System;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Defines that a receive method's Invoke method.
/// </summary>
/// <remarks>This is mainly used by source generators.</remarks>
[MeansImplicitUse, EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class RpcGeneratedProxyReceiveMethodAttribute : Attribute
{
    public string MethodName { get; }
    public Type[] Parameters { get; }
    public string InvokeBytesMethod { get; }
    public string InvokeStreamMethod { get; }

    public RpcGeneratedProxyReceiveMethodAttribute(string methodName, string invokeBytesMethod, string invokeStreamMethod, params Type[] parameters)
    {
        MethodName = methodName;
        InvokeBytesMethod = invokeBytesMethod;
        InvokeStreamMethod = invokeStreamMethod;
        Parameters = parameters;
    }
}