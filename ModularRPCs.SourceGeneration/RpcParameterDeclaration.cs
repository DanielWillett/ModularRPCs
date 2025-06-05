using System;

namespace DanielWillett.ModularRpcs.SourceGeneration;

public class RpcParameterDeclaration : IEquatable<RpcParameterDeclaration>
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required int Index { get; init; }

    /// <inheritdoc />
    public bool Equals(RpcParameterDeclaration other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(other, this))
            return true;

        return string.Equals(other.TypeName, TypeName, StringComparison.Ordinal)
               && string.Equals(other.Name, Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RpcParameterDeclaration decl && Equals(decl);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, TypeName);
}