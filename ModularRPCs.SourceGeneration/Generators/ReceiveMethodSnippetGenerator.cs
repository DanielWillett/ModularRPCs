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


    public void GenerateMethodBodySnippetBytes(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();
    }
    public void GenerateMethodBodySnippetStream(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();
    }
}

internal struct ReceiveMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;

    public ReceiveMethodInfo(RpcMethodDeclaration method, int overload)
    {
        Method = method;
        Overload = overload;
    }
}