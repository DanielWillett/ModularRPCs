using System;
using DanielWillett.ModularRpcs.SourceGeneration.Util;

namespace DanielWillett.ModularRpcs.SourceGeneration;

public class RpcParameterDeclaration : IEquatable<RpcParameterDeclaration>
{
    public required string Name { get; init; }
    public required TypeSymbolInfo Type { get; init; }
    public required int Index { get; init; }
    public required string DisplayString { get; init; }

    /// <inheritdoc />
    public bool Equals(RpcParameterDeclaration other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(other, this))
            return true;

        return Index == other.Index
               && string.Equals(other.DisplayString, DisplayString, StringComparison.Ordinal)
               && Type.Equals(other.Type)
               && string.Equals(other.Name, Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RpcParameterDeclaration decl && Equals(decl);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, Type);
}