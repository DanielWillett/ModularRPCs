using DanielWillett.ModularRpcs.Reflection;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Tells the source generator to create a proxy class at compile time instead of at runtime. This type must be partial, non-static, and non-sealed.
/// </summary>
/// <remarks>A <see cref="RpcGeneratedProxyTypeAttribute"/> will be added to this type, and it will be made to implement <see cref="IRpcGeneratedProxyType"/>.</remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateRpcSourceAttribute : Attribute;