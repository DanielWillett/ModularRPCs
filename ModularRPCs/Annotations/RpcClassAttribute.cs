using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Marks a class or struct for automatic RPC scanning and optionally provides a default declaring type for all targeting attributes.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcClassAttribute : Attribute
{
    /// <summary>
    /// The default declaring type to use for all targeting attributes in this class if not otherwise specified.
    /// </summary>
    [UsedImplicitly]
    public Type? DefaultType { get; set; }

    /// <summary>
    /// The default case-sensitive assembly-qualified declaring type name to use for all targeting attributes in this class if not otherwise specified.
    /// </summary>
    [UsedImplicitly]
    public string? DefaultTypeName { get; set; }
}