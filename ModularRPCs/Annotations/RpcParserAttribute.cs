using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Specify a <see cref="IBinaryTypeParser"/> to use for the type this attribute is decorating.
/// <para>Serializers can either have an empty constructor or a constructor with only a <see cref="SerializationConfiguration"/> parameter.</para>
/// </summary>
/// <remarks>Must call <see cref="SerializationHelper.RegisterParserAttributes(IDictionary{Type, IBinaryTypeParser}, SerializationConfiguration, Assembly)"/> (or one of it's overloads) when configuring the serializer for all assemblies registering custom parsers.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RpcParserAttribute(Type parserType) : Attribute
{
    /// <summary>
    /// Type of the <see cref="IBinaryTypeParser"/> to use for the type this attribute is decorating.
    /// </summary>
    public Type ParserType { get; } = parserType;
}