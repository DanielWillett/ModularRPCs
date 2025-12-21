using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ModularRPCs.Util;

public class TypeSerializationInfo : IEquatable<TypeSerializationInfo>
{
#nullable disable
    public readonly TypeSerializationInfoType Type;
    public readonly TypeHelper.QuickSerializeMode PrimitiveSerializationMode;
    public readonly TypeSymbolInfo UnderlyingType;
    public readonly TypeSymbolInfo SerializableType;
    public readonly TypeSymbolInfo CollectionType;
    public readonly bool IsMultipleConnections;
    public readonly bool IsSingleConnection;
#nullable restore

    public TypeSerializationInfo(Compilation compilation, ITypeSymbol type)
    {
        string name = type.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
        if (name.Equals("global::System.Void"))
        {
            Type = TypeSerializationInfoType.Void;
            SerializableType = new TypeSymbolInfo(compilation, type);
            return;
        }

        if (type.IsNullable(out ITypeSymbol nullableType))
        {
            if (IsRpcSerializableType(nullableType))
            {
                Type = TypeSerializationInfoType.NullableSerializableValue;
                SerializableType = new TypeSymbolInfo(compilation, nullableType);
                UnderlyingType = SerializableType;
                return;
            }

            name = nullableType.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
            ITypeSymbol? t = GetSerializableEnumerableType(nullableType, name, out bool isNullable);
            if (t != null)
            {
                CollectionType = new TypeSymbolInfo(compilation, nullableType);
                if (isNullable)
                {
                    Type = TypeSerializationInfoType.NullableCollectionNullableSerializableCollection;
                    UnderlyingType = t.IsNullable(out ITypeSymbol s) ? new TypeSymbolInfo(compilation, s) : null;
                }
                else
                {
                    Type = TypeSerializationInfoType.NullableCollectionSerializableCollection;
                    UnderlyingType = new TypeSymbolInfo(compilation, nullableType);
                }
                SerializableType = new TypeSymbolInfo(compilation, t);
            }
            else
            {
                Type = TypeSerializationInfoType.NullableValue;
                SerializableType = new TypeSymbolInfo(compilation, type);
                UnderlyingType = new TypeSymbolInfo(compilation, nullableType);
                IsMultipleConnections = nullableType is INamedTypeSymbol n && GetIsMultipleConnections(n);
                IsSingleConnection = GetIsSingleConnection(nullableType);
            }
        }
        else
        {
            PrimitiveSerializationMode = TypeHelper.CanQuickSerializeType(type);
            if (PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
            {
                Type = TypeSerializationInfoType.PrimitiveLike;
                SerializableType = new TypeSymbolInfo(compilation, type);
            }
            else if (IsRpcSerializableType(type))
            {
                Type = TypeSerializationInfoType.SerializableValue;
                SerializableType = new TypeSymbolInfo(compilation, type);
            }
            else
            {
                ITypeSymbol? t = GetSerializableEnumerableType(type, name, out bool isNullable);

                if (t != null)
                {
                    CollectionType = new TypeSymbolInfo(compilation, type);
                    if (isNullable)
                    {
                        Type = TypeSerializationInfoType.NullableSerializableCollection;
                        SerializableType = new TypeSymbolInfo(compilation, t);
                        UnderlyingType = t.IsNullable(out ITypeSymbol s) ? new TypeSymbolInfo(compilation, s) : null;
                    }
                    else
                    {
                        Type = TypeSerializationInfoType.SerializableCollection;
                        SerializableType = new TypeSymbolInfo(compilation, t);
                    }
                }
                else
                {
                    Type = TypeSerializationInfoType.Value;
                    SerializableType = new TypeSymbolInfo(compilation, type);
                    IsMultipleConnections = type is INamedTypeSymbol n && GetIsMultipleConnections(n);
                    IsSingleConnection = GetIsSingleConnection(type);
                }
            }
        }
    }

    private static bool GetIsMultipleConnections(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface
            && GetIsIEnumerableMultipleConnections(type))
        {
            return true;
        }

        foreach (INamedTypeSymbol intx in type.AllInterfaces)
        {
            if (GetIsIEnumerableMultipleConnections(intx))
                return true;
        }

        return false;
    }

    private static bool GetIsIEnumerableMultipleConnections(INamedTypeSymbol @interface)
    {
        if (@interface is not
            {
                IsGenericType: true,
                ConstructedFrom.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T
                    or SpecialType.System_Collections_Generic_IList_T
                    or SpecialType.System_Collections_Generic_ICollection_T
                    or SpecialType.System_Collections_Generic_IReadOnlyList_T
                    or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            })
        {
            return false;
        }

        ITypeSymbol typeArg = @interface.TypeArguments[0];
        string toString = typeArg.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
        return toString is "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection"
            or "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection"
            or "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection"
            or "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection"
            or "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection";
    }

    private static bool GetIsSingleConnection(ITypeSymbol type)
    {
        return type.Implements("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection");
    }

    private static ITypeSymbol? GetSerializableEnumerableType(ITypeSymbol type, string name, out bool isNullable)
    {
        if (string.Equals(name, "global::System.String", StringComparison.Ordinal))
        {
            isNullable = false;
            return null;
        }

        if (type is IArrayTypeSymbol array)
        {
            return GetRpcSerializableTypeFromEnumerableElement(array.ElementType, out isNullable);
        }

        if (type is INamedTypeSymbol namedType)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                if (namedType is { IsGenericType: true, TypeArguments.Length: 1 }
                    && namedType.ConstructedFrom.IsEqualTo("global::System.Collections.Generic.IEnumerable<T>"))
                {
                    return GetRpcSerializableTypeFromEnumerableElement(namedType.TypeArguments[0], out isNullable);
                }
            }

