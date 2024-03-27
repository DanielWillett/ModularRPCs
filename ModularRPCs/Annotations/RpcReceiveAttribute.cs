using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a non-abstract method as able to be invoked by an RPC.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcReceiveAttribute : Attribute;