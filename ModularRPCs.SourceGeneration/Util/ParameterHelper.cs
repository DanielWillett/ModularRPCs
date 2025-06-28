using Microsoft.CodeAnalysis;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;

public static class ParameterHelper
{
    public static AutoInjectType GetAutoInjectType(ITypeSymbol? parameter)
    {
        if (parameter == null)
            return AutoInjectType.NotInjected;

        if (parameter is IArrayTypeSymbol arrayType)
        {
            if (!arrayType.IsSZArray)
            {
                return AutoInjectType.NotInjected;
            }

            string element = arrayType.ElementType.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
            if (element.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection"))
                return AutoInjectType.ModularRpcConnections;
            if (element.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection"))
                return AutoInjectType.ModularRpcLocalConnections;
            if (element.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection"))
                return AutoInjectType.ModularRpcRemoteConnections;
            if (element.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection"))
                return AutoInjectType.ModularRpcClientsideConnections;
            if (element.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection"))
                return AutoInjectType.ModularRpcServersideConnections;
            if (element.Equals("global::System.IServiceProvider"))
                return AutoInjectType.ServiceProviders;

            return AutoInjectType.NotInjected;
        }
        
        switch (parameter.TypeKind)
        {
            case TypeKind.Enum:

                string name = parameter.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);

                if (name.Equals("global::DanielWillett.ModularRpcs.Protocol.RpcFlags", StringComparison.Ordinal))
                    return AutoInjectType.RpcFlags;

                break;

            case TypeKind.Struct:

                name = parameter.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);

                if (name.Equals("global::System.Threading.CancellationToken", StringComparison.Ordinal))
                    return AutoInjectType.CancellationToken;

                foreach (INamedTypeSymbol intx in parameter.AllInterfaces)
                {
                    AutoInjectType inject = HandleInterface(intx);
                    if (inject != AutoInjectType.NotInjected)
                        return inject;
                }
                break;

            case TypeKind.Interface:
                return HandleInterface(parameter);

            case TypeKind.Class:

                name = parameter.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);

                for (ITypeSymbol? baseType = parameter; baseType is { SpecialType: not SpecialType.System_Object and not SpecialType.System_ValueType }; baseType = baseType.BaseType)
                {
                    if (name.Equals("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead", StringComparison.Ordinal))
                        return AutoInjectType.RpcOverhead;
                }

                foreach (INamedTypeSymbol intx in parameter.AllInterfaces)
                {
                    AutoInjectType inject = HandleInterface(intx);
                    if (inject != AutoInjectType.NotInjected)
                        return inject;
                }

                break;
        }

        return AutoInjectType.NotInjected;

        static AutoInjectType HandleInterface(ITypeSymbol parameter)
        {
            if (parameter is INamedTypeSymbol
                {
                    IsGenericType: true,
                    ConstructedFrom.SpecialType:
                        SpecialType.System_Collections_Generic_IEnumerable_T
                        or SpecialType.System_Collections_Generic_IList_T
                        or SpecialType.System_Collections_Generic_IReadOnlyList_T
                        or SpecialType.System_Collections_Generic_ICollection_T
                        or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
                } n)
            {
                string elementType = n.TypeArguments[0].ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);

                if (elementType.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcConnections;
                if (elementType.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcLocalConnections;
                if (elementType.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcRemoteConnections;
                if (elementType.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcClientsideConnections;
                if (elementType.Equals("global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection", StringComparison.Ordinal))
                    return AutoInjectType.ModularRpcServersideConnections;
                if (elementType.Equals("global::System.IServiceProvider", StringComparison.Ordinal))
                    return AutoInjectType.ServiceProviders;

                return AutoInjectType.NotInjected;
            }

            string name = parameter.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
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

            if (name.Equals("global::System.IServiceProvider", StringComparison.Ordinal))
                return AutoInjectType.ServiceProvider;

            return AutoInjectType.NotInjected;
        }
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

    internal static int GetMethodSignatureHash(Compilation compilation, IMethodSymbol method, EquatableList<RpcParameterDeclaration> parameters)
    {
        BindParameters(parameters, out _, out RpcParameterDeclaration[] toBind);

        if (toBind.Length == 0)
            return 0;

        ITypeSymbol[] types = new ITypeSymbol[toBind.Length];
        int len = 0;
        for (int i = 0; i < toBind.Length; ++i)
        {
            IParameterSymbol parameter = method.Parameters[toBind[i].Index];
            ITypeSymbol type = parameter.Type.OriginalDefinition;

            if (type.SpecialType != SpecialType.System_String)
            {
                INamedTypeSymbol? intxType;
                if (type.TypeKind == TypeKind.Interface && type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T } n)
                {
                    intxType = n;
                }
                else
                {
                    intxType = type.GetImplementation(
                        x => x.IsGenericType && x.ConstructedFrom is { SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T }
                    );
                }

                if (intxType != null)
                    type = compilation.CreateArrayTypeSymbol(intxType.TypeArguments[0]);
            }

            types[i] = type;
            len += Encoding.UTF8.GetByteCount(type.MetadataName);
        }

        byte[] toHash = new byte[len];
        int index = 0;
        for (int i = 0; i < types.Length; ++i)
        {
            ITypeSymbol type = types[i];
            string typeName = type.MetadataName;
            index += Encoding.UTF8.GetBytes(typeName, 0, typeName.Length, toHash, index);
        }

        byte[] hash;
        using (SHA1 sha1 = SHA1.Create())
            hash = sha1.ComputeHash(toHash);

        uint h1, h2, h3, h4, h5;
        if (BitConverter.IsLittleEndian)
        {
            h1 = Unsafe.ReadUnaligned<uint>(ref hash[00]);
            h2 = Unsafe.ReadUnaligned<uint>(ref hash[04]);
            h3 = Unsafe.ReadUnaligned<uint>(ref hash[08]);
            h4 = Unsafe.ReadUnaligned<uint>(ref hash[12]);
            h5 = Unsafe.ReadUnaligned<uint>(ref hash[16]);
        }
        else
        {
            h1 = (uint)(hash[00] << 24 | hash[01] << 16 | hash[02] << 8 | hash[03]);
            h2 = (uint)(hash[04] << 24 | hash[05] << 16 | hash[06] << 8 | hash[07]);
            h3 = (uint)(hash[08] << 24 | hash[09] << 16 | hash[10] << 8 | hash[11]);
            h4 = (uint)(hash[12] << 24 | hash[13] << 16 | hash[14] << 8 | hash[15]);
            h5 = (uint)(hash[16] << 24 | hash[17] << 16 | hash[18] << 8 | hash[19]);
        }

        return unchecked((int)(h1 + ((h2 >> 6) | (h2 << 26)) + ((h3 >> 12) | (h3 << 20)) + ((h4 >> 18) | (h4 << 14)) + ((h5 >> 24) | (h5 << 8))));
    }
}
