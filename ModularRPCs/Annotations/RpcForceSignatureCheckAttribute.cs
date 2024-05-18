using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Force an RPC to handle overloads, even if the overload isn't present on the local machine.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcForceSignatureCheckAttribute : Attribute;