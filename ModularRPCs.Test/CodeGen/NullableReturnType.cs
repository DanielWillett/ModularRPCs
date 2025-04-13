using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen;

[NonParallelizable, TestFixture]
public class NullableReturnType
{
    private static bool _wasInvoked;
    private const int RtnValue = 3;

    [Test]
    public async Task ServerToClientBytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromServer(false, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromClient(false);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }

    [Test]
    public async Task ServerToClientStream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromServer(false, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromClient(false);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }


    [Test]
    public async Task ServerToClientTaskBytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromServer(false, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromClient(false);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }

    [Test]
    public async Task ServerToClientTaskStream()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromServer(false, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromClient(false);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }



    [Test]
    public async Task ServerToClientBytesNull()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromServer(true, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }
    
    [Test]
    public async Task ClientToServerBytesNull()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromClient(true);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }

    [Test]
    public async Task ServerToClientStreamNull()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromServer(true, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }
    
    [Test]
    public async Task ClientToServerStreamNull()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeFromClient(true);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }


    [Test]
    public async Task ServerToClientTaskBytesNull()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromServer(true, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }
    
    [Test]
    public async Task ClientToServerTaskBytesNull()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromClient(true);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }

    [Test]
    public async Task ServerToClientTaskStreamNull()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

        TestClass proxy = server.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromServer(true, connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }
    
    [Test]
    public async Task ClientToServerTaskStreamNull()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int? rtnValue = await proxy.InvokeTaskFromClient(true);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.Null);
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive))]
        public virtual RpcTask<int?> InvokeFromClient(bool useNull) => RpcTask<int?>.NotImplemented;

        [RpcSend(nameof(Receive))]
        public virtual RpcTask<int?> InvokeFromServer(bool useNull, IModularRpcRemoteConnection connection) => RpcTask<int?>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<int?> InvokeTaskFromClient(bool useNull) => RpcTask<int?>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<int?> InvokeTaskFromServer(bool useNull, IModularRpcRemoteConnection connection) => RpcTask<int?>.NotImplemented;

        [RpcReceive]
        private int? Receive(bool useNull)
        {
            _wasInvoked = true;

            return useNull ? null : RtnValue;
        }

        [RpcReceive]
        private async Task<int?> ReceiveTask(bool useNull)
        {
            _wasInvoked = true;

            await Task.Delay(1);

            return useNull ? null : RtnValue;
        }
    }
}
