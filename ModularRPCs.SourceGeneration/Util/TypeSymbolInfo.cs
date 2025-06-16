using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

public class TypeSymbolInfo : IEquatable<TypeSymbolInfo>, IEquatable<string>
{
    public string Name { get; }
    public string? NamespaceDeclaration { get; }

    /// <summary>
    /// Doesn't include '<c>global::</c>'.
    /// </summary>
    public string FullyQualifiedName { get; }

    /// <summary>
    /// Includes '<c>global::</c>'.
    /// </summary>
    public string GloballyQualifiedName { get; }
    public string FileName { get; }
    public bool IsNullable { get; }

#nullable disable
    public TypeSerializationInfo Info { get; }
#nullable restore

    public TypeSymbolInfo(ITypeSymbol typeSymbol, bool createInfo = false)
    {
        IsNullable = typeSymbol.IsNullable();
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
        FullyQualifiedName = typeSymbol.ToDisplayString(NullableFlowState.NotNull, CustomFormats.FullTypeNameFormat);
        GloballyQualifiedName = typeSymbol.ToDisplayString(NullableFlowState.NotNull, CustomFormats.FullTypeNameWithGlobalFormat);

        // for some reason it ignores keyword preferences for these two types
        if (GloballyQualifiedName.Equals("nint", StringComparison.Ordinal)
            && typeSymbol is { Name: "IntPtr", ContainingNamespace.Name: "System" })
        {
            IsNullable = false;
            FullyQualifiedName = "System.IntPtr";
            GloballyQualifiedName = "global::System.IntPtr";
        }
        else if (GloballyQualifiedName.Equals("nuint", StringComparison.Ordinal)
                 && typeSymbol is { Name: "UIntPtr", ContainingNamespace.Name: "System" })
        {
            IsNullable = false;
            FullyQualifiedName = "System.UIntPtr";
            GloballyQualifiedName = "global::System.UIntPtr";
        }

        if (createInfo)
            Info = new TypeSerializationInfo(typeSymbol);
    }

    public bool Equals(string fullyQualifiedName)
    {
        return string.Equals(fullyQualifiedName, GloballyQualifiedName);
    }

    public override bool Equals(object? obj) => obj is TypeSymbolInfo other && Equals(other);

    public bool Equals(TypeSymbolInfo? other) => other != null && string.Equals(FullyQualifiedName, other.FullyQualifiedName, StringComparison.Ordinal)
                                                               && IsNullable == other.IsNullable
                                                               && string.Equals(NamespaceDeclaration, other.NamespaceDeclaration, StringComparison.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Name, FullyQualifiedName);
}
