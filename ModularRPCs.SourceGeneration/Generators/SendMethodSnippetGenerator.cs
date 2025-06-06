using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct SendMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;
    public readonly SendMethodInfo Info;

    internal SendMethodSnippetGenerator(SourceProductionContext context, SendMethodInfo method)
    {
        Context = context;
        Method = method.Method;
        Send = (RpcSendAttribute)method.Method.Target;
        Info = method;
    }


    public void GenerateMethodBodySnippet(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        bldr.String("global::System.Console.WriteLine(\"Hello World\");")
            .String("return default;");
    }
}

internal struct SendMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;

    public SendMethodInfo(RpcMethodDeclaration method, int overload)
    {
        Method = method;
        Overload = overload;
    }
}