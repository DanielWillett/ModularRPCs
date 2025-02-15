using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a virtual or abstract RPC method as an RPC caller. It should return either <see langword="void"/>, <see cref="Task"/>, <see cref="Task{T}"/>, <see cref="ValueTask"/>, <see cref="ValueTask{T}"/>, or a serializable value. Returning <see langword="void"/> marks the RPC as fire-and-forget.
/// <code>
/// [RpcReceive]
/// internal async Task&lt;string&gt; ReceiveRpc(int numChars)
/// {
///     await Task.Delay(500);
///     return new string('0', numChars);
/// }
/// </code>
/// Or in broadcast mode:
/// <code>
/// [RpcReceive(nameof(CallRpc))]
/// internal async Task&lt;string&gt; ReceiveRpc(int numChars)
/// {
///     await Task.Delay(500);
///     return new string('0', numChars);
/// }
/// </code>
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcReceiveAttribute : RpcTargetAttribute
{
    /// <summary>
    /// Treat this send method as a 'broadcast' instead of an invocation. Methods with the <see cref="RpcReceiveAttribute"/> can listen for broadcasts from this endpoint.
    /// </summary>
    public RpcReceiveAttribute() { }

    /// <summary>
    /// Reference a receive method in this same type with the given name.
    /// </summary>
    /// <param name="methodName">The case-sensitive name of another method within the same type as the decorating method is declared in.</param>
    public RpcReceiveAttribute(string methodName) : base(methodName) { }

    /// <summary>
    /// Reference a receive method in the given type with the given name.
    /// </summary>
    /// <param name="declaringType">The case-sensitive assembly qualified name of the type where the target is declared.</param>
    /// <param name="methodName">The case-sensitive name of another method within the same type as <paramref name="declaringType"/>.</param>
    public RpcReceiveAttribute(string declaringType, string methodName) : base(declaringType, methodName) { }

    /// <summary>
    /// Reference a receive method in the given type with the given name.
    /// </summary>
    /// <param name="declaringType">The type where the target is declared.</param>
    /// <param name="methodName">The case-sensitive name of another method within the same type as <paramref name="declaringType"/>.</param>
    public RpcReceiveAttribute(Type declaringType, string methodName) : base(declaringType, methodName) { }
}