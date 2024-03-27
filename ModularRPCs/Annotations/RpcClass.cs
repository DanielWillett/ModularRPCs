using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a class or struct for automatic RPC scanning.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcClassAttribute : Attribute;