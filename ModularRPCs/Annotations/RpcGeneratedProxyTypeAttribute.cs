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
    /// The name of the method that is invoked by <see cref="ProxyGenerator"/> the first time this type is created. This has no effect in NET7_0_OR_GREATER as static virtual interface members are used instead.
    /// </summary>
    /// <remarks>It should have the following signature: <c>void ModularRpcsGeneratedSetupStaticGeneratedProxy(<see cref="GeneratedProxyTypeBuilder"/> state)</c></remarks>
    public string? TypeSetupMethodName { get; set; }
}