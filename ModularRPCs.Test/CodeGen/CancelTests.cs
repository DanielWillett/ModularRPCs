using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen;
public class CancelTests
{
    private const int DelayMs = 100;

    [Test]
    public async Task ServerToClientBytes()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientStream()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientBytesAsArgument()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerBytesAsArgument()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientStreamAsArgument()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerStreamAsArgument()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.TypeOf<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
        public virtual RpcTask InvokeFromClient() => RpcTask.NotImplemented;

        [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
        public virtual RpcTask InvokeFromServer(IModularRpcRemoteConnection connection) => RpcTask.NotImplemented;
        
        [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
        public virtual RpcTask InvokeFromClient(CancellationToken token) => RpcTask.NotImplemented;

        [RpcSend(nameof(Receive)), RpcTimeout(DelayMs)]
        public virtual RpcTask InvokeFromServer(IModularRpcRemoteConnection connection, CancellationToken token) => RpcTask.NotImplemented;

        [RpcReceive]
        private Task Receive()
        {
            return Task.Delay(TimeSpan.FromMilliseconds(DelayMs));
        }
    }
}
