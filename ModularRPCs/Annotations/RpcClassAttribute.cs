using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a class or struct for automatic RPC scanning.
/// </summary>
/// <remarks>This only really affects finding broadcast receive RPC's but could be used by your own type discovery system.</remarks>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcClassAttribute : Attribute;