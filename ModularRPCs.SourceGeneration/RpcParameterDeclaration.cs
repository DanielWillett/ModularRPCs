using Microsoft.CodeAnalysis;
using ModularRPCs.Util;

namespace ModularRPCs;

public record RpcParameterDeclaration
{
    public required string Name { get; init; }
    public required TypeSymbolInfo Type { get; init; }
    public required int Index { get; init; }
    public required ParameterHelper.AutoInjectType InjectType { get; init; }
    public required bool IsManualInjected { get; init; }
    public required string Definition { get; init; }
    public required RefKind RefKind { get; init; }
    public required ParameterHelper.RawByteInjectType RawInjectType { get; init; }
}