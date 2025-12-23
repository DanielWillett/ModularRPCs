using DanielWillett.ModularRpcs.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModularRPCs.Util;
#if !ROSLYN_4_0_OR_GREATER
using SourceProductionContext = Microsoft.CodeAnalysis.GeneratorExecutionContext;
#endif

namespace ModularRPCs.Generators;

internal readonly struct ReceiveMethodSnippetGenerator
{
    // will be GeneratorExecutionContext if !ROSLYN_4_0_OR_GREATER (see usings)
    public readonly SourceProductionContext Context;
    public readonly CSharpCompilation Compilation;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcClassDeclaration Class;
    public readonly RpcReceiveAttribute Receive;
    public readonly ReceiveMethodInfo Info;

    internal ReceiveMethodSnippetGenerator(SourceProductionContext context, CSharpCompilation compilation, ReceiveMethodInfo method, RpcClassDeclaration @class)
    {
        Context = context;
        Method = method.Method;
        Receive = (RpcReceiveAttribute)method.Method.Target;
        Info = method;
        Compilation = compilation;
        Class = @class;
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

    // or (for stream or raw stream):
    //      object? serviceProvider,
    //      object? targetObject,
    //      RpcOverhead overhead,
    //      IRpcRouter router,
    //      IRpcSerializer serializer,
    //      Stream stream,
    //      CancellationToken token

    // or (for raw byte methods):
    //      object? serviceProvider,
    //      object? targetObject,
    //      RpcOverhead overhead,
    //      IRpcRouter router,
    //      IRpcSerializer serializer,
    //      ReadOnlyMemory<byte> rawData,
    //      bool canTakeOwnership,
    //      CancellationToken token*/

    public void GenerateMethodBodySnippet(SourceStringBuilder bldr, bool stream)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        ParameterHelper.BindParameters(Method.Parameters, out RpcParameterDeclaration[] toInject, out RpcParameterDeclaration[] toBind);

        if (toBind.Length > 0 && !Method.Target.Raw)
        {
            bldr.String("bool preCalc = serializer.CanFastReadPrimitives;")
                .String("uint readIndex = 0;")
                .String("int bytesReadTemp;");
        }

        bool hasReturnValue = !Method.ReturnType.Equals("global::System.Void");
        if (hasReturnValue)
        {
            bldr.Build($"{Method.ReturnType.GloballyQualifiedName} rtnValue;");
        }

#if ROSLYN_4_3_OR_GREATER
        bool canUseNativeIntArithmitic = Compilation.LanguageVersion >= LanguageVersion.CSharp11;
#else
        const bool canUseNativeIntArithmitic = false;
#endif

        bool hasSpVars = false;
        foreach (RpcParameterDeclaration param in toInject)
        {
            bldr.Empty();
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
                        .String("}");
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

        bool isFixed = false;
        int byteDataRefArg = -1;
        if (!Method.Target.Raw)
        {
            foreach (RpcParameterDeclaration param in toBind)
            {
                bldr.Empty();
                bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index};");
                ReadValue(bldr, param.Type, "readIndex", $"arg{param.Index}", canUseNativeIntArithmitic, stream);
            }
        }
        else
        {
            if (stream)
            {
                bldr.String("bool canTakeOwnership = true;");
            }
            bool hasByteCount = false,
                 hasBytes = false;
            int canTakeOwnershipArg = -1;
            foreach (RpcParameterDeclaration param in toBind)
            {
                bldr.Empty();

                switch (param.Type.PrimitiveLikeType)
                {
                    case TypeHelper.PrimitiveLikeType.Boolean:

                        if (canTakeOwnershipArg >= 0)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleCanTakeOwnership);");
                            continue;
                        }
                        
                        canTakeOwnershipArg = param.Index;
                        continue;

                    case TypeHelper.PrimitiveLikeType.SByte:
                    case TypeHelper.PrimitiveLikeType.Byte:
                    case TypeHelper.PrimitiveLikeType.Int16:
                    case TypeHelper.PrimitiveLikeType.UInt16:
                    case TypeHelper.PrimitiveLikeType.Int32:
                    case TypeHelper.PrimitiveLikeType.UInt32:
                    case TypeHelper.PrimitiveLikeType.Int64:
                    case TypeHelper.PrimitiveLikeType.UInt64:
                    case TypeHelper.PrimitiveLikeType.IntPtr:
                    case TypeHelper.PrimitiveLikeType.UIntPtr:

                        if (hasByteCount)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleByteCount);");
                            continue;
                        }

                        hasByteCount = true;
                        string typeName = param.Type.GloballyQualifiedName;
                        if (!canUseNativeIntArithmitic && param.Type.PrimitiveLikeType is TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr && param.Type.IsNumericNativeInt)
                        {
                            typeName = param.Type.PrimitiveLikeType == TypeHelper.PrimitiveLikeType.IntPtr ? "nint" : "nuint";
                        }

                        if (stream)
                            bldr.Build($"{typeName} arg{param.Index} = checked ( ({typeName})overhead.MessageSize );");
                        else
                            bldr.Build($"{typeName} arg{param.Index} = checked ( ({typeName})rawData.Length );");
                        continue;

                    default:
                        if (param.RawInjectType is ParameterHelper.RawByteInjectType.None)
                        {
                            bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = default;")
                                .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInvalidRawParameter, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\"));");
                            continue;
                        }

                        if (hasBytes)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleByteData);");
                            continue;
                        }

                        hasBytes = true;
                        switch (param.RawInjectType)
                        {
                            case ParameterHelper.RawByteInjectType.Pointer:
                                if (stream)
                                {
                                    bldr.String("byte[] rawData = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream);")
                                        .Build($"fixed (byte* arg{param.Index} = rawData)");
                                }
                                else
                                {
                                    bldr.Build($"fixed (byte* arg{param.Index} = rawData.Span)");
                                }

                                bldr.String("{")
                                    .In()
                                    .String("canTakeOwnership = false;");
                                isFixed = true;
                                break;

                            case ParameterHelper.RawByteInjectType.Array:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream);");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromMemory(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.Stream:
                                if (stream)
                                {
                                    if (param.Type.Equals("global::System.IO.MemoryStream"))
                                    {
                                        bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::System.IO.MemoryStream(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(rawData, canTakeOwnership, out canTakeOwnership));");
                                    }
                                    else
                                    {
                                        bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::DanielWillett.ModularRpcs.Data.PassthroughReadStream(stream, overhead.MessageSize);")
                                            .String("canTakeOwnership = false;");
                                    }
                                }
                                else
                                {
                                    bldr.Build($"global::System.ArraySegment<byte> _arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArraySegmentFromMemory(rawData, canTakeOwnership, out canTakeOwnership);")
                                        .Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::System.IO.MemoryStream(_arg{param.Index}.Array, _arg{param.Index}.Offset, _arg{param.Index}.Count);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.Memory:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsMemory<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsMemory<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArraySegmentFromMemory(rawData, canTakeOwnership, out canTakeOwnership));");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.ReadOnlyMemory:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsMemory<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = rawData;");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.Span:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsSpan<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsSpan<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArraySegmentFromMemory(rawData, canTakeOwnership, out canTakeOwnership));");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.ReadOnlySpan:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::System.MemoryExtensions.AsSpan<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = rawData.Span;");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.ArraySegment:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::System.ArraySegment<byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArraySegmentFromMemory(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.IList:
                            case ParameterHelper.RawByteInjectType.ICollection:
                            case ParameterHelper.RawByteInjectType.IEnumerable:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream);");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetIListFromMemory(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.IReadOnlyList:
                            case ParameterHelper.RawByteInjectType.IReadOnlyCollection:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream);");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetIReadOnlyListFromMemory(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.List:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetListFromStream(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetListFromMemory(rawData, canTakeOwnership, out canTakeOwnership);");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.ArrayList:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayListFromStream(overhead.MessageSize, stream);");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayListFromMemory(rawData);");
                                }
                                bldr.String("canTakeOwnership = true;");
                                break;

                            case ParameterHelper.RawByteInjectType.ReadOnlyCollection:
                                if (stream)
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::System.Collections.ObjectModel.ReadOnlyCollection<System.Byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetListFromStream(overhead.MessageSize, stream));");
                                }
                                else
                                {
                                    bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::System.Collections.ObjectModel.ReadOnlyCollection<System.Byte>(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetListFromMemory(rawData, canTakeOwnership, out canTakeOwnership));");
                                }
                                break;

                            case ParameterHelper.RawByteInjectType.ByRefByte:
                                if (stream)
                                {
                                    bldr.String("byte[] rawData = global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArrayFromStream(overhead.MessageSize, stream);");
                                }

                                byteDataRefArg = param.Index;
                                break;
                                
                            case ParameterHelper.RawByteInjectType.ByteReader:
                                bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = new global::DanielWillett.SpeedBytes.ByteReader();");
                                if (stream)
                                {
                                    bldr.Build($"arg{param.Index}.LoadNew(new global::DanielWillett.ModularRpcs.Data.PassthroughReadStream(stream, overhead.MessageSize));");
                                }
                                else
                                {
                                    bldr.Build($"arg{param.Index}.LoadNew(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetArraySegmentFromMemory(rawData, canTakeOwnership, out canTakeOwnership));");
                                }
                                break;

                            default:
                                bldr.Build($"{param.Type.GloballyQualifiedName} arg{param.Index} = default;")
                                    .Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInvalidRawParameter, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\"));");
                                break;
                        }

                        continue;
                }
            }

            if (canTakeOwnershipArg >= 0)
            {
                bldr.String($"bool arg{canTakeOwnershipArg} = canTakeOwnership;");
            }
        }

        if (!Method.IsStatic)
        {
            bldr.String("if (targetObject == null)")
                .In().Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull, \"{TypeHelper.Escape(Class.Type.FullyQualifiedName)}\", \"{TypeHelper.Escape(Method.DisplayString)}\"));")
                .Out()
                .Empty();
        }
        else
        {
            bldr.Empty();
        }

        if (byteDataRefArg >= 0)
        {
            bldr.String("if (rawData.Length >= 0)")
                .String("{")
                .In();
        }

        for (int i = 0;; ++i)
        {
            if (i == 1)
            {
                bldr.Build($"byte arg{byteDataRefArg} = 0;");
            }
            if (!Method.IsStatic)
            {
                if (hasReturnValue)
                {
                    bldr.Build($"rtnValue = (({Class.Type.GloballyQualifiedName})targetObject).@{Method.Name}(");
                }
                else
                {
                    bldr.Build($"(({Class.Type.GloballyQualifiedName})targetObject).@{Method.Name}(");
                }
            }
            else
            {
                if (hasReturnValue)
                {
                    bldr.Build($"rtnValue = @{Class.Type.Name}.@{Method.Name}(");
                }
                else
                {
                    bldr.Build($"@{Class.Type.Name}.@{Method.Name}(");
                }
            }

            bldr.In();
            foreach (RpcParameterDeclaration param in Method.Parameters)
            {
                if (param.Index == byteDataRefArg)
                {
                    if (i == 1)
                        bldr.String($"ref arg{byteDataRefArg}, // data is empty, would throw IOB.");
                    else
                        bldr.String("ref rawData[0],");
                }
                else if (param.Index == Method.Parameters.Count - 1)
                    bldr.Build($"arg{param.Index}");
                else
                    bldr.Build($"arg{param.Index}, ");
            }

            bldr.Out()
                .String(");")
                .Empty();

            if (byteDataRefArg < 0 || i == 1)
                break;

            bldr.Out()
                .String("}")
                .String("else")
                .String("{").In();
        }

        if (byteDataRefArg >= 0)
        {
            bldr.Out().String("}");
        }

        if (isFixed)
        {
            bldr.Out()
                .String("}");
        }

        if (Method.ReturnTypeAwaitableInfo.HasValue)
        {
            AwaitableInfo awaitableInfo = Method.ReturnTypeAwaitableInfo.Value;

            switch (awaitableInfo.ConfigureAwaitMethodType)
            {
                case ConfigureAwaitMethodType.Void:
                    bldr.Build($"rtnValue.ConfigureAwait(false);");
                    bldr.Build($"{awaitableInfo.AwaiterType.GloballyQualifiedName} rtnAwaiter = rtnValue.GetAwaiter();");
                    break;

                case ConfigureAwaitMethodType.ReturnsAwaiterRef:
                case ConfigureAwaitMethodType.ReturnsAwaiter:
                    bldr.Build($"");
                    bldr.Build($"{awaitableInfo.AwaiterType.GloballyQualifiedName} rtnAwaiter = rtnValue.ConfigureAwait(false).GetAwaiter();");
                    break;

                default:
                    bldr.Build($"{awaitableInfo.AwaiterType.GloballyQualifiedName} rtnAwaiter = rtnValue.GetAwaiter();");
                    break;
            }

            bldr.String("if (!rtnAwaiter.IsCompleted)")
                .String("{").In()
                    .Build($"_ = new {Info.ClosureTypeName}(overhead, router, serializer, rtnAwaiter);")
                    .String("return;")
                .Out()
                .String("}")
                .Empty();

            GenerateContinuationBody(bldr, "overhead", "router", "serializer", "rtnAwaiter", "rtnAwaiterValue");
        }
        else
        {
            InvokeReturnValue(bldr, Method.ReturnType, "overhead", "router", "serializer", "rtnValue");
        }

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

    public void GenerateContinuationBody(SourceStringBuilder bldr, string overhead, string router, string serializer, string awaiter, string value = "rtnValue")
    {
        AwaitableInfo awaitInfo = Method.ReturnTypeAwaitableInfo!.Value;
        TypeSymbolInfo rtnType = awaitInfo.AwaitReturnType;

        bool hasReturnValue = rtnType.Info.Type != TypeSerializationInfoType.Void;
        if (hasReturnValue)
        {
            bldr.Build($"{rtnType.GloballyQualifiedName} {value};");
        }

        bldr.String("try")
            .String("{").In();

        bldr.String(hasReturnValue ? $"{value} = {awaiter}.GetResult();" : $"{awaiter}.GetResult();");

        bldr.Out()
            .String("}")
            .String("catch (global::System.Exception ex)")
            .String("{").In()
            .Build($"{router}.HandleInvokeException(ex, {overhead}, {serializer});")
            .String("return;")
            .Out()
            .String("}")
            .Empty()
            .Build($"if (({overhead}.Flags & global::DanielWillett.ModularRpcs.Protocol.RpcFlags.FireAndForget) != 0)")
            .In().String("return;").Out()
            .Empty();

        InvokeReturnValue(bldr, rtnType, overhead, router, serializer, value);
    }

    private static void InvokeReturnValue(SourceStringBuilder bldr, TypeSymbolInfo rtnType, string overhead, string router, string serializer, string value)
    {
        TypeSerializationInfo rtnTypeInfo = rtnType.Info;
        switch (rtnTypeInfo.Type)
        {
            case TypeSerializationInfoType.Void:
                bldr.Build($"{router}.HandleInvokeVoidReturn({overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.PrimitiveLike:
            case TypeSerializationInfoType.Value:
                bldr.Build($"{router}.HandleInvokeReturnValue<{rtnType.GloballyQualifiedName}>({value}, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.NullableValue:
                bldr.Build($"{router}.HandleInvokeNullableReturnValue<{rtnTypeInfo.UnderlyingType.GloballyQualifiedName}>({value}, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.SerializableValue:
                bldr.Build($"{router}.HandleInvokeSerializableReturnValue<{rtnTypeInfo.SerializableType.GloballyQualifiedName}>({value}, null, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.SerializableCollection:
                bldr.Build($"{router}.HandleInvokeSerializableReturnValue<{rtnTypeInfo.SerializableType.GloballyQualifiedName}>(default, {value} == null ? global::System.DBNull.Value : {value}, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.NullableSerializableValue:
                bldr.Build($"{router}.HandleInvokeNullableSerializableReturnValue<{rtnTypeInfo.UnderlyingType.GloballyQualifiedName}>({value}, null, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.NullableSerializableCollection:
                bldr.Build($"{router}.HandleInvokeNullableSerializableReturnValue<{rtnTypeInfo.UnderlyingType.GloballyQualifiedName}>(null, {value} == null ? global::System.DBNull.Value : {value}, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                bldr.Build($"{router}.HandleInvokeSerializableReturnValue<{rtnTypeInfo.SerializableType.GloballyQualifiedName}>(default({rtnTypeInfo.SerializableType.GloballyQualifiedName}), !{value}.HasValue ? global::System.DBNull.Value : {value}.Value, {overhead}, {serializer});");
                break;

            case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
                bldr.Build($"{router}.HandleInvokeNullableSerializableReturnValue<{rtnTypeInfo.UnderlyingType.GloballyQualifiedName}>(null, !{value}.HasValue ? global::System.DBNull.Value : {value}.Value, {overhead}, {serializer});");
                break;
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
                            bldr.String("{").In()
                                    .Build($"{valueVar} = *({symbolInfo.GloballyQualifiedName}*){bufferName};");
                            if (idPrimType is TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr)
                                bldr.Build($"{offsetVar} += 8u;");
                            else
                                bldr.Build($"{offsetVar} += sizeof({symbolInfo.GloballyQualifiedName});");
                            bldr.Out()
                                .String("}")
                                .String("else")
                                .String("{").In();

                            switch (idPrimType)
                            {
                                case TypeHelper.PrimitiveLikeType.Char:
                                case TypeHelper.PrimitiveLikeType.Int16:
                                case TypeHelper.PrimitiveLikeType.UInt16:
                                    bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8) );")
                                        .Build($"{offsetVar} += 2u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int32:
                                case TypeHelper.PrimitiveLikeType.UInt32:
                                    bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) );")
                                        .Build($"{offsetVar} += 4u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Single:

                                    bldr.Build($"int {valueVar}Num = bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24);")
                                        .Build($"{valueVar} = *(float*)&{valueVar}Num;")
                                        .Build($"{offsetVar} += 4u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Double:

                                    bldr.Build($"long {valueVar}Num = unchecked ( (long)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (long)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                        .Build($"{valueVar} = *(double*)&{valueVar}Num;")
                                        .Build($"{offsetVar} += 8u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int64:
                                case TypeHelper.PrimitiveLikeType.UInt64:
                                    if ((symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                    {
                                        bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | ({symbolInfo.GloballyQualifiedName})(({(idPrimType == TypeHelper.PrimitiveLikeType.UInt64 ? "ulong" : "long")})(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32) );")
                                            .Build($"{offsetVar} += 8u;");
                                    }
                                    else
                                    {
                                        bldr.Build($"{valueVar} = unchecked ( ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | ({symbolInfo.GloballyQualifiedName})(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                            .Build($"{offsetVar} += 8u;");
                                    }
                                    break;

                                case TypeHelper.PrimitiveLikeType.IntPtr:

                                    if (canUseNativeIntArithmitic)
                                    {
                                        bldr.String("if (global::System.IntPtr.Size == 8)").In()
                                                .Build($"{valueVar} = unchecked ( (nint)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (nint)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );").Out()
                                            .String("else")
                                            .String("{").In()
                                                .Build($"long {valueVar}Num = unchecked ( (long)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (long)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                                .Build($"if ({valueVar}Num > {int.MaxValue} || {valueVar}Num < {int.MinValue})")
                                                .In().String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcParseException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcParseExceptionBufferRunOutNativeIntOverflow, \"IntPtrParser\")) { ErrorCode = 9 };").Out()
                                                .Empty()
                                                .Build($"{valueVar} = (nint){valueVar}Num;")
                                                .Out()
                                            .String("}");
                                    }
                                    else
                                    {
                                        bldr.Build($"long {valueVar}Num = unchecked ( (long)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (long)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                            .Build($"if (global::System.IntPtr.Size != 8 && {valueVar}Num > {int.MaxValue} || {valueVar}Num < {int.MinValue})")
                                            .In().String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcParseException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcParseExceptionBufferRunOutNativeIntOverflow, \"IntPtrParser\")) { ErrorCode = 9 };").Out()
                                            .Empty()
                                            .Build($"{valueVar} = ({((symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.Enum) != 0 ? symbolInfo.GloballyQualifiedName : "nint")}){valueVar}Num;");
                                    }

                                    bldr.Build($"{offsetVar} += 8u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.UIntPtr:
                                    
                                    if (canUseNativeIntArithmitic)
                                    {
                                        bldr.String("if (global::System.IntPtr.Size == 8)").In();
                                        if ((symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                            bldr.Build($"{valueVar} = unchecked ( (nuint)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | ({symbolInfo.GloballyQualifiedName})((nuint)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32) );").Out();
                                        else
                                            bldr.Build($"{valueVar} = unchecked ( (nuint)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (nuint)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );").Out();
                                        
                                        bldr.String("else")
                                            .String("{").In()
                                                .Build($"long {valueVar}Num = unchecked ( (ulong)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (ulong)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                                .Build($"if ({valueVar}Num > {uint.MaxValue})")
                                                .In().String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcParseException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcParseExceptionBufferRunOutNativeIntOverflow, \"UIntPtrParser\")) { ErrorCode = 9 };").Out()
                                                .Empty()
                                                .Build($"{valueVar} = ({symbolInfo.GloballyQualifiedName}){valueVar}Num;")
                                                .Out()
                                            .String("}");
                                    }
                                    else
                                    {
                                        bldr.Build($"ulong {valueVar}Num = unchecked ( (ulong)(bytes[{offsetVar}] | bytes[{offsetVar} + 1] << 8 | bytes[{offsetVar} + 2] << 16 | bytes[{offsetVar} + 3] << 24) | (ulong)(bytes[{offsetVar} + 4] | bytes[{offsetVar} + 5] << 8 | bytes[{offsetVar} + 6] << 16 | bytes[{offsetVar} + 7] << 24) << 32 );")
                                            .Build($"if (global::System.IntPtr.Size != 8 && {valueVar}Num > {uint.MaxValue})")
                                            .In().String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcParseException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcParseExceptionBufferRunOutNativeIntOverflow, \"UIntPtrParser\")) { ErrorCode = 9 };").Out()
                                            .Empty()
                                            .Build($"{valueVar} = ({((symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.Enum) != 0 ? symbolInfo.GloballyQualifiedName : "nuint")}){valueVar}Num;");
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

internal class ReceiveMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;
    public string ReceiveMethodNameStream;
    public string ReceiveMethodNameBytes;
    public string? ClosureTypeName;
    public string? DelegateName;
    public DelegateType? DelegateType;
    public bool IsDuplicateClosure;
    public bool IsDuplicateDelegateType;

    public ReceiveMethodInfo(RpcMethodDeclaration method, int overload)
    {
        Method = method;
        Overload = overload;
        ReceiveMethodNameBytes = null!;
        ReceiveMethodNameStream = null!;
    }
}