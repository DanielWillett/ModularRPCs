using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

public readonly struct TypeSymbolInfo : IEquatable<TypeSymbolInfo>
{
    public string Name { get; }
    public string? NamespaceDeclaration { get; }
    public string FullyQualifiedName { get; }
    public string Definition { get; }
    public string FileName { get; }

    public TypeSymbolInfo(ITypeSymbol typeSymbol)
    {
        Name = typeSymbol.Name;
        string? nameSpace = typeSymbol.ContainingNamespace?.ToDisplayString(CustomFormats.NamespaceWithoutGlobalFormat);
        if (string.IsNullOrEmpty(nameSpace))
        {
            FileName = Name;
        }
        else
        {
            FileName = $"{nameSpace}.{Name}";
        }
        NamespaceDeclaration = typeSymbol.ContainingNamespace?.ToDisplayString(CustomFormats.NamespaceDeclarationFormat);
        Definition = typeSymbol.ToDisplayString(CustomFormats.TypeDeclarationFormat);
        FullyQualifiedName = typeSymbol.ToDisplayString(NullableFlowState.NotNull, CustomFormats.FullTypeNameFormat);
    }

    public override bool Equals(object? obj) => obj is TypeSymbolInfo other && Equals(other);

    public bool Equals(TypeSymbolInfo other) => string.Equals(Definition, other.Definition, StringComparison.Ordinal)
                                                && string.Equals(NamespaceDeclaration, other.NamespaceDeclaration, StringComparison.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Name, FullyQualifiedName);
}
