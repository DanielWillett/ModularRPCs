using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using DanielWillett.ModularRpcs.Annotations;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal class ReceiveMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcReceiveAttribute Receive;

    internal ReceiveMethodSnippetGenerator(SourceProductionContext context, RpcMethodDeclaration method)
    {
        Context = context;
        Method = method;
        Receive = (RpcReceiveAttribute)method.Target;
    }


    public SourceText GenerateClassSnippet()
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        StringBuilder sb = new StringBuilder(2048);

        string ns = NamespaceHelper.SanitizeNamespace(Method.Namespace);

        sb.Append(    "namespace ").AppendLine(ns);
        sb.AppendLine("{");
        sb.Append(    "    partial ")
            .Append(Method.IsStruct ? "struct" : "class")
            .Append(" @")
            .Append(Method.Name)
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
                       
                       """).Append(Method.Name).AppendLine("), out this._modularRpcsGeneratedProxyContext);")
          .Append(    "        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString());
    }
}
