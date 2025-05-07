using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration;

internal class RpcMethodDeclaration : IEquatable<RpcMethodDeclaration>
{
    public required string Name { get; init; }
    public bool IsReceive => Target is RpcReceiveAttribute;
    public bool IsSend => Target is RpcSendAttribute;
    public required RpcTargetAttribute Target { get; init; }
    public required RpcClassDeclaration Type { get; init; }
    public required EquatableList<RpcParameterDeclaration> Parameters { get; init; }

    /// <inheritdoc />
    public bool Equals(RpcMethodDeclaration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.Ordinal)
               && Equals(Parameters, other.Parameters)
               && Equals(Type, other.Type)
               && Equals(Target, other.Target);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RpcMethodDeclaration decl && Equals(decl);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, Parameters);
}