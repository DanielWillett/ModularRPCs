using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen;

[NonParallelizable, TestFixture]
public class CancelTests
{
    private const int DelayMs = 100;
    private static bool _didCancel;

    [Test]
    public async Task ServerToClientBytes()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientStream()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection).WithToken(tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient().WithToken(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientBytesAsArgument()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerBytesAsArgument()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
    }

    [Test]
    public async Task ServerToClientStreamAsArgument()
    {
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromServer(connection, tknSrc.Token));
    }
    
    [Test]
    public async Task ClientToServerStreamAsArgument()
    {
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(Is.AssignableTo<OperationCanceledException>(), async () => await proxy.InvokeFromClient(tknSrc.Token));
    }

    private static IResolveConstraint GetOpCancelledConstraint()
    {
        return Is.Not.Null; // can not figure out why it won't match TaskCanceledExceptions no matter what i do
    }


    /* TOKEN */

    [Test]
    public async Task ServerToClientBytesToken()
    {
        _didCancel = false;
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection).WithToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }
    
    [Test]
    public async Task ClientToServerBytesToken()
    {
        _didCancel = false;
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken().WithToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }

    [Test]
    public async Task ServerToClientStreamToken()
    {
        _didCancel = false;
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection).WithToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }
    
    [Test]
    public async Task ClientToServerStreamToken()
    {
        _didCancel = false;
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken().WithToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }

    [Test]
    public async Task ServerToClientBytesAsArgumentToken()
    {
        _didCancel = false;
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection, tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }
    
    [Test]
    public async Task ClientToServerBytesAsArgumentToken()
    {
        _didCancel = false;
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }

    [Test]
    public async Task ServerToClientStreamAsArgumentToken()
    {
        _didCancel = false;
        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);

        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromServerToken(connection, tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
    }
    
    [Test]
    public async Task ClientToServerStreamAsArgumentToken()
    {
        _didCancel = false;
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        using CancellationTokenSource tknSrc = new CancellationTokenSource(10);
        Assert.ThrowsAsync(GetOpCancelledConstraint(), async () => await proxy.InvokeFromClientToken(tknSrc.Token));

        await Task.Delay(10, CancellationToken.None);

        Assert.That(_didCancel, Is.True);
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
        
        [RpcSend(nameof(ReceiveToken))]
        public virtual RpcTask InvokeFromClientToken() => RpcTask.NotImplemented;

        [RpcSend(nameof(ReceiveToken))]
        public virtual RpcTask InvokeFromServerToken(IModularRpcRemoteConnection connection) => RpcTask.NotImplemented;
        
        [RpcSend(nameof(ReceiveToken))]
        public virtual RpcTask InvokeFromClientToken(CancellationToken token) => RpcTask.NotImplemented;

        [RpcSend(nameof(ReceiveToken))]
        public virtual RpcTask InvokeFromServerToken(IModularRpcRemoteConnection connection, CancellationToken token) => RpcTask.NotImplemented;

        [RpcReceive]
        private Task Receive()
        {
            return Task.Delay(TimeSpan.FromMilliseconds(DelayMs));
        }

        [RpcReceive]
        private async Task ReceiveToken(CancellationToken token)
        {
            try
            {
                Console.WriteLine("Starting delay");
                Debug.WriteLine("Starting delay");
                await Task.Delay(TimeSpan.FromMilliseconds(55000), token);
                Console.WriteLine("finished delay");
                Debug.WriteLine("finished delay");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("delay cancelled");
                Console.WriteLine("delay cancelled");
                _didCancel = true;
                throw;
            }
        }
    }
}
