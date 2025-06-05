using System;
using System.Collections.Generic;
using System.Text;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal class ClassSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcClassDeclaration Class;

    internal ClassSnippetGenerator(SourceProductionContext context, RpcClassDeclaration @class)
    {
        Context = context;
        Class = @class;
    }


    public SourceText GenerateClassSnippet()
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        StringBuilder sb = new StringBuilder(2048);

        string ns = NamespaceHelper.SanitizeNamespace(Class.Namespace);

        sb.Append(    "namespace ").AppendLine(ns);
        sb.AppendLine("{");
        sb.Append(    "    partial ")
            .Append(Class.IsStruct ? "struct" : "class")
            .Append(" @")
            .Append(Class.Name)
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
                       
                       """).Append(Class.Name).AppendLine("), out this._modularRpcsGeneratedProxyContext);")
          .Append(    "        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString());
    }
}
