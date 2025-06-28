using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration;

internal record RpcMethodDeclaration
{
    public required string Name { get; init; }
    public required string Definition { get; init; }
    public required string XmlDocs { get; init; }
    public required string DisplayString { get; init; }
    public required bool IsFireAndForget { get; init; }
    public required bool IsBroadcast { get; init; }
    public bool IsReceive => Target is RpcReceiveAttribute;
    public bool IsSend => Target is RpcSendAttribute;
    public required bool IsStatic { get; init; }
    public required Accessibility Visibility { get; init; }
    public required RpcTargetAttribute Target { get; init; }
    public required RpcClassDeclaration Type { get; init; }
    public required EquatableList<RpcParameterDeclaration> Parameters { get; init; }
    public required TypeSymbolInfo ReturnType { get; init; }
    public required int SignatureHash { get; init; }
    public required bool ForceSignatureCheck { get; init; }
    public required TimeSpan Timeout { get; init; }
}