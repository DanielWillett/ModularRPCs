using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct ClassSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcClassDeclaration Class;
    public readonly EquatableList<RpcMethodDeclaration> MethodDeclarations;

    internal ClassSnippetGenerator(SourceProductionContext context, RpcClassDeclaration @class, EquatableList<RpcMethodDeclaration> methodDeclarations)
    {
        Context = context;
        Class = @class;
        MethodDeclarations = methodDeclarations;
    }

    public void GenerateClassSnippet()
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        SourceStringBuilder bldr = new SourceStringBuilder(2048, CultureInfo.InvariantCulture);

        List<SendMethodInfo> sendMethods = MethodDeclarations
            .Where(x => x.IsSend)
            .OrderBy(x => x.Name)
            .Select(x => new SendMethodInfo(x, -1))
            .ToList();

        List<ReceiveMethodInfo> recvMethods = MethodDeclarations
            .Where(x => x.IsReceive)
            .OrderBy(x => x.Name)
            .Select(x => new ReceiveMethodInfo(x, -1))
            .ToList();

        int overload = 0;
        string? last = null;
        for (int i = 0; i < recvMethods.Count; i++)
        {
            ReceiveMethodInfo info = recvMethods[i];
            if (!string.Equals(info.Method.Name, last, StringComparison.Ordinal))
                overload = 0;
            else
                ++overload;

            info.Overload = overload;
            recvMethods[i] = info;
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

            info.Overload = overload;
            sendMethods[i] = info;
        }

        // namespace {
        bldr.String("#nullable disable")
            .Build($"{Class.Type.NamespaceDeclaration}")
            .String("{").In();

        // class attributes
        bldr.String("[global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyTypeAttribute(");
        if (sendMethods.Count > 0)
        {
            bldr.In().Build($"TypeSetupMethodName = nameof({Class.Type.FullyQualifiedName}.ModularRpcsGeneratedSetupStaticGeneratedProxy)").Out();
        }
        bldr.String(")]");
        
        const string receiveInvokeNamePrefix = "ModularRpcsGeneratedInvoke";
        const string callMethodInfoFieldPrefix = "_modularRpcsGeneratedCallMethodInfo";

        foreach (ReceiveMethodInfo recvMethod in recvMethods)
        {
            bldr.String("[global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyReceiveMethodAttribute(")
                .In().Build($"nameof({Class.Type.FullyQualifiedName}.{recvMethod.Method.Name}),")
                     .Build($"nameof({Class.Type.FullyQualifiedName}.{receiveInvokeNamePrefix}{recvMethod.Method.Name}Ovl{recvMethod.Overload}Bytes),")
                     .Build($"nameof({Class.Type.FullyQualifiedName}.{receiveInvokeNamePrefix}{recvMethod.Method.Name}Ovl{recvMethod.Overload}Stream)").Out()
                .String(")]");
        }

        // class {
        bldr.Build($"partial {Class.Type.Definition} : global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType")
            .String("{").In()
                .Empty()
                .String("#region ModularRPCs class-level infrastructure")
                .Empty()
                .String("/// <summary>")
                .String("/// Stores information specific to generated proxy types that is needed to send and receive RPCs.")
                .String("/// </summary>")
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("private global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo _modularRpcsGeneratedProxyTypeInfo;")
                .Empty()
                .String("/// <summary>")
                .String("/// Stores generic information that is needed by all proxy types to send and receive RPCs.")
                .String("/// </summary>")
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("private global::DanielWillett.ModularRpcs.Reflection.ProxyContext _modularRpcsGeneratedProxyContext;")
                .Empty()
                .String("/// <summary>")
                .String("/// Invoked after this type is created by <see cref=\"M:DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CreateProxy\">.")
                .String("/// </summary>")
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("void global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType.SetupGeneratedProxyInfo(").In()
                    .String("global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo info)").Out()
                .String("{").In()
                    .String("this._modularRpcsGeneratedProxyTypeInfo = info;")
                    .Build($"info.Router.GetDefaultProxyContext(typeof({Class.Type.FullyQualifiedName}), out this._modularRpcsGeneratedProxyContext);").Out()
                .String("}")
                .Empty();

        // static init method

        if (sendMethods.Count > 0)
        {
            bldr.String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .String("private static void ModularRpcsGeneratedSetupStaticGeneratedProxy(").In()
                    .String("global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeBuilder state)").Out()
                .String("{").In();

            foreach (SendMethodInfo method in sendMethods)
            {
                if (Class.IsValueType)
                {
                    bldr.Build($"state.AddCallGetter(static () => {callMethodInfoFieldPrefix}{method.Method.Name}Ovl{method.Overload});");
                }
                else
                {
                    bldr.Build($"state.AddCallGetter(static () => ref {callMethodInfoFieldPrefix}{method.Method.Name}Ovl{method.Overload});");
                }
            }

            bldr.Out()
                .String("}");
        }


        bldr    .String("#endregion")
                .Empty();

        // send methods
        bldr.Empty()
            .Empty()
            .String("#region Generated send stubs");
        foreach (SendMethodInfo method in sendMethods)
        {
            bldr.Empty()
                .String("[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")
                .String("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]")
                .Build($"private static global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo {callMethodInfoFieldPrefix}{method.Method.Name}Ovl{method.Overload}").In()
                    .String("= global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo.FromCallMethod(").In()
                        .String("global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance,")
                        .Build($"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<global::System.Action<{Class.Type.FullyQualifiedName}>>(").In()
                            .Build($"@{Class.Type.Name} => @{Class.Type.Name}.@{method.Method.Name}(").In();

            bool needsComma = false;
            foreach (RpcParameterDeclaration parameter in method.Method.Parameters)
            {
                string comma = needsComma ? ", " : string.Empty;
                bldr.Build($"{comma}default({parameter.Type.FullyQualifiedName})");
                needsComma = true;
            }

            bldr        .Out()
                        .String(")").Out()
                    .String("),")
                    .String("false").Out()
                .String(");").Out();

            bldr.Empty()
                .String("/// <summary>")
                .Build($"/// Generated receive invoker for <see cref=\"{method.Method.XmlDocs}\"> (Overload {method.Overload + 1}).")
                .String("/// </summary>")
                .String("/// <remarks>This method is responsible for triggering the initial RPC invocation.</remarks>")
                .Empty();

            bldr.Build($"{method.Method.Definition}")
                .String("{").In();

            new SendMethodSnippetGenerator(Context, method)
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
                .Build($"private static unsafe void {receiveInvokeNamePrefix}{method.Method.Name}Ovl{method.Overload}Bytes(").In()
                    .String("object serviceProvider,")
                    .String("object targetObject,")
                    .String("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,")
                    .String("global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,")
                    .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,")
                    .String("byte* bytes,")
                    .String("uint maxSize,")
                    .String("global::System.Threading.CancellationToken token)").Out()
                .String("{").In();

            new ReceiveMethodSnippetGenerator(Context, method)
                .GenerateMethodBodySnippetBytes(bldr);

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
                .Build($"private static unsafe void {receiveInvokeNamePrefix}{method.Method.Name}Ovl{method.Overload}Stream(").In()
                    .String("object serviceProvider,")
                    .String("object targetObject,")
                    .String("global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,")
                    .String("global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,")
                    .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,")
                    .String("global::System.IO.Stream stream,")
                    .String("global::System.Threading.CancellationToken token)").Out()
                .String("{").In();

            new ReceiveMethodSnippetGenerator(Context, method)
                .GenerateMethodBodySnippetStream(bldr);

            bldr.Out()
                .String("}")
                .Empty();
        }
        bldr.String("#endregion").Out();

        bldr    .String("}").Out()
            .String("}")
            .String("#nullable restore");

        Context.AddSource(Class.Type.FileName, SourceText.From(bldr.ToString(), Encoding.UTF8));
    }
}