            if (type.TypeKind == TypeKind.Struct)
            {
                if (namedType is { IsGenericType: true, TypeArguments.Length: 1 })
                {
                    string cfrom = namedType.ConstructedFrom.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
                    if (string.Equals(cfrom, "global::System.Memory<T>")
                        || string.Equals(cfrom, "global::System.ReadOnlyMemory<T>")
                        || string.Equals(cfrom, "global::System.ArraySegment<T>"))
                    {
                        return GetRpcSerializableTypeFromEnumerableElement(namedType.TypeArguments[0], out isNullable);
                    }
                }
            }
        }

        bool isIListNonGeneric = false;
        foreach (INamedTypeSymbol intx in type.AllInterfaces)
        {
            if (intx.IsEqualTo("global::System.Collections.IList"))
                isIListNonGeneric = true;

            if (!intx.IsGenericType
                || intx.TypeArguments.Length != 1
                || !intx.ConstructedFrom.IsEqualTo("global::System.Collections.Generic.IEnumerable<T>"))
                continue;

            return GetRpcSerializableTypeFromEnumerableElement(intx.TypeArguments[0], out isNullable);
        }

        if (isIListNonGeneric && type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType2)
        {
            return GetRpcSerializableTypeFromEnumerableElement(namedType2.TypeArguments[0], out isNullable);
        }

        isNullable = false;
        return null;
    }

    private static ITypeSymbol? GetRpcSerializableTypeFromEnumerableElement(ITypeSymbol elementType, out bool isNullable)
    {
        if (elementType.IsNullable(out ITypeSymbol underlyingType))
        {
            if (IsRpcSerializableType(underlyingType))
            {
                isNullable = true;
                return elementType;
            }
        }
        else if (IsRpcSerializableType(elementType))
        {
            isNullable = false;
            return elementType;
        }

        isNullable = false;
        return null;
    }

    private static bool IsRpcSerializableType(ITypeSymbol symbol)
    {
        return symbol.Implements("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializable")
            && symbol.HasAttribute("global::DanielWillett.ModularRpcs.Serialization.RpcSerializableAttribute");
    }

    /// <inheritdoc />
    public bool Equals(TypeSerializationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type
               && PrimitiveSerializationMode == other.PrimitiveSerializationMode
               && EqualityComparer<TypeSymbolInfo>.Default.Equals(UnderlyingType, other.UnderlyingType)
               && EqualityComparer<TypeSymbolInfo>.Default.Equals(SerializableType, other.SerializableType)
               && EqualityComparer<TypeSymbolInfo>.Default.Equals(CollectionType, other.CollectionType);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is TypeSerializationInfo s && Equals(s);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine((int)Type, (int)PrimitiveSerializationMode, UnderlyingType, SerializableType, CollectionType);
    }
}

public enum TypeSerializationInfoType
{
    /// <summary>
    /// Return type is <see langword="void"/>.
    /// </summary>
    Void,

    /// <summary>
    /// Return type is a non-nullable primitive-like value type.
    /// </summary>
    PrimitiveLike,

    /// <summary>
    /// Value is a standard value or nullable reference type.
    /// </summary>
    Value,

    /// <summary>
    /// Value is a nullable value type.
    /// </summary>
    NullableValue,

    /// <summary>
    /// Value is <see cref="IRpcSerializable"/>.
    /// </summary>
    /// <remarks>Ex: <see cref="IRpcSerializable"/> (where IRpcSerializable is some value type)</remarks>
    SerializableValue,

    /// <summary>
    /// Value is collection of <see cref="IRpcSerializable"/> or nullable reference type.
    /// </summary>
    /// <remarks>Ex: <see cref="IRpcSerializable[]"/> (where IRpcSerializable is some value type)</remarks>
    SerializableCollection,

    /// <summary>
    /// Value is a nullable value type of <see cref="IRpcSerializable"/>.
    /// </summary>
    /// <remarks>Ex: <see cref="IRpcSerializable?"/> (where IRpcSerializable is some value type)</remarks>
    NullableSerializableValue,

    /// <summary>
    /// Value is a collection of nullable value types of <see cref="IRpcSerializable"/>.
    /// </summary>
    /// <remarks>Ex: <see cref="IRpcSerializable?[]"/> (where IRpcSerializable is some value type)</remarks>
    NullableSerializableCollection,

    /// <summary>
    /// Value is a nullable value type collection of <see cref="IRpcSerializable"/> values.
    /// </summary>
    /// <remarks>Ex: <see cref="ArraySegment{IRpcSerializable}?"/> (where IRpcSerializable is some value type)</remarks>
    NullableCollectionSerializableCollection,

    /// <summary>
    /// Value is a nullable value type collection of nullable value types of <see cref="IRpcSerializable"/>.
    /// </summary>
    /// <remarks>Ex: <see cref="ArraySegment{IRpcSerializable?}?"/> (where IRpcSerializable is some value type)</remarks>
    NullableCollectionNullableSerializableCollection,
}