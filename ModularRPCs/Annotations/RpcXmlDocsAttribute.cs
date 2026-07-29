using System;

namespace DanielWillett.ModularRpcs.Annotations;

/// <summary>
/// Tells the source generator to whether or not to generate XML documentation for the given send method(s).
/// This has no effect on dynamically-generated proxy classes, only source-generated types annotated with the <see cref="GenerateRpcSourceAttribute"/>.
/// </summary>
/// <remarks>
/// By default, docs are only generated for <see langword="internal"/>, <see langword="private"/>, or <see langword="private protected"/> functions.
/// This attribute will override that default functionality.
/// <para>
/// It can be placed on the method itself, its containing type, module, or assembly.
/// </para>
/// </remarks>
/// <param name="generateXmlDocs">Whether or not to generate XML docs for this send method.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Module | AttributeTargets.Assembly)]
public sealed class RpcXmlDocsAttribute(bool generateXmlDocs = true) : Attribute
{
    /// <summary>
    /// Whether or not to generate the default XML docs for this send method.
    /// </summary>
    public bool GenerateXmlDocs { get; } = generateXmlDocs;
}