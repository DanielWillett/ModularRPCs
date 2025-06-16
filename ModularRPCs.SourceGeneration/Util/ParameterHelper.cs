using Microsoft.CodeAnalysis;
using System;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

public static class ParameterHelper
{
    public static AutoInjectType GetAutoInjectType(INamedTypeSymbol? parameter)
    {
        if (parameter == null)
            return AutoInjectType.NotInjected;

        string name = parameter.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);

        switch (parameter.TypeKind)
        {
            case TypeKind.Enum:
                
                if (name.Equals("global::DanielWillett.ModularRpcs.Protocol.RpcFlags", StringComparison.Ordinal))
                    return AutoInjectType.RpcFlags;

                break;

            case TypeKind.Struct:

                if (name.Equals("global::System.Threading.CancellationToken", StringComparison.Ordinal))
                    return AutoInjectType.CancellationToken;

                break;
            
            case TypeKind.Interface:

                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IRpcInvocationPoint", StringComparison.Ordinal))
                    return AutoInjectType.RpcInvocationPoint;

                if (name.Equals("global::DanielWillett.ModularRpcs.Routing.IRpcRouter", StringComparison.Ordinal))
                    return AutoInjectType.RpcRouter;

                if (name.Equals("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer", StringComparison.Ordinal))
                    return AutoInjectType.RpcSerializer;

                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcConnection;
                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcLocalConnection;
                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcRemoteConnection;
                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcClientsideConnection;
                if (name.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcServersideConnection;

                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection>", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcConnections;
                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection>", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcLocalConnections;
                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection>", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcRemoteConnections;
                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection>", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcClientsideConnections;
                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection>", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcServersideConnections;

                if (name.Equals("global::System.IServiceProvider", StringComparison.Ordinal))
                    return AutoInjectType.ServiceProvider;

                if (name.Equals("global::System.Collections.Generic.IEnumerable<global::System.IServiceProvider>", StringComparison.Ordinal))
                    return AutoInjectType.ServiceProviders;
                break;

            case TypeKind.Class:
                
                if (name.Equals("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead", StringComparison.Ordinal))
                    return AutoInjectType.RpcOverhead;

                break;
        }

        return AutoInjectType.NotInjected;
    }

    public enum AutoInjectType
    {
        NotInjected,
        RpcInvocationPoint,
        CancellationToken,
        RpcOverhead,
        RpcRouter,
        RpcSerializer,
        ModularRpcConnection,
        ModularRpcRemoteConnection,
        ModularRpcLocalConnection,
        ModularRpcClientsideConnection,
        ModularRpcServersideConnection,
        ModularRpcConnections,
        ModularRpcRemoteConnections,
        ModularRpcLocalConnections,
        ModularRpcClientsideConnections,
        ModularRpcServersideConnections,
        RpcFlags,
        ServiceProvider,
        ServiceProviders
    }

    internal static void BindParameters(
        EquatableList<RpcParameterDeclaration> parameters,
        out RpcParameterDeclaration[] toInject,
        out RpcParameterDeclaration[] toBind)
    {
        if (parameters.Count == 0)
        {
            toInject = Array.Empty<RpcParameterDeclaration>();
            toBind = Array.Empty<RpcParameterDeclaration>();
            return;
        }

        int injectionParams = 0;
        int bindParams = 0;
        for (int i = 0; i < parameters.Count; ++i)
        {
            RpcParameterDeclaration parameterInfo = parameters[i];

            bool isInjected = parameterInfo.IsManualInjected || parameterInfo.InjectType != AutoInjectType.NotInjected;

            injectionParams += isInjected ? 1 : 0;
            bindParams += isInjected ? 0 : 1;
        }

        RpcParameterDeclaration[] toInj = new RpcParameterDeclaration[injectionParams];
        RpcParameterDeclaration[] toBnd = new RpcParameterDeclaration[bindParams];
        int injInd = -1, bndInd = -1;
        for (int i = 0; i < parameters.Count; ++i)
        {
            RpcParameterDeclaration parameterInfo = parameters[i];

            bool isInjected = parameterInfo.IsManualInjected || parameterInfo.InjectType != AutoInjectType.NotInjected;

            if (isInjected)
                toInj[++injInd] = parameters[i];
            else
                toBnd[++bndInd] = parameters[i];
        }

        toBind = toBnd;
        toInject = toInj;
    }
}
