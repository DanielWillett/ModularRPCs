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
public sealed class RpcGeneratedProxyTypeAttribute : Attribute;