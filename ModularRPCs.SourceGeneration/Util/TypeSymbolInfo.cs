using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

public class TypeSymbolInfo : IEquatable<TypeSymbolInfo>, IEquatable<string>
{
    public string Name { get; }
    public string? Namespace { get; }

    /// <summary>
    /// Doesn't include '<c>global::</c>'.
    /// </summary>
    public string FullyQualifiedName { get; }

    /// <summary>
    /// Includes '<c>global::</c>'.
    /// </summary>
    public string GloballyQualifiedName { get; }
    public string AssemblyQualifiedName { get; }
    public bool IsNullable { get; }
    public bool IsValueType { get; }

    public TypeHelper.PrimitiveLikeType PrimitiveLikeType { get; }
    public TypeHelper.PrimitiveLikeType PrimitiveType { get; }
    public bool IsNumericNativeInt { get; }

#nullable disable
    public TypeSerializationInfo Info { get; }
#nullable restore

    public TypeSymbolInfo(Compilation compilation, ITypeSymbol typeSymbol, bool createInfo = false)
    {
        IsValueType = typeSymbol.IsValueType;
        IsNullable = typeSymbol.IsNullable();
        Name = typeSymbol.Name;
        Namespace = typeSymbol.ContainingNamespace?.ToDisplayString(CustomFormats.FullTypeNameFormat);
        if (typeSymbol.SpecialType == SpecialType.System_Void)
        {
            FullyQualifiedName = "void";
            GloballyQualifiedName = "void";
        }
        else
        {
            FullyQualifiedName = typeSymbol.ToDisplayString(NullableFlowState.NotNull, CustomFormats.FullTypeNameFormat);
            GloballyQualifiedName = typeSymbol.ToDisplayString(NullableFlowState.NotNull, CustomFormats.FullTypeNameWithGlobalFormat);
        }
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
            IsNumericNativeInt = true;
        }
        else if (GloballyQualifiedName.Equals("nuint", StringComparison.Ordinal)
                 && typeSymbol is { Name: "UIntPtr", ContainingNamespace.Name: "System" })
        {
            IsNullable = false;
            FullyQualifiedName = "System.UIntPtr";
            GloballyQualifiedName = "global::System.UIntPtr";
            IsNumericNativeInt = true;
        }

        if (createInfo)
            Info = new TypeSerializationInfo(compilation, typeSymbol);
    }

    public bool Equals(string fullyQualifiedName)
    {
        if (string.Equals(fullyQualifiedName, "global::System.Void", StringComparison.Ordinal))
        {
            return string.Equals(FullyQualifiedName, "void", StringComparison.Ordinal);
        }

        return string.Equals(fullyQualifiedName, GloballyQualifiedName);
    }

    public override bool Equals(object? obj) => obj is TypeSymbolInfo other && Equals(other);

    public bool Equals(TypeSymbolInfo? other) => other != null && string.Equals(AssemblyQualifiedName, other.AssemblyQualifiedName, StringComparison.Ordinal)
                                                               && IsNullable == other.IsNullable
                                                               && PrimitiveLikeType == other.PrimitiveLikeType
                                                               && IsValueType == other.IsValueType
                                                               && Equals(Info, other.Info);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = AssemblyQualifiedName.GetHashCode();
            hashCode = (hashCode * 397) ^ IsNullable.GetHashCode();
            hashCode = (hashCode * 397) ^ IsValueType.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)PrimitiveLikeType;
            hashCode = (hashCode * 397) ^ (Info != null ? Info.GetHashCode() : 0);
            return hashCode;
        }
    }

}
