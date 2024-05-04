using DanielWillett.ModularRpcs.Protocol;
using JetBrains.Annotations;
using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Place this attribute on a identifier implementation for <see cref="IRpcObject{T}"/> to prevent automatic checks from using the underlying field.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RpcDontUseBackingFieldAttribute : Attribute;

/// <summary>
/// Place this attribute on a field in an <see cref="IRpcObject{T}"/> to hint at which field is the backing field for <see cref="IRpcObject{T}.Identifier"/>.
/// </summary>
/// <remarks>Fields of names: <c>[m][_]identifier</c> and <c>[m][_]id</c> (case-insensitive) will already be checked, along with auto property backing fields: <c>&lt;Identifier&gt;k__BackingField</c> and <c>&lt;DanielWillett.ModularRpcs.Protocol.IRpcObject&lt;{T}&gt;.Identifier&gt;k__BackingField</c>. Put a <see cref="RpcDontUseBackingFieldAttribute"/> on the property or backing field to prevent the backing field from being used.</remarks>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RpcIdentifierBackingFieldAttribute : Attribute;