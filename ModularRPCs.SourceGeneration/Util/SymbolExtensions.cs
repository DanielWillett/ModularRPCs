using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

internal static class SymbolExtensions
{
    public static bool IsEqualTo(this ITypeSymbol? type, string globalTypeName)
    {
        return type != null && type
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Equals(globalTypeName, StringComparison.Ordinal);
    }
}
