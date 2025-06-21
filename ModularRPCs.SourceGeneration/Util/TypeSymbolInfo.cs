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
    public string AssemblyQualifiedName { get; }
    public string FileName { get; }
    public bool IsNullable { get; }
    public bool IsValueType { get; }

    public TypeHelper.PrimitiveLikeType PrimitiveLikeType { get; }
    public TypeHelper.PrimitiveLikeType PrimitiveType { get; }

#nullable disable
    public TypeSerializationInfo Info { get; }
#nullable restore

    public TypeSymbolInfo(Compilation compilation, ITypeSymbol typeSymbol, bool createInfo = false)
    {
        IsValueType = typeSymbol.IsValueType;
        IsNullable = typeSymbol.IsNullable(out ITypeSymbol nullableUnderlyingType);
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
        AssemblyQualifiedName = TypeHelper.GetAssemblyQualifiedNameNoVersion(compilation, typeSymbol);

        // easy way to detect enum underlying type changes
        PrimitiveLikeType = TypeHelper.GetPrimitiveLikeType(typeSymbol);
        PrimitiveType = TypeHelper.GetPrimitiveType(typeSymbol);

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
            Info = new TypeSerializationInfo(compilation, typeSymbol);
    }

    public bool Equals(string fullyQualifiedName)
    {
        return string.Equals(fullyQualifiedName, GloballyQualifiedName);
    }

    public override bool Equals(object? obj) => obj is TypeSymbolInfo other && Equals(other);

    public bool Equals(TypeSymbolInfo? other) => other != null && string.Equals(AssemblyQualifiedName, other.AssemblyQualifiedName, StringComparison.Ordinal)
                                                               && IsNullable == other.IsNullable
                                                               && string.Equals(NamespaceDeclaration, other.NamespaceDeclaration, StringComparison.Ordinal)
                                                               && PrimitiveLikeType == other.PrimitiveLikeType;

    public override int GetHashCode() => HashCode.Combine(Name, AssemblyQualifiedName);
}
