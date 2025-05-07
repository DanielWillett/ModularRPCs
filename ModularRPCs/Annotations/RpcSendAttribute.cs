using System;
using DanielWillett.ModularRpcs.Async;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a virtual or abstract RPC method as an RPC caller. It should return either <see langword="void"/>, <see cref="RpcTask"/>, or <see cref="RpcTask{T}"/>. Returning <see langword="void"/> marks the RPC as fire-and-forget.
/// <code>
/// [RpcSend(nameof(ReceiveRpc))]
/// protected virtual RpcTask&lt;string&gt; CallRpc(int numChars) => RpcTask&lt;string&gt;.NotImplemented;
/// </code>
/// Or in broadcast mode:
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcSendAttribute : RpcTargetAttribute
{
    /// <summary>
    /// Treat this send method as a 'broadcast' instead of an invocation. Methods with the <see cref="RpcReceiveAttribute"/> can listen for broadcasts from this endpoint.
    /// </summary>
    public RpcSendAttribute() { }

    /// <summary>
    /// Reference a receive method in this same type with the given name.
    /// </summary>
    /// <param name="methodName">The case-sensitive name of another method within the same type as the decorating method is declared in.</param>
    public RpcSendAttribute(string methodName) : base(methodName) { }

    /// <summary>
    /// Reference a receive method in the given type with the given name.
    /// </summary>
    /// <param name="methodName">The case-sensitive assembly qualified name of a type.</param>
    /// <param name="methodName">The case-sensitive name of another method within the same type as <paramref name="declaringType"/>.</param>
    public RpcSendAttribute(string declaringType, string methodName) : base(declaringType, methodName) { }

    /// <summary>
    /// Reference a receive method in the given type with the given name.
    /// </summary>
    /// <param name="methodName">The case-sensitive assembly qualified name of a type.</param>
    /// <param name="methodName">The case-sensitive name of another method within the same type as <paramref name="declaringType"/>.</param>
    public RpcSendAttribute(Type declaringType, string methodName) : base(declaringType, methodName) { }
}