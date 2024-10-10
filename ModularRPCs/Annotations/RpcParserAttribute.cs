using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Serialization;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using DanielWillett.ReflectionTools;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Specify a <see cref="IBinaryTypeParser"/> to use for the type this attribute is decorating or a value type for the <see cref="IBinaryTypeParser"/> this attribute is decorating.
/// <para>Parsers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</para>
/// <para>Parsers implementing <see cref="IArrayBinaryTypeParser{T}"/> will be properly registered with their enumerable types instead of their value types. In this case supply the value as the type.</para>
/// <para>Any types nested within the parser will be registered if they implement <see cref="IBinaryTypeParser"/> unless they're decorated with an <see cref="IgnoreAttribute"/>.</para>
/// </summary>
/// <remarks>Must call <see cref="SerializationHelper.RegisterParserAttributes(IDictionary{System.Type, IBinaryTypeParser}, SerializationConfiguration, Assembly)"/> (or one of it's overloads) when configuring the serializer for all assemblies registering custom parsers.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
[BaseTypeRequired(typeof(IBinaryTypeParser))]
public sealed class RpcParserAttribute(Type type) : Attribute
{
    /// <summary>
    /// Type of the <see cref="IBinaryTypeParser"/> to use for the type this attribute is decorating, or the type of value this parser is for.
    /// </summary>
    public Type Type { get; } = type;
}