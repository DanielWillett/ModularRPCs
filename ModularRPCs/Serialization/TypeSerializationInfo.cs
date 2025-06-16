using DanielWillett.ModularRpcs.Reflection;
using System;

namespace DanielWillett.ModularRpcs.Serialization;
internal readonly struct TypeSerializationInfo
{
#nullable disable
    public readonly TypeSerializationInfoType Type;
    public readonly Type UnderlyingType;
    public readonly Type SerializableType;
    public readonly Type CollectionType;
#nullable restore

    public TypeSerializationInfo(Type type)
    {
        if (type == typeof(void))
        {
            Type = TypeSerializationInfoType.Void;
            SerializableType = typeof(void);
            return;
        }

        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            if (SerializerGenerator.IsRpcSerializableType(nullableType))
            {
                Type = TypeSerializationInfoType.NullableSerializableValue;
                SerializableType = nullableType;
                UnderlyingType = nullableType;
                return;
            }

            Type? t = TypeUtility.GetSerializableEnumerableType(nullableType, out bool isNullable);

            if (t != null)
            {
                CollectionType = nullableType;
                if (isNullable)
                {
                    Type = TypeSerializationInfoType.NullableCollectionNullableSerializableCollection;
                    SerializableType = t;
                    UnderlyingType = Nullable.GetUnderlyingType(t);
                }
                else
                {
                    Type = TypeSerializationInfoType.NullableCollectionSerializableCollection;
                    SerializableType = t;
                    UnderlyingType = nullableType;
                }
            }
            else
            {
                Type = TypeSerializationInfoType.NullableValue;
                SerializableType = type;
                UnderlyingType = nullableType;
            }
        }
        else
        {
            if (SerializerGenerator.CanQuickSerializeType(type))
            {
                Type = TypeSerializationInfoType.PrimitiveLike;
                SerializableType = type;
            }
            else if (SerializerGenerator.IsRpcSerializableType(type))
            {
                Type = TypeSerializationInfoType.SerializableValue;
                SerializableType = type;
            }
            else
            {
                Type? t = TypeUtility.GetSerializableEnumerableType(type, out bool isNullable);

                if (t != null)
                {
                    CollectionType = type;
                    if (isNullable)
                    {
                        Type = TypeSerializationInfoType.NullableSerializableCollection;
                        SerializableType = t;
                        UnderlyingType = Nullable.GetUnderlyingType(t);
                    }
                    else
                    {
                        Type = TypeSerializationInfoType.SerializableCollection;
                        SerializableType = t;
                    }
                }
                else
                {
                    Type = TypeSerializationInfoType.Value;
                    SerializableType = type;
                }
            }
        }
    }
}

internal enum TypeSerializationInfoType
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