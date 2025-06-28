using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct ReceiveMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly CSharpCompilation Compilation;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcReceiveAttribute Receive;
    public readonly ReceiveMethodInfo Info;

    internal ReceiveMethodSnippetGenerator(SourceProductionContext context, CSharpCompilation compilation, ReceiveMethodInfo method)
    {
        Context = context;
        Method = method.Method;
        Receive = (RpcReceiveAttribute)method.Method.Target;
        Info = method;
        Compilation = compilation;
    }

    // method signature:
    //      object serviceProvider
    //      object targetObject
    //      RpcOverhead overhead
    //      IRpcRouter router
    //      IRpcSerializer serializer
    //      byte* bytes
    //      uint maxSize
    //      CancellationToken token

    public void GenerateMethodBodySnippet(SourceStringBuilder bldr, bool stream)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        bldr.String("bool preCalc = serializer.CanFastReadPrimitives;")
            .String("uint readIndex = 0;")
            .String("int bytesReadTemp;")
            .Empty();


        ParameterHelper.BindParameters(Method.Parameters, out RpcParameterDeclaration[] toInject, out RpcParameterDeclaration[] toBind);

        bool canUseNativeIntArithmitic = Compilation.LanguageVersion >= LanguageVersion.CSharp11;

        bool hasSpVars = false;
        foreach (RpcParameterDeclaration param in toInject)
        {
            switch (param.InjectType)
            {
                default:

                    if (!hasSpVars)
                    {
                        bldr.Build($"global::System.IServiceProvider serviceProviderVar = serviceProvider as global::System.IServiceProvider;");
                        bldr.Build($"global::System.Collections.Generic.IEnumerable<global::System.IServiceProvider> serviceProvidersVar = serviceProvider as global::System.Collections.Generic.IEnumerable<global::System.IServiceProvider>;");
                        hasSpVars = true;
                    }

                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};")
                        .String("if (serviceProviderVar != null)")
                        .String("{").In();

                    bldr.Build($"object service = serviceProviderVar.GetService(typeof({param.Type.GloballyQualifiedName}));")
                        .String("if (service == null)").In()
                            .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));").Out();
                     

                    bldr.Empty()
                        .Build($"arg{param.Index} = ({param.Type.GloballyQualifiedName})service;")
                        .Out()
                        .String("}")
                        .String("else if (serviceProvidersVar != null)")
                        .String("{").In();

                    bldr.Build($"arg{param.Index} = default;")
                        .String("bool found = false;")
                        .String("foreach (global::System.IServiceProvider serviceProviderElement in serviceProvidersVar)")
                        .String("{").In()

                        .Build($"object service = serviceProviderElement.GetService(typeof({param.Type.GloballyQualifiedName}));")
                        .String("if (service == null)").In()
                            .String("continue;").Out()
                        .String("found = true;")
                        .Build($"arg{param.Index} = ({param.Type.GloballyQualifiedName})service;")
                        .Out()
                        .String("}")

                        .String("if (!found)").In()
                            .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));").Out()
                    
                        .Out()
                        .String("}")
                        .String("else")
                        .String("{").In()
                            .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));").Out()
                        .String("}").Out();
                    break;

                case ParameterHelper.AutoInjectType.CancellationToken:
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = token;");
                    break;

                case ParameterHelper.AutoInjectType.RpcInvocationPoint:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IRpcInvocationPoint", "overhead.Rpc");
                    break;

                case ParameterHelper.AutoInjectType.RpcOverhead:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Protocol.RpcOverhead", "overhead");
                    break;

                case ParameterHelper.AutoInjectType.RpcRouter:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Routing.IRpcRouter", "router");
                    break;

                case ParameterHelper.AutoInjectType.RpcSerializer:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer", "serializer");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcConnection:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection", "overhead.SendingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcRemoteConnection:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection", "overhead.SendingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcConnections:
                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection", "overhead.SendingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcRemoteConnections:
                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection", "overhead.SendingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcLocalConnection:
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection", "overhead.ReceivingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcLocalConnections:
                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcLocalConnection", "overhead.ReceivingConnection");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcClientsideConnection:
                    bldr.Build($"global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection __arg{param.Index} = overhead.SendingConnection as global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection ?? overhead.ReceivingConnection as global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection;");
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection", $"__arg{param.Index}");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcServersideConnection:
                    bldr.Build($"global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection __arg{param.Index} = overhead.ReceivingConnection as global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection ?? overhead.SendingConnection as global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection;");
                    Inject(Method, bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection", $"__arg{param.Index}");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcClientsideConnections:
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};")
                        .Build($"if (router.SendingConnection is global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection c1{param.Index})")
                        .String("{").In();

                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection", $"c1{param.Index}", alreadyDefined: true);
                    bldr.Out().String("}")
                        .Build($"else if (router.ReceivingConnection is global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection c2{param.Index})")
                        .String("{").In();

                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcClientsideConnection", $"c2{param.Index}", alreadyDefined: true);
                    bldr.Out().String("}");
                    break;

                case ParameterHelper.AutoInjectType.ModularRpcServersideConnections:
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};")
                        .Build($"if (router.ReceivingConnection is global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection s1{param.Index})")
                        .String("{").In();

                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection", $"s1{param.Index}", alreadyDefined: true);
                    bldr.Out().String("}")
                        .Build($"else if (router.SendingConnection is global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection s2{param.Index})")
                        .String("{").In();

                    InjectCollection(bldr, param, "global::DanielWillett.ModularRpcs.Abstractions.IModularRpcServersideConnection", $"s2{param.Index}", alreadyDefined: true);
                    bldr.Out().String("}");
                    break;

                case ParameterHelper.AutoInjectType.RpcFlags:
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = overhead.Flags;");
                    break;

                case ParameterHelper.AutoInjectType.ServiceProvider:

                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};")
                        .Build($"if (serviceProvider is {param.Type.GloballyQualifiedName} sp{param.Index})")
                        .String("{").In();
                    Inject(Method, bldr, param, "global::System.IServiceProvider", $"sp{param.Index}", alreadyDefined: true);    
                    bldr.Out().String("}")
                        .String("else if (serviceProvider == null)").In()
                            .String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleServiceProviders);").Out()
                        .String("else").In()
                            .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));").Out();
                    break;

                case ParameterHelper.AutoInjectType.ServiceProviders:

                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};")
                        .Build($"if (serviceProvider is global::System.IServiceProvider sp{param.Index})")
                        .String("{").In();
                    InjectCollection(bldr, param, "global::System.IServiceProvider", $"sp{param.Index}", alreadyDefined: true);
                    bldr.Out()
                        .String("}")
                        .String($"else if (serviceProvider is global::System.Collections.Generic.IEnumerable<global::System.IServiceProvider> serviceProviders{param.Index})")
                        .String("{").In();

                    Inject(Method, bldr, param, "global::System.Collections.Generic.IEnumerable<global::System.IServiceProvider>", $"serviceProviders{param.Index}", alreadyDefined: true);
                    bldr.Out()
                        .String("}")
                        .String("else if (serviceProvider == null)")
                        .String("{").In();

                    InjectCollection(bldr, param, "global::System.IServiceProvider", null, alreadyDefined: true);
                    bldr.Out()
                        .String("}")
                        .String("else").In()
                            .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));").Out();
                    break;
            }
        }

        foreach (RpcParameterDeclaration param in toBind)
        {
            bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};");
            ReadValue(bldr, param.Type, "readIndex", $"arg{param.Index}", canUseNativeIntArithmitic, stream);
        }

        if (!Method.IsStatic)
        {
            bldr.String("if (targetObject == null)")
                .In().Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull, \"{TypeHelper.Escape(Method.Type.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));")
                .Out()
                .Empty();

            bldr.Build($"(({Method.Type.Type.GloballyQualifiedName})targetObject).@{Method.Name}(");
        }
        else
        {
            bldr.Empty()
                .Build($"@{Method.Name}(");
        }

        bldr.In();
        foreach (RpcParameterDeclaration param in Method.Parameters)
        {
            if (param.Index == Method.Parameters.Count - 1)
                bldr.Build($"arg{param.Index}");
            else
                bldr.Build($"arg{param.Index}, ");
        }

        bldr.Out()
            .String(");");
        return;

        static void Inject(RpcMethodDeclaration method, SourceStringBuilder bldr, RpcParameterDeclaration param, string baseInterface, string value, bool alreadyDefined = false)
        {
            if (param.Type.Equals(baseInterface))
            {
                if (alreadyDefined)
                    bldr.Build($"arg{param.Index} = {value};");
                else
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = {value};");
            }
            else
            {
                if (!alreadyDefined)
                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};");

                bldr.Build($"if ({value} is {param.Type.GloballyQualifiedName} _arg{param.Index})").In()
                        .Build($"arg{param.Index} = _arg{param.Index};").Out()
                    .String("else").In()
                        .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInfo, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(method.DisplayString)}\"));").Out();
            }
        }

        static void InjectCollection(SourceStringBuilder bldr, RpcParameterDeclaration param, string baseInterface, string? value, bool alreadyDefined = false)
        {
            if (!alreadyDefined)
                bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};");

            if (param.Type.Equals($"global::System.Collections.Generic.IEnumerable<{baseInterface}>")
                || param.Type.Equals($"global::System.Collections.Generic.ICollection<{baseInterface}>")
                || param.Type.Equals($"global::System.Collections.Generic.IList<{baseInterface}>")
                || param.Type.Equals($"global::System.Collections.Generic.IReadOnlyCollection<{baseInterface}>")
                || param.Type.Equals($"global::System.Collections.Generic.IReadOnlyList<{baseInterface}>")
                || param.Type.Equals($"{baseInterface}[]"))
            {
                if (value == null)
                    bldr.Build($"arg{param.Index} = global::System.Array.Empty<{baseInterface}>();");
                else
                    bldr.Build($"arg{param.Index} = {value} == null ? global::System.Array.Empty<{baseInterface}>() : new {baseInterface}[] {{ {value} }};");
            }
            else if (param.Type.Equals($"global::System.ArraySegment<{baseInterface}>"))
            {
                if (value == null)
                {
                    bldr.Preprocessor("#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER")
                        .Build($"arg{param.Index} = global::System.ArraySegment<{baseInterface}>.Empty;")
                        .Preprocessor("#else")
                        .Build($"arg{param.Index} = new global::System.ArraySegment<{baseInterface}>(global::System.Array.Empty<{baseInterface}>());")
                        .Preprocessor("#endif");
                }
                else
                {
                    bldr.Preprocessor("#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER")
                        .Build($"arg{param.Index} = {value} == null ? global::System.ArraySegment<{baseInterface}>.Empty : new global::System.ArraySegment<{baseInterface}>(new {baseInterface}[] {{ {value} }});")
                        .Preprocessor("#else")
                        .Build($"arg{param.Index} = {value} == null ? new global::System.ArraySegment<{baseInterface}>(global::System.Array.Empty<{baseInterface}>()) : new global::System.ArraySegment<{baseInterface}>(new {baseInterface}[] {{ {value} }});")
                        .Preprocessor("#endif");
                }
            }
            else
            {
                if (value == null)
                    bldr.Build($"arg{param.Index} = new {param.Type.GloballyQualifiedName}();");
                else
                    bldr.Build($"arg{param.Index} = {value} == null ? new {param.Type.GloballyQualifiedName}() : new {param.Type.GloballyQualifiedName} {{ {value} }};");
            }
        }
    }

    private static void ReadValue(SourceStringBuilder bldr, TypeSymbolInfo symbolInfo, string offsetVar, string valueVar, bool canUseNativeIntArithmitic, bool stream)
    {
        string args = stream ? "stream, out bytesReadTemp" : $"bytes + {offsetVar}, maxSize - {offsetVar}, out bytesReadTemp";
        TypeSerializationInfo info = symbolInfo.Info;
        switch (info.Type)
        {
            case TypeSerializationInfoType.PrimitiveLike:
            case TypeSerializationInfoType.Value:

                if (!stream && info.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                {
                    switch (info.PrimitiveSerializationMode)
                    {
                        case TypeHelper.QuickSerializeMode.If64Bit:
                            bldr.String("if (preCalc && global::System.IntPtr.Size == 8)");
                            break;
                        case TypeHelper.QuickSerializeMode.If64BitLittleEndian:
                            bldr.String("if (preCalc && global::System.IntPtr.Size == 8 && global::System.BitConverter.IsLittleEndian)");
                            break;
                        case TypeHelper.QuickSerializeMode.IfLittleEndian:
                            bldr.String("if (preCalc && global::System.BitConverter.IsLittleEndian)");
                            break;
                        default: // Always
                            bldr.String("if (preCalc)");
                            break;
                    }

                    bldr.String("{")
                        .In();

                    TypeHelper.PrimitiveLikeType idPrimType = symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.UnderlyingTypeMask;
                    switch (idPrimType)
                    {
                        case TypeHelper.PrimitiveLikeType.Boolean:
                            if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                bldr.Build($"{valueVar} = ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar}] != 0);");
                            else
                                bldr.Build($"{valueVar} = bytes[{offsetVar}] != 0;");

                            bldr.Build($"++{offsetVar};");
                            break;

                        case TypeHelper.PrimitiveLikeType.Byte:
                            if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                bldr.Build($"{valueVar} = ({symbolInfo.GloballyQualifiedName})bytes[{offsetVar}];");
                            else
                                bldr.Build($"{valueVar} = bytes[{offsetVar}];");

                            bldr.Build($"++{offsetVar};");
                            break;

                        case TypeHelper.PrimitiveLikeType.SByte:
                            if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                bldr.Build($"{valueVar} = ({symbolInfo.GloballyQualifiedName})(unchecked ( (sbyte)bytes[{offsetVar}] ));");
                            else
                                bldr.Build($"{valueVar} = unchecked ( (sbyte)bytes[{offsetVar}] );");

                            bldr.Build($"++{offsetVar};");
                            break;

                        default:
                            string bufferName = offsetVar + "BufferLocation";

                            bldr.Build($"byte* {bufferName} = bytes + {offsetVar};");
                            if (idPrimType is TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr)
                                bldr.Build($"if ((nint){bufferName} % 8 == 0)");
                            else
                                bldr.Build($"if ((nint){bufferName} % sizeof({symbolInfo.GloballyQualifiedName}) == 0)");
                            bldr.In().Build($"{valueVar} = *({symbolInfo.GloballyQualifiedName}*){bufferName};").Out()
                                .String("else")
                                .String("{").In();

                            switch (idPrimType)
                            {
                                case TypeHelper.PrimitiveLikeType.Char:
                                case TypeHelper.PrimitiveLikeType.Int16:
                                case TypeHelper.PrimitiveLikeType.UInt16:
                                    bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(*bytes | bytes[1] << 8) );")
                                        .Build($"{offsetVar} += 2u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int32:
                                case TypeHelper.PrimitiveLikeType.UInt32:
                                    bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) );")
                                        .Build($"{offsetVar} += 4u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Single:

                                    bldr.Build($"int {valueVar}Num = *bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);")
                                        .Build($"{valueVar} = *(float*)&{valueVar}Num;")
                                        .Build($"{offsetVar} += 4u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Double:

                                    bldr.Build($"long {valueVar}Num = unchecked ( (long)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (long)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                        .Build($"{valueVar} = *(double*)&{valueVar}Num;")
                                        .Build($"{offsetVar} += 8u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int64:
                                case TypeHelper.PrimitiveLikeType.UInt64:
                                    bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | ({symbolInfo.GloballyQualifiedName})(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                        .Build($"{offsetVar} += 8u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.IntPtr:

                                    if (canUseNativeIntArithmitic)
                                    {
                                        bldr.String("if (global::System.IntPtr.Size == 8)").In()
                                                .Build($"{valueVar} = unchecked ( (nint)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (nint)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );").Out()
                                            .String("else")
                                            .String("{").In()
                                                .Build($"long {valueVar}Num = unchecked ( (long)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (long)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                                .Build($"if ({valueVar}Num > {int.MaxValue} || {valueVar}Num < {int.MinValue})")
                                                .In().String("throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, \"IntPtrParser\")) { ErrorCode = 9 };").Out()
                                                .Empty()
                                                .Build($"{valueVar} = (nint){valueVar}Num;")
                                                .Out()
                                            .String("}");
                                    }
                                    else
                                    {
                                        bldr.Build($"long {valueVar}Num = unchecked ( (long)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (long)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                            .Build($"if (global::System.IntPtr.Size != 8 && {valueVar}Num > {int.MaxValue} || {valueVar}Num < {int.MinValue})")
                                            .In().String("throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, \"IntPtrParser\")) { ErrorCode = 9 };").Out()
                                            .Empty()
                                            .Build($"{valueVar} = (nint){valueVar}Num;");
                                    }

                                    bldr.Build($"{offsetVar} += 8u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.UIntPtr:
                                    
                                    if (canUseNativeIntArithmitic)
                                    {
                                        bldr.String("if (global::System.IntPtr.Size == 8)").In()
                                                .Build($"{valueVar} = unchecked ( (nuint)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (nuint)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );").Out()
                                            .String("else")
                                            .String("{").In()
                                                .Build($"long {valueVar}Num = unchecked ( (ulong)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (ulong)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                                .Build($"if ({valueVar}Num > {uint.MaxValue})")
                                                .In().String("throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, \"UIntPtrParser\")) { ErrorCode = 9 };").Out()
                                                .Empty()
                                                .Build($"{valueVar} = (nuint){valueVar}Num;")
                                                .Out()
                                            .String("}");
                                    }
                                    else
                                    {
                                        bldr.Build($"ulong {valueVar}Num = unchecked ( (ulong)(*bytes | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24) | (ulong)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24) << 32 );")
                                            .Build($"if (global::System.IntPtr.Size != 8 && {valueVar}Num > {uint.MaxValue})")
                                            .In().String("throw new RpcParseException(string.Format(Properties.Exceptions.RpcParseExceptionBufferRunOutNativeIntOverflow, \"UIntPtrParser\")) { ErrorCode = 9 };").Out()
                                            .Empty()
                                            .Build($"{valueVar} = (nuint){valueVar}Num;");
                                    }

                                    bldr.Build($"{offsetVar} += 8u;");
                                    break;
                            }

                            bldr.Out()
                                .String("}");
                            break;
                    }

                    bldr.Out()
                        .String("}");

                    bldr.String("else")
                        .String("{")
                        .In();
                }

                bldr.Build($"{valueVar} = serializer.ReadObject<{symbolInfo.GloballyQualifiedName}>({args});")
                    .Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");

                if (!stream && info.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                {
                    bldr.Out()
                        .String("}");
                }

                break;

            case TypeSerializationInfoType.NullableValue:
                bldr.Build($"{valueVar} = serializer.ReadNullable<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                    .Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.SerializableValue:
                bldr.Build($"{valueVar} = serializer.ReadSerializableObject<{info.SerializableType.GloballyQualifiedName}>({args});")
                    .Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.NullableSerializableValue:
                bldr.Build($"{valueVar} = serializer.ReadNullableSerializableObject<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                    .Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.SerializableCollection:
                if (info.CollectionType.Equals($"{info.SerializableType.GloballyQualifiedName}[]")) // is array
                    bldr.Build($"{valueVar} = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});");
                else
                    bldr.Build($"{valueVar} = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}, {info.CollectionType.GloballyQualifiedName}>({args});");

                bldr.Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                if (info.UnderlyingType.Equals($"global::System.ArraySegment<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ArraySegment<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.UnderlyingType.Equals($"global::System.ReadOnlyMemory<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ReadOnlyMemory<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.UnderlyingType.Equals($"global::System.Memory<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.Memory<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.UnderlyingType.Equals($"global::System.ReadOnlySpan<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ReadOnlySpan<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.UnderlyingType.Equals($"global::System.Span<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.Span<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else
                {
                    bldr.Build($"{valueVar} = serializer.ReadSerializableObjects<{info.SerializableType.GloballyQualifiedName}, {info.UnderlyingType.GloballyQualifiedName}>({args});");
                }
                bldr.Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.NullableSerializableCollection:
                if (info.CollectionType.Equals($"{info.SerializableType.GloballyQualifiedName}[]")) // is array
                    bldr.Build($"{valueVar} = serializer.ReadSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});");
                else
                    bldr.Build($"{valueVar} = serializer.ReadSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}, {info.CollectionType.GloballyQualifiedName}>({args});");

                bldr.Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;

            case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
                if (info.CollectionType.Equals($"global::System.ArraySegment<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ArraySegment<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.CollectionType.Equals($"global::System.ReadOnlyMemory<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ReadOnlyMemory<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.CollectionType.Equals($"global::System.Memory<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.Memory<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.CollectionType.Equals($"global::System.ReadOnlySpan<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.ReadOnlySpan<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else if (info.CollectionType.Equals($"global::System.Span<{info.SerializableType.GloballyQualifiedName}>"))
                {
                    bldr.Build($"{info.SerializableType.GloballyQualifiedName}[] {valueVar}Array = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}>({args});")
                        .Build($"{valueVar} = {valueVar}Array == null ? null : new global::System.Span<{info.SerializableType.GloballyQualifiedName}>({valueVar}Array);");
                }
                else
                {
                    bldr.Build($"{valueVar} = serializer.ReadNullableSerializableObjects<{info.UnderlyingType.GloballyQualifiedName}, {info.CollectionType.GloballyQualifiedName}>({args});");
                }
                bldr.Build($"{offsetVar} += checked ( (uint)bytesReadTemp );");
                break;
        }
    }
}

internal struct ReceiveMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;
    public string ReceiveMethodNameStream;
    public string ReceiveMethodNameBytes;

    public ReceiveMethodInfo(RpcMethodDeclaration method, int overload)
    {
        Method = method;
        Overload = overload;
        ReceiveMethodNameBytes = null!;
        ReceiveMethodNameStream = null!;
    }
}