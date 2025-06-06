using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;

namespace DanielWillett.ModularRpcs.SourceGeneration;

internal record RpcMethodDeclaration
{
    public required string Name { get; init; }
    public required string Definition { get; init; }
    public required string XmlDocs { get; init; }
    public bool IsReceive => Target is RpcReceiveAttribute;
    public bool IsSend => Target is RpcSendAttribute;
    public required Accessibility Visibility { get; init; }
    public required RpcTargetAttribute Target { get; init; }
    public required RpcClassDeclaration Type { get; init; }
    public required EquatableList<RpcParameterDeclaration> Parameters { get; init; }
    public required TypeSymbolInfo ReturnType { get; init; }
}