using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a parameter in an RPC receive method to be injected.
/// If set up using dependency injection, that service provider can be used to inject other services as well.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RpcInjectAttribute : Attribute;