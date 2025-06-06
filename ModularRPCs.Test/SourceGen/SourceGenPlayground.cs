using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen;

internal class SourceGenPlayground
{
    internal static int DidInvokeMethod;

    internal const int Val1 = 32;
    internal const string Val2 = "test string";

    [Test]
    public async Task ServerToClientVoid([Values(true, false)] bool useStreams)
    {
        DidInvokeMethod = -1;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out IServiceProvider server, out _, useStreams);

        SourceGenPlaygroundTestClass proxy = server.GetRequiredService<SourceGenPlaygroundTestClass>();

        await proxy.InvokeFromServer(connection);

        Assert.That(DidInvokeMethod, Is.EqualTo(0));
    }

    [Test]
    public async Task ClientToServerVoid([Values(true, false)] bool useStreams)
    {
        DidInvokeMethod = -1;

        await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out _, out IServiceProvider client, useStreams);

        SourceGenPlaygroundTestClass proxy = client.GetRequiredService<SourceGenPlaygroundTestClass>();

        await proxy.InvokeFromClient();

        Assert.That(DidInvokeMethod, Is.EqualTo(0));
    }

    [Test]
    public async Task ServerToClientTask([Values(true, false)] bool useStreams)
    {
        DidInvokeMethod = -1;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out IServiceProvider server, out _, useStreams);

        SourceGenPlaygroundTestClass proxy = server.GetRequiredService<SourceGenPlaygroundTestClass>();

        await proxy.InvokeWithParamsFromServer(Val1, Val2, connection);

        Assert.That(DidInvokeMethod, Is.EqualTo(1));
    }

    [Test]
    public async Task ClientToServerTask([Values(true, false)] bool useStreams)
    {
        DidInvokeMethod = -1;

        await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out _, out IServiceProvider client, useStreams);

        SourceGenPlaygroundTestClass proxy = client.GetRequiredService<SourceGenPlaygroundTestClass>();

        await proxy.InvokeWithParamsFromClient(Val1, Val2);

        Assert.That(DidInvokeMethod, Is.EqualTo(1));
    }
}


[RpcClass, GenerateRpcSource]
public sealed partial class SourceGenPlaygroundTestClass
{
    [RpcSend(nameof(Receive))]
    public partial RpcTask InvokeFromClient();

    [RpcSend(nameof(Receive))]
    public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

    [RpcReceive]
    private void Receive()
    {
        SourceGenPlayground.DidInvokeMethod = 0;
    }

    [RpcSend(nameof(ReceiveWithParams))]
    public partial RpcTask InvokeWithParamsFromClient(int primitiveLikeValue, string nonPrimitiveLikeValue);

    [RpcSend(nameof(ReceiveWithParams))]
    public partial RpcTask InvokeWithParamsFromServer(int primitiveLikeValue, string nonPrimitiveLikeValue, IModularRpcRemoteConnection connection);

    [RpcReceive]
    private async Task ReceiveWithParams(int primitiveLikeValue, string nonPrimitiveLikeValue)
    {
        await Task.Delay(5);
        SourceGenPlayground.DidInvokeMethod = 1;
        Assert.That(primitiveLikeValue, Is.EqualTo(SourceGenPlayground.Val1));
        Assert.That(nonPrimitiveLikeValue, Is.EqualTo(SourceGenPlayground.Val2));
    }
}