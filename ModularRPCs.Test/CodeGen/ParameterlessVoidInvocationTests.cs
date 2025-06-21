using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class ParameterlessVoidInvocationTests
    {
        private static bool _wasInvoked;
        [Test]
        public async Task ServerToClientBytes()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [Test]
        public async Task ServerToClientStream()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();

            Assert.That(_wasInvoked, Is.True);
        }



        [Test]
        public async Task ServerToClientTaskBytes()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerTaskBytes()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [Test]
        public async Task ServerToClientTaskStream()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerTaskStream()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [RpcClass]
        public class TestClass
        {
            [RpcSend(nameof(Receive))]
            public virtual RpcTask InvokeFromClient() => RpcTask.NotImplemented;

            [RpcSend(nameof(Receive))]
            public virtual RpcTask InvokeFromServer(IModularRpcRemoteConnection connection) => RpcTask.NotImplemented;

            [RpcSend(nameof(ReceiveTask))]
            public virtual RpcTask InvokeTaskFromClient() => RpcTask.NotImplemented;

            [RpcSend(nameof(ReceiveTask))]
            public virtual RpcTask InvokeTaskFromServer(IModularRpcRemoteConnection connection) => RpcTask.NotImplemented;

            [RpcReceive]
            private void Receive()
            {
                _wasInvoked = true;
            }

            [RpcReceive]
            private async Task ReceiveTask()
            {
                await Task.Delay(1);
                _wasInvoked = true;
            }
        }
    }
}
