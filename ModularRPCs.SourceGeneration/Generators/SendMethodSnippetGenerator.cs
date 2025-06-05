using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct SendMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;

    internal SendMethodSnippetGenerator(SourceProductionContext context, RpcMethodDeclaration method)
    {
        Context = context;
        Method = method;
        Send = (RpcSendAttribute)method.Target;
    }

    public void GenerateClassSnippet()
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        StringBuilder sb = new StringBuilder(2048);

        string ns = NamespaceHelper.SanitizeNamespace(Method.Type.Namespace);

        sb.Append(    "namespace ").AppendLine(ns);
        sb.AppendLine("{");
        sb.Append(    "    partial ")
            .Append(Method.Type.IsStruct ? "struct" : "class")
            .Append(" @")
            .Append(Method.Type.Name)
            .Append(" : global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType");
        sb.AppendLine("    {");
        sb.Append(     """
                               [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                               private global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo _modularRpcsGeneratedProxyTypeInfo;
                               
                               [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                               private global::DanielWillett.ModularRpcs.Reflection.ProxyContext _modularRpcsGeneratedProxyContext;
                               
                               
                               [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                               void global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType.SetupGeneratedProxyInfo(
                                   global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo info)
                               {
                                   this._modularRpcsGeneratedProxyTypeInfo = info;
                                   info.Router.GetDefaultProxyContext(typeof(
                       
                       """).Append(Method.Type.Name).AppendLine("), out this._modularRpcsGeneratedProxyContext);")
          .Append(    "        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        Context.AddSource(Method.Type.Namespace == null
            ? $"{Method.Type.Name}.Send_{Method.Name}"
            : $"{Method.Type.Namespace}.{Method.Type.Name}.Send_{Method.Name}",
            SourceText.From(sb.ToString())
        );
    }
}
