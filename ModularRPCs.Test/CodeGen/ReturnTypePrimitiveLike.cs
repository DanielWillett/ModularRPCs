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
public class ReturnTypePrimitiveLike
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

        int rtnValue = await proxy.InvokeFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int rtnValue = await proxy.InvokeFromClient();

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

        int rtnValue = await proxy.InvokeFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int rtnValue = await proxy.InvokeFromClient();

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

        int rtnValue = await proxy.InvokeTaskFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int rtnValue = await proxy.InvokeTaskFromClient();

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

        int rtnValue = await proxy.InvokeTaskFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        int rtnValue = await proxy.InvokeTaskFromClient();

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive))]
        public virtual RpcTask<int> InvokeFromClient() => RpcTask<int>.NotImplemented;

        [RpcSend(nameof(Receive))]
        public virtual RpcTask<int> InvokeFromServer(IModularRpcRemoteConnection connection) => RpcTask<int>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<int> InvokeTaskFromClient() => RpcTask<int>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<int> InvokeTaskFromServer(IModularRpcRemoteConnection connection) => RpcTask<int>.NotImplemented;

        [RpcReceive]
        private int Receive()
        {
            _wasInvoked = true;

            return RtnValue;
        }

        [RpcReceive]
        private async Task<int> ReceiveTask()
        {
            _wasInvoked = true;

            await Task.Delay(1);

            return RtnValue;
        }
    }
}
