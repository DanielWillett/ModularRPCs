using DanielWillett.ModularRpcs.Reflection;
using JetBrains.Annotations;
using System;
using System.ComponentModel;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Defines that a type implements the required proxy steps itself.
/// </summary>
/// <remarks>This is mainly used by source generators.</remarks>
[MeansImplicitUse, EditorBrowsable(EditorBrowsableState.Never)]
[BaseTypeRequired(typeof(IRpcGeneratedProxyType))]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcGeneratedProxyTypeAttribute : Attribute
{
    /// <summary>
    /// The name of the method that is invoked by <see cref="ProxyGenerator"/> the first time this type is created.
    /// </summary>
    /// <remarks>It should have the following signature: <c>void ModularRpcsGeneratedSetupStaticGeneratedProxy(<see cref="GeneratedProxyTypeBuilder"/> state)</c></remarks>
    public string? TypeSetupMethodName { get; set; }
}