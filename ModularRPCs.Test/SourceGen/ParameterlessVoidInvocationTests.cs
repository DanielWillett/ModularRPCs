using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class ParameterlessVoidInvocationTests
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

        private static bool _wasInvoked;
        [Test]
        public async Task ServerToClientBytes()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [Test]
        public async Task ServerToClientStream()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient();

            Assert.That(_wasInvoked, Is.True);
        }



        [Test]
        public async Task ServerToClientTaskBytes()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerTaskBytes()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [Test]
        public async Task ServerToClientTaskStream()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromServer(connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerTaskStream()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeTaskFromClient();

            Assert.That(_wasInvoked, Is.True);
        }

        [GenerateRpcSource]
        public partial class TestClass
        {
            [RpcSend(nameof(Receive))]
            public partial RpcTask InvokeFromClient();

            [RpcSend(nameof(Receive))]
            public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

            [RpcSend(nameof(ReceiveTask))]
            public partial RpcTask InvokeTaskFromClient();

            [RpcSend(nameof(ReceiveTask))]
            public partial RpcTask InvokeTaskFromServer(IModularRpcRemoteConnection connection);

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
