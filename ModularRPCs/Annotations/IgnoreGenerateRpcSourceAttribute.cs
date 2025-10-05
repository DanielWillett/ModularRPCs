using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Tells the source generator to ignore a <see cref="GenerateRpcSourceAttribute"/> on this type for some reason.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreGenerateRpcSourceAttribute : Attribute;