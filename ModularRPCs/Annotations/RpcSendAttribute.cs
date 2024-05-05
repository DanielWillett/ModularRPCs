using System;
using DanielWillett.ModularRpcs.Async;
using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a virtual or abstract RPC method as an RPC caller. It should return either <see langword="void"/>, <see cref="RpcTask"/>, or <see cref="RpcTask{T}"/>.
/// <code>
/// [RpcSend]
/// protected virtual RpcTask CallRpc() { }
/// </code>
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcSendAttribute : Attribute
{
    /// <summary>
    /// The declaring type of the RPC to call on the receiving end.
    /// </summary>
    /// <remarks>Default value depends on the RPC configuration setting.
    /// If it's set on same assembly mode, it defaults to the declaring type of this send method,
    /// otherwise <see cref="TypeName"/> defaults to the same name as the current declaring type name but not necessarily the same type.</remarks>
    public Type? Type { get; set; }

    /// <summary>
    /// The declaring type name of the RPC to call on the receiving end. Best to use a full type name and assembly name, especially when not in same assembly mode.
    /// </summary>
    /// <remarks>Default value depends on the RPC configuration setting.
    /// If it's set on same assembly mode, it defaults to the declaring type of this send method,
    /// otherwise it defaults to the same name as the current declaring type name but not necessarily the same type.</remarks>
    public string? TypeName { get; set; }

    /// <summary>
    /// The method name to invoke on the receiving end.
    /// </summary>
    /// <remarks>Defaults to the current method name being decorated by this attribute. If the method name starts with 'Call', that is removed.</remarks>
    public string? MethodName { get; set; }
}