using DanielWillett.ModularRpcs.Reflection;
using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Provides a default declaring type for all targeting attributes.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcDefaultTargetTypeAttribute : Attribute
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

    /// <summary>
    /// Provides a default declaring type for all targeting attributes.
    /// </summary>
    public RpcDefaultTargetTypeAttribute(Type defaultType)
    {
        DefaultType = defaultType;
        DefaultTypeName = TypeUtility.GetAssemblyQualifiedNameNoVersion(defaultType);
    }

    /// <summary>
    /// Provides a default declaring type name for all targeting attributes.
    /// </summary>
    public RpcDefaultTargetTypeAttribute(string defaultTypeName)
    {
        DefaultTypeName = defaultTypeName;
    }
}