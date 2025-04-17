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
public class ReturnTypeValue
{
    private static bool _wasInvoked;
    private const string RtnValue = "test";

    [Test]
    public async Task ServerToClientBytes()
    {
        _wasInvoked = false;

        LoopbackRpcServersideRemoteConnection connection
            = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

        TestClass proxy = server.GetRequiredService<TestClass>();

        string rtnValue = await proxy.InvokeFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        string rtnValue = await proxy.InvokeFromClient();

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

        string rtnValue = await proxy.InvokeFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        string rtnValue = await proxy.InvokeFromClient();

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

        string rtnValue = await proxy.InvokeTaskFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskBytes()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        string rtnValue = await proxy.InvokeTaskFromClient();

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

        string rtnValue = await proxy.InvokeTaskFromServer(connection);

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }
    
    [Test]
    public async Task ClientToServerTaskStream()
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        string rtnValue = await proxy.InvokeTaskFromClient();

        Assert.That(_wasInvoked, Is.True);
        Assert.That(rtnValue, Is.EqualTo(RtnValue));
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(Receive))]
        public virtual RpcTask<string> InvokeFromClient() => RpcTask<string>.NotImplemented;

        [RpcSend(nameof(Receive))]
        public virtual RpcTask<string> InvokeFromServer(IModularRpcRemoteConnection connection) => RpcTask<string>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<string> InvokeTaskFromClient() => RpcTask<string>.NotImplemented;

        [RpcSend(nameof(ReceiveTask))]
        public virtual RpcTask<string> InvokeTaskFromServer(IModularRpcRemoteConnection connection) => RpcTask<string>.NotImplemented;

        [RpcReceive]
        private string Receive()
        {
            _wasInvoked = true;

            return RtnValue;
        }

        [RpcReceive]
        private async Task<string> ReceiveTask()
        {
            _wasInvoked = true;

            await Task.Delay(1);

            return RtnValue;
        }
    }
}
