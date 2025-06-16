using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct SendRawMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;
    public readonly SendMethodInfo Info;

    internal SendRawMethodSnippetGenerator(SourceProductionContext context, SendMethodInfo method)
    {
        Context = context;
        Method = method.Method;
        Send = (RpcSendAttribute)method.Method.Target;
        Info = method;
    }


    public void GenerateMethodBodySnippet(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        EquatableList<RpcParameterDeclaration> parameters = Method.Parameters;



        bldr.String("global::System.Console.WriteLine(\"Hello World\");")
            .String("return default;");
    }
}