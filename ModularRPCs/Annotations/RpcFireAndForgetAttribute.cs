using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Mark an RPC method as 'fire or forget', which is the same as having the method return <see langword="void"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcFireAndForgetAttribute : Attribute;