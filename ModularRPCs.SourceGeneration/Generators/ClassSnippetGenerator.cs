using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DanielWillett.ModularRpcs.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ModularRPCs.Util;

namespace ModularRPCs.Generators;

internal readonly struct ClassSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcClassDeclaration Class;
    public readonly EquatableList<RpcMethodDeclaration> MethodDeclarations;
    public readonly CSharpCompilation Compilation;

    internal ClassSnippetGenerator(SourceProductionContext context, CSharpCompilation compilation, RpcClassDeclaration @class)
    {
        Compilation = compilation;
        Context = context;
        Class = @class;
        MethodDeclarations = @class.Methods;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GenerateClassSnippet()
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        const string receiveInvokeNamePrefix = "ModularRpcsGeneratedInvoke";
        const string callMethodInfoFieldPrefix = "_modularRpcsGeneratedCallMethodInfo";
        const string generatedClosureTypePrefix = "__ModularRpcsGeneratedClosure";

        SourceStringBuilder bldr = new SourceStringBuilder(2048, CultureInfo.InvariantCulture);

        List<SendMethodInfo> sendMethods = MethodDeclarations
            .Where(x => x.IsSend)
            .OrderBy(x => x.Name)
            .Select(x => new SendMethodInfo(x, -1, null!))
            .ToList();

        List<ReceiveMethodInfo> recvMethods = MethodDeclarations
            .Where(x => x.IsReceive)
            .OrderBy(x => x.Name)
            .Select(x => new ReceiveMethodInfo(x, -1))
            .ToList();

        int overload = 0;
        string? last = null;
        int closures = -1;
        for (int i = 0; i < recvMethods.Count; i++)
        {
            ReceiveMethodInfo info = recvMethods[i];
            if (!string.Equals(info.Method.Name, last, StringComparison.Ordinal))
                overload = 0;
            else
                ++overload;

            info.Overload = overload;
            object overloadBox = info.Overload;
            info.ReceiveMethodNameBytes = $"{receiveInvokeNamePrefix}{info.Method.Name}Ovl{overloadBox}Bytes";
            info.ReceiveMethodNameStream = $"{receiveInvokeNamePrefix}{info.Method.Name}Ovl{overloadBox}Stream";

            if (info.Method.DelegateType.Predefined == PredefinedDelegateType.None)
            {
                for (int j = 0; j < i; ++j)
                {
                    ReceiveMethodInfo otherInfo = recvMethods[j];
                    if (!otherInfo.Method.DelegateType.Equals(info.Method.DelegateType))
                        continue;

                    info.DelegateName = otherInfo.DelegateName;
                    info.DelegateType = otherInfo.DelegateType;
                    info.IsDuplicateDelegateType = true;
                    break;
                }

                if (info.DelegateType == null)
                {
                    info.DelegateName = string.Format(info.Method.DelegateType.Name, overloadBox);
                    info.DelegateType = info.Method.DelegateType;
                }
            }

            if (info.Method.ReturnTypeAwaitableInfo.HasValue)
            {
                TypeSymbolInfo awaiterType = info.Method.ReturnTypeAwaitableInfo.Value.AwaiterType;
                string? existing = null;
                for (int j = 0; j < i; ++j)
                {
                    ReceiveMethodInfo otherInfo = recvMethods[j];
                    if (otherInfo.ClosureTypeName == null || !otherInfo.Method.ReturnTypeAwaitableInfo!.Value.AwaiterType.Equals(awaiterType))
                        continue;

                    existing = otherInfo.ClosureTypeName;
                    break;
                }

                info.ClosureTypeName = existing ?? $"{generatedClosureTypePrefix}_{++closures}";
                info.IsDuplicateClosure = existing != null;
            }
            
            last = info.Method.Name;
        }

        overload = 0;
        last = null;
        for (int i = 0; i < sendMethods.Count; i++)
        {
            SendMethodInfo info = sendMethods[i];
            if (!string.Equals(info.Method.Name, last, StringComparison.Ordinal))
                overload = 0;
            else
                ++overload;

            object overloadBox = overload;

            if (info.Method.DelegateType.Predefined == PredefinedDelegateType.None)
            {
                for (int j = 0; j < recvMethods.Count; ++j)
                {
                    ReceiveMethodInfo otherInfo = recvMethods[j];
                    if (!otherInfo.Method.DelegateType.Equals(info.Method.DelegateType))
                        continue;

                    info.DelegateName = otherInfo.DelegateName;
                    info.DelegateType = otherInfo.DelegateType;
                    info.IsDuplicateDelegateType = true;
                    break;
                }

                if (info.DelegateType == null)
                {
                    for (int j = 0; j < i; ++j)
                    {
                        SendMethodInfo otherInfo = sendMethods[j];
                        if (!otherInfo.Method.DelegateType.Equals(info.Method.DelegateType))
                            continue;

                        info.DelegateName = otherInfo.DelegateName;
                        info.DelegateType = otherInfo.DelegateType;
                        info.IsDuplicateDelegateType = true;
                        break;
                    }

                    if (info.DelegateType == null)
                    {
                        info.DelegateName = string.Format(info.Method.DelegateType.Name, overloadBox);
                        info.DelegateType = info.Method.DelegateType;
                    }
                }
            }

            info.Overload = overload;
            info.MethodInfoFieldName = $"{callMethodInfoFieldPrefix}{info.Method.Name}Ovl{overloadBox}";
            last = info.Method.Name;
        }

        bldr.String("// <auto-generated/>")
            .Preprocessor("#nullable disable");
        if (Class.Type.Namespace != null)
        {
            bldr.Build($"namespace {Class.Type.Namespace}")
                .String("{").In();
        }

        if (Class.NestedParents is not null)
        {
            foreach (string def in Class.NestedParents)
            {
                bldr.Build($"partial {def}")
                    .String("{").In();
            }
        }

        string idVarMe = Class is { IdType: not null, IdIsExplicit: true }
            ? $"((global::DanielWillett.ModularRpcs.Protocol.IRpcObject<{Class.IdType.GloballyQualifiedName}>)me).Identifier"
            : "me.Identifier";

        string idVarThis = Class is { IdType: not null, IdIsExplicit: true }
            ? $"((global::DanielWillett.ModularRpcs.Protocol.IRpcObject<{Class.IdType.GloballyQualifiedName}>)this).Identifier"
            : "this.Identifier";

        // class attributes
        bldr.String("[global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyTypeAttribute(");
        if (sendMethods.Count > 0 || recvMethods.Count > 0)
        {
            bldr.In().Build($"TypeSetupMethodName = nameof(@{Class.Type.Name}.__ModularRpcsGeneratedSetupStaticGeneratedProxy)").Out();
        }
        bldr.String(")]")
            .Build($"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"DanielWillett.ModularRpcs.SourceGeneration\", \"{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}\")]");

        string @protected = Class.IsSealed ? "private" : "protected";
        string @virtual = Class.InheritedGeneratedType != null ? "override " : (Class.IsSealed ? string.Empty : "virtual ");

        // class {
        bldr.Build($"partial {Class.Definition} : global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType")
            .Preprocessor("#if NET7_0_OR_GREATER")
            .In().String(", global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyTypeWithSetupMethod").Out()
            .Preprocessor("#endif")
            .String("{").In()
                .Empty()
                .String("#region ModularRPCs class-level infrastructure");

        if (Class.InheritedGeneratedType == null)
        {
            bldr.Empty()
                .String("/// <summary>")
                .String("/// Stores generic information that is needed by all proxy types to send and receive RPCs.")
                .String("/// </summary>")
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"{@protected} global::DanielWillett.ModularRpcs.Reflection.ProxyContext __modularRpcsGeneratedProxyContext;");
        }

        bldr
                .Empty()
                .String("/// <summary>")
                .String("/// Invoked after this type is created by <see cref=\"M:DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CreateProxy\">.")
                .String("/// </summary>")
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String($"public {@virtual}void __ModularRpcsGeneratedSetupGeneratedProxyInfo(").In()
                    .String("in global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo info)").Out()
                .String("{").In();

        if (Class.InheritedGeneratedType == null)
        {
            bldr.Build($"info.Router.GetDefaultProxyContext(typeof(@{Class.Type.Name}), out this.__modularRpcsGeneratedProxyContext);");
        }
        else
        {
            bldr.String("base.__ModularRpcsGeneratedSetupGeneratedProxyInfo(in info);");
        }

        if (Class is { IdType: not null, IdIsInBaseType: false })
        {
            if (Class.IdType.IsNullable)
            {
                bldr.Empty()
                    .Build($"if (!{idVarThis}.HasValue)")
                    .In().String($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcObjectInitializationException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxInstanceIdDefaultValue, \"{TypeHelper.Escape(Class.Type.FullyQualifiedName)}\", \"IRpcObject<{TypeHelper.Escape(Class.IdType.FullyQualifiedName)}>\"));").Out();
            }
            else if (!Class.IdType.IsValueType)
            {
                bldr.Empty()
                    .Build($"if ({idVarThis} == null)")
                    .In().String($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcObjectInitializationException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxInstanceIdDefaultValue, \"{TypeHelper.Escape(Class.Type.FullyQualifiedName)}\", \"IRpcObject<{TypeHelper.Escape(Class.IdType.FullyQualifiedName)}>\"));").Out();
            }

            bldr.Empty()
                .Build($"if (!__modularRpcsGeneratedInstances.TryAdd({idVarThis}{(Class.IdType.IsNullable ? ".Value" : string.Empty)}, new global::System.WeakReference(this)))")
                .In().String($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcObjectInitializationException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxInstanceWithThisIdAlreadyExists, \"{TypeHelper.Escape(Class.Type.FullyQualifiedName)}\", \"IRpcObject<{TypeHelper.Escape(Class.IdType.FullyQualifiedName)}>\"));").Out();
        }

        bldr    .Out()
                .String("}")
                .Empty();

        // static init method

        bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
            .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
            .Preprocessor("#if NET7_0_OR_GREATER")
            .String("public")
            .Preprocessor("#else")
            .String(@protected)
            .Preprocessor("#endif")
            .String("static unsafe void __ModularRpcsGeneratedSetupStaticGeneratedProxy(").In()
                .String("global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder state)").Out()
            .String("{").In();

        if (Class.InheritedGeneratedType != null)
        {
            bldr.Build($"{Class.InheritedGeneratedType.GloballyQualifiedName}.__ModularRpcsGeneratedSetupStaticGeneratedProxy(state);");
        }

        if (Class.IdType != null)
        {
            bldr.Build($"state.AddGetObjectFunction(typeof(@{Class.Type.Name}), new global::System.Func<object, global::System.WeakReference>(__ModularRpcsGeneratedGetObject));");
            bldr.Build($"state.AddReleaseObjectFunction(typeof(@{Class.Type.Name}), new global::System.Func<object, bool>(__ModularRpcsGeneratedReleaseObject));");
        }

        bldr.Build($"state.AddGetOverheadSizeFunction(typeof(@{Class.Type.Name}), new global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.GetOverheadSize(__ModularRpcsGeneratedGetOverheadSize));");

        foreach (SendMethodInfo method in sendMethods)
        {
            if (Class.IsValueType)
            {
                bldr.Build($"state.AddCallGetter(static () => @{Class.Type.Name}.{method.MethodInfoFieldName});");
            }
            else
            {
                bldr.Build($"state.AddCallGetter(static () => ref @{Class.Type.Name}.{method.MethodInfoFieldName});");
            }
            bldr.Build($"state.AddMethodSignatureHash(@{Class.Type.Name}.{method.MethodInfoFieldName}.MethodHandle, {method.Method.SignatureHash});");
        }

        if (recvMethods.Count > 0)
        {
            if (sendMethods.Count > 0)
                bldr.Empty();

            bldr.String("global::System.Reflection.MethodInfo workingMethod;");
            
            foreach (ReceiveMethodInfo method in recvMethods)
            {
                bldr.Empty()
                    .Build($"// Register {method.Method.Name}")
                    .Build($"workingMethod = {(method.DelegateType ?? method.Method.DelegateType).GetMethodByExpressionString(method.Method, Class.Type.Name, method.DelegateName)};");

                bldr.String("state.AddReceiveMethod(").In()
                        .String("workingMethod.MethodHandle,")
                        .String(method.Method.Target.Raw
                            ? "global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder.ReceiveMethodInvokerType.BytesRaw,"
                            : "global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder.ReceiveMethodInvokerType.Bytes,")
                        .String(method.Method.Target.Raw
                            ? "new global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.RpcInvokeHandlerRawBytes("
                            : "new global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.RpcInvokeHandlerBytes(").In()
                            .Build($"@{Class.Type.Name}.{method.ReceiveMethodNameBytes}")
                            .Out()
                        .String(")")
                        .Out()
                    .String(");");

                bldr.String("state.AddReceiveMethod(").In()
                        .String("workingMethod.MethodHandle,")
                        .String(method.Method.Target.Raw
                            ? "global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder.ReceiveMethodInvokerType.StreamRaw,"
                            : "global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder.ReceiveMethodInvokerType.Stream,")
                        .String("new global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.RpcInvokeHandlerStream(").In()
                            .Build($"@{Class.Type.Name}.{method.ReceiveMethodNameStream}")
                            .Out()
                        .String(")")
                        .Out()
                    .String(");");

                bldr.Build($"state.AddMethodSignatureHash(workingMethod.MethodHandle, {method.Method.SignatureHash});");
            }
        }
        bool hasStartedRegisteringBroadcastReceiveMethods = false;

        foreach (ReceiveMethodInfo method in recvMethods)
        {
            if (string.IsNullOrEmpty(method.Method.Target.MethodName))
                continue;

            if (!hasStartedRegisteringBroadcastReceiveMethods)
            {
                bldr.Empty()
                    .Build($"state.AddBroadcastReceiveMethods(typeof(@{Class.Type.Name}),").In()
                    .String("r =>")
                    .String("{").In();
                hasStartedRegisteringBroadcastReceiveMethods = true;
            }

            bldr.String("r.AddMethod(").In()
                .String("new global::DanielWillett.ModularRpcs.Reflection.RpcEndpointTarget()")
                .String("{").In()
                .Build($"MethodName = \"{TypeHelper.Escape(string.IsNullOrEmpty(method.Method.Target.MethodName) ? method.Method.Name : method.Method.Target.MethodName!)}\",")
                .Build($"DeclaringTypeName = \"{TypeHelper.Escape(string.IsNullOrEmpty(method.Method.Target.TypeName) ? Class.Type.AssemblyQualifiedName : method.Method.Target.TypeName!)}\",")
                .Build($"SignatureHash = {method.Method.SignatureHash},")
                .String("IgnoreSignatureHash = false,")
                .Build($"ParameterTypesAreBindOnly = {(method.Method.Target.ParametersAreBindedParametersOnly ? "true" : "false")},");
            if (method.Method.Target.ParameterTypeNames == null)
            {
                bldr.String("ParameterTypes = null,");
            }
            else if (method.Method.Target.ParameterTypeNames.Length == 0)
            {
                bldr.String("ParameterTypes = global::System.Array.Empty<string>(),");
            }
            else
            {
                string types = string.Join("\", \"", method.Method.Target.ParameterTypeNames.Select(TypeHelper.Escape));
                bldr.Build($"ParameterTypes = new string[] {{ \"{types}\" }},");
            }

            bldr.Build($"IsBroadcast = {(method.Method.IsBroadcast ? "true" : "false")},")
                .Build($"InjectsCancellationToken = {(method.Method.InjectsCancellationToken ? "true" : "false")},")
                .Build($"OwnerMethodInfo = {(method.DelegateType ?? method.Method.DelegateType).GetMethodByExpressionString(method.Method, Class.Type.Name, method.DelegateName)}").Out()
                .String("}")
                .Out()
                .String(");");
        }

        if (hasStartedRegisteringBroadcastReceiveMethods)
        {
            bldr.Out()
                .String("}")
                .Out()
                .String(");");
        }

        bldr.Out()
            .String("}");

        if (Class.IdType != null)
        {
            string idType = Class.IdType.Info.Type switch
            {
                TypeSerializationInfoType.NullableValue or TypeSerializationInfoType.NullableSerializableValue
                    => Class.IdType.Info.UnderlyingType.GloballyQualifiedName,
                _ => Class.IdType.GloballyQualifiedName
            };

            if (!Class.IdIsInBaseType)
            {
                bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                    .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"{@protected} static unsafe readonly global::System.Collections.Concurrent.ConcurrentDictionary<{idType}, global::System.WeakReference> __modularRpcsGeneratedInstances")
                    .In().Build($"= new global::System.Collections.Concurrent.ConcurrentDictionary<{idType}, global::System.WeakReference>();").Out()
                    .Empty()
                    .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                    .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"{@protected} int __modularRpcsSuppressFinalize;")
                    .Empty();
            }

            bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("private static unsafe global::System.WeakReference __ModularRpcsGeneratedGetObject(object id)")
                .String("{").In()
                    
                    .Build($"{idType} idType = ({idType})id;")
                    .String("__modularRpcsGeneratedInstances.TryGetValue(idType, out global::System.WeakReference outRef);")
                    .String("return outRef;")
                
                .Out()
                .String("}")
                .Empty()
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("private static unsafe bool __ModularRpcsGeneratedReleaseObject(object obj)")
                .String("{").In()
                    
                    .Build($"@{Class.Type.Name} me = (@{Class.Type.Name})obj;")
                    .String("if (global::System.Threading.Interlocked.Exchange(ref me.__modularRpcsSuppressFinalize, 1) != 0)")
                    .In().String("return false;").Out()
                    .Empty();

            if (Class.HasReleaseMethod)
            {
                bldr.String("try")
                    .String("{").In()
                    .String("me.Release();")
                    .Out()
                    .String("}")
                    .String("catch")
                    .String("{").In()
                    .String("me.__modularRpcsSuppressFinalize = 0;")
                    .String("throw;")
                    .Out()
                    .String("}");
            }

            bldr    .Build($"return __modularRpcsGeneratedInstances.TryRemove({idVarMe}, out _);")
                
                .Out()
                .String("}")
                .Empty();

            bldr.String("// Use DanielWillett.ModularRpcs.Protocol.IExplicitFinalizerRpcObject to implement your own finalizer.");

            if (Class.IsUnityType)
            {
                bldr.String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"protected {@virtual}void OnDestroy()");
            }
            else
            {
                bldr.String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"~@{Class.Type.Name}()");
            }

            bldr.String("{").In();

            if (Class.HasExplicitFinalizer)
            {
                bldr.String("try")
                    .String("{").In();
            }

            bldr.String("if (global::System.Threading.Interlocked.Exchange(ref this.__modularRpcsSuppressFinalize, 1) == 0)")
                .In().Build($"__modularRpcsGeneratedInstances.TryRemove({idVarThis}, out _);").Out();

            if (Class.HasExplicitFinalizer)
            {

                bldr.Out()
                    .String("}")
                    .String("finally")
                    .String("{").In();

                string src = Class.IsUnityType
                    ? "global::DanielWillett.ModularRpcs.Protocol.ExplicitFinalizerSource.OnDestroy"
                    : "global::DanielWillett.ModularRpcs.Protocol.ExplicitFinalizerSource.Finalizer";

                if (Class.IsExplicitFinalizerExplicit)
                    bldr.Build($"((global::DanielWillett.ModularRpcs.Protocol.IExplicitFinalizerRpcObject)this).OnFinalizing({src});");
                else
                    bldr.Build($"this.OnFinalizing({src});");

                bldr.Out()
                    .String("}");
            }

            bldr.Out()
                .String("}");
        }

        bldr.String("private static unsafe int __ModularRpcsGeneratedGetOverheadSize(object obj, global::System.RuntimeMethodHandle handle, ref global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo callInfo, out int sizeWithoutId)")
            .String("{").In()

            .Build($"@{Class.Type.Name} me = (@{Class.Type.Name})obj;")
            .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer = me.__modularRpcsGeneratedProxyContext.DefaultSerializer;")
            .String("uint overheadSize = me.__modularRpcsGeneratedProxyContext.Router.GetOverheadSize(handle, ref callInfo);")
            .String("sizeWithoutId = checked ( (int)overheadSize );")
            .Empty()
            .String("uint idTypeSize;")
            .String("uint idSize;")
            .Empty();

        SendMethodSnippetGenerator.GenerateGetIdSize(
                bldr,
                Class,
                out _,
                "idTypeSize",
                "idSize",
                "serializer",
                null,
                "_",
                "serializer.CanFastReadPrimitives",
                "overheadSize",
                out _,
                "me"
            );


        bldr.Empty()
            .String("return checked ( (int)(overheadSize + idTypeSize) );")

            .Out()
            .String("}");

        bldr.String("#endregion")
            .Empty();

        // send methods
        bldr.Empty()
            .Empty()
            .String("#region Generated send stubs");
        foreach (SendMethodInfo method in sendMethods)
        {
            bool receiveInjectsCancellationToken = true;
            if (TryGetReceiveMethod(method.Method, out RpcMethodDeclaration recvMethod))
            {
                receiveInjectsCancellationToken = recvMethod.InjectsCancellationToken;
            }

            bldr.Empty()
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"private static {(method.Method.NeedsUnsafe || recvMethod is { NeedsUnsafe: true } ? "unsafe " : string.Empty)}global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo {method.MethodInfoFieldName}").In()
                    .String("= new global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo()")
                    .String("{").In()
                        .Build($"MethodHandle = {(method.DelegateType ?? method.Method.DelegateType).GetMethodByExpressionString(method.Method, Class.Type.Name, method.DelegateName)}.MethodHandle,")
                .Build($"IsFireAndForget = {(method.Method.IsFireAndForget ? "true" : "false")},")
                .Build($"SignatureHash = {method.Method.SignatureHash},")
                .Build($"HasIdentifier = {(Class.IdType == null ? "false" : "true")},");
            if (method.Method.Timeout.Ticks == 0)
            {
                bldr.Build($"Timeout = global::System.TimeSpan.Zero,");
            }
            else
            {
                bldr.Build($"Timeout = new global::System.TimeSpan({method.Method.Timeout.Ticks}),");
            }

            bldr.String("Endpoint = new global::DanielWillett.ModularRpcs.Reflection.RpcEndpointTarget()")
                .String("{").In()
                    .Build($"MethodName = \"{TypeHelper.Escape(string.IsNullOrEmpty(method.Method.Target.MethodName) ? method.Method.Name : method.Method.Target.MethodName!)}\",")
                    .Build($"DeclaringTypeName = \"{TypeHelper.Escape(string.IsNullOrEmpty(method.Method.Target.TypeName) ? Class.Type.AssemblyQualifiedName : method.Method.Target.TypeName!)}\",")
                    .Build($"SignatureHash = {method.Method.SignatureHash},")
                    .String("IgnoreSignatureHash = false,")
                    .Build($"ParameterTypesAreBindOnly = {(method.Method.Target.ParametersAreBindedParametersOnly ? "true" : "false")},");

            if (recvMethod is { NeedsSignatureCheck: true })
            {
                IList<RpcParameterDeclaration> parameters = recvMethod.Parameters;
                if (method.Method.Target.ParametersAreBindedParametersOnly)
                {
                    ParameterHelper.BindParameters(recvMethod.Parameters, out _, out RpcParameterDeclaration[] toBind);
                    parameters = toBind;
                }

                if (parameters.Count == 0)
                {
                    bldr.String("ParameterTypes = global::System.Array.Empty<string>(),");
                }
                else
                {
                    string types = string.Join("\", \"", parameters.Select(x => TypeHelper.Escape(x.Type.AssemblyQualifiedName)));
                    bldr.Build($"ParameterTypes = new string[] {{ \"{types}\" }},");
                }
            }
            else if (method.Method.Target.ParameterTypeNames == null)
            {
                bldr.String("ParameterTypes = null,");
            }
            else if (method.Method.Target.ParameterTypeNames.Length == 0)
            {
                bldr.String("ParameterTypes = global::System.Array.Empty<string>(),");
            }
            else
            {
                string types = string.Join("\", \"", method.Method.Target.ParameterTypeNames.Select(TypeHelper.Escape));
                bldr.Build($"ParameterTypes = new string[] {{ \"{types}\" }},");
            }

            bldr    .String("IsBroadcast = false,")
                    .Build($"InjectsCancellationToken = {(receiveInjectsCancellationToken ? "true" : "false")},");

            ReceiveMethodInfo? recvMethodInfo = recvMethod != null ? recvMethods.Find(x => ReferenceEquals(x.Method, recvMethod)) : null;
            if (recvMethodInfo != null)
            {
                bldr.Build($"OwnerMethodInfo = {(recvMethodInfo.DelegateType ?? recvMethod!.DelegateType).GetMethodByExpressionString(recvMethod!, Class.Type.Name, recvMethodInfo.DelegateName)}");
            }
            else
            {
                bldr.String("OwnerMethodInfo = null");
            }
            bldr.Out()
                .String("}")
                .Out()
                .String("};")
                .Out();

            bldr.Empty()
                .String("/// <summary>")
                .Build($"/// Generated receive invoker for <see cref=\"{method.Method.XmlDocs}\"> (Overload {method.Overload + 1}).")
                .String("/// </summary>")
                .String("/// <remarks>This method is responsible for triggering the initial RPC invocation.</remarks>")
                .Empty();

            bldr.Build($"{(method.Method.NeedsUnsafe ? "unsafe " : string.Empty)}{method.Method.Definition}")
                .String("{").In();

            new SendMethodSnippetGenerator(Context, Compilation, method, Class)
                .GenerateMethodBodySnippet(bldr);

            bldr.Out()
                .String("}");
        }

        bldr.Empty()
            .String("#endregion");

        // receive methods
        bldr.Empty()
            .Empty()
            .String("#region Generated receive invokers");
        foreach (ReceiveMethodInfo method in recvMethods)
        {
            bldr.Empty()
                .String("/// <summary>")
                .Build($"/// Generated raw binary receive invoker for <see cref=\"{method.Method.XmlDocs}\"> (Overload {method.Overload + 1}).")
                .String("/// </summary>")
                .String("/// <remarks>This method is responsible for parsing the data received by another party and invoking the receive method.</remarks>")
                .Empty();

            bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"private static unsafe void {method.ReceiveMethodNameBytes}(").In()
                    .String("object serviceProvider,")
                    .String("object targetObject,")
                    .String("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,")
                    .String("global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,")
                    .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,");

            if (method.Method.Target.Raw)
            {
                bldr.String("global::System.ReadOnlyMemory<byte> rawData,")
                    .String("bool canTakeOwnership,");
            }
            else
            {
                bldr.String("byte* bytes,")
                    .String("uint maxSize,");
            }
            
            bldr.String("global::System.Threading.CancellationToken token)").Out()
                .String("{").In();

            ReceiveMethodSnippetGenerator generator = new ReceiveMethodSnippetGenerator(Context, Compilation, method, Class);
            generator.GenerateMethodBodySnippet(bldr, stream: false);

            bldr.Out()
                .String("}");

            bldr.Empty()
                .String("/// <summary>")
                .Build($"/// Generated stream receive invoker for <see cref=\"{method.Method.XmlDocs}\"> (Overload {method.Overload + 1}).")
                .String("/// </summary>")
                .String("/// <remarks>This method is responsible for parsing the data received by another party and invoking the receive method.</remarks>")
                .Empty();

            bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"private static unsafe void {method.ReceiveMethodNameStream}(").In()
                    .String("object serviceProvider,")
                    .String("object targetObject,")
                    .String("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,")
                    .String("global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,")
                    .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,")
                    .String("global::System.IO.Stream stream,")
                    .String("global::System.Threading.CancellationToken token)").Out()
                .String("{").In();

            generator.GenerateMethodBodySnippet(bldr, stream: true);

            bldr.Out()
                .String("}")
                .Empty();

            if (method.ClosureTypeName == null || method.IsDuplicateClosure)
                continue;

            AwaitableInfo awaitableInfo = method.Method.ReturnTypeAwaitableInfo!.Value;

            bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"private sealed class {method.ClosureTypeName} : DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GeneratedClosure")
                .String("{").In()
                    .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"private {awaitableInfo.AwaiterType.GloballyQualifiedName} _awaiter;")
                    .Empty()
                    .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .Build($"public {method.ClosureTypeName}(global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead, global::DanielWillett.ModularRpcs.Routing.IRpcRouter router, global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer, {awaitableInfo.AwaiterType.GloballyQualifiedName} awaiter)")
                    .In().String(": base(overhead, router, serializer)").Out()
                    .String("{").In()
                        .String("this._awaiter = awaiter;");

            if (awaitableInfo.CriticalOnCompleted)
            {
                bldr.String(awaitableInfo.OnCompletedIsExplicit
                    ? "((global::System.Runtime.CompilerServices.ICriticalNotifyCompletion)this._awaiter).UnsafeOnCompleted(new global::System.Action(Continuation));"
                    : "this._awaiter.UnsafeOnCompleted(new global::System.Action(Continuation));");
            }
            else
            {
                bldr.String(awaitableInfo.OnCompletedIsExplicit
                    ? "((global::System.Runtime.CompilerServices.INotifyCompletion)this._awaiter).OnCompleted(new global::System.Action(Continuation));"
                    : "this._awaiter.OnCompleted(new global::System.Action(Continuation));");
            }

            bldr.Out()
                    .String("}")
                    .Empty()
                    .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                    .String("private void Continuation()")
                    .String("{").In();

            generator.GenerateContinuationBody(bldr, "this.Overhead", "this.Router", "this.Serializer", "this._awaiter");

            bldr.Out()
                    .String("}")
                    .Out()
                .String("}");
        }
        bldr.String("#endregion");
        
        foreach (ReceiveMethodInfo method in recvMethods)
        {
            if (method.DelegateType == null || method.IsDuplicateDelegateType)
                continue;

            method.DelegateType.WriteDefinition(bldr, Accessibility.Private, method.DelegateName!);
        }
        foreach (SendMethodInfo method in sendMethods)
        {
            if (method.DelegateType == null || method.IsDuplicateDelegateType)
                continue;

            method.DelegateType.WriteDefinition(bldr, Accessibility.Private, method.DelegateName!);
        }

        bldr.Out()
            .String("}");

        if (Class.NestedParents is not null)
        {
            foreach (string _ in Class.NestedParents)
            {
                bldr.Out().String("}");
            }
        }

        if (Class.Type.Namespace != null)
        {
            bldr.Out()
                .String("}");
        }

        bldr.Preprocessor("#nullable restore");

        Context.AddSource(Class.Type.FullyQualifiedName, SourceText.From(bldr.ToString(), Encoding.UTF8));
    }

    private bool TryGetReceiveMethod(RpcMethodDeclaration send, out RpcMethodDeclaration receive)
    {
        RpcTargetAttribute target = send.Target;
        receive = null!;
        if (string.IsNullOrEmpty(target.MethodName))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(target.TypeName) && !TypeHelper.TypesEqual(target.TypeName.AsSpan(), Class.Type.AssemblyQualifiedName.AsSpan()))
        {
            return false;
        }

        if (target.ParameterTypeNames == null)
        {
            receive = Class.Methods.Find(x => x.IsReceive && string.Equals(x.Name, target.MethodName, StringComparison.Ordinal));
            return receive != null;
        }

        receive = Class.Methods.Find(x =>
        {
            if (!x.IsReceive || !string.Equals(x.Name, target.MethodName, StringComparison.Ordinal))
                return false;

            IList<RpcParameterDeclaration> checkParameters = x.Parameters;
            if (target.ParametersAreBindedParametersOnly)
            {
                ParameterHelper.BindParameters(x.Parameters, out _, out RpcParameterDeclaration[] toBind);
                checkParameters = toBind;
            }

            if (checkParameters.Count != target.ParameterTypeNames.Length)
                return false;

            for (int i = 0; i < checkParameters.Count; ++i)
            {
                string p0 = target.ParameterTypeNames[i];
                RpcParameterDeclaration p1 = checkParameters[i];

                if (!TypeHelper.TypesEqual(p0.AsSpan(), p1.Type.AssemblyQualifiedName.AsSpan()))
                {
                    return false;
                }
            }

            return true;
        });
        return receive != null;
    }
}