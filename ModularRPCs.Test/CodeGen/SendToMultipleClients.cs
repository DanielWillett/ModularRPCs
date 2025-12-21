using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable CoVariantArrayConversion

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class SendToMultipleClients
    {
        private IDisposable _disposable;

        private static readonly List<IModularRpcRemoteConnection> TriggeredConnections = new List<IModularRpcRemoteConnection>();

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
            TriggeredConnections.Clear();
        }

        [SetUp]
        public void SetUp()
        {
            TriggeredConnections.Clear();
        }

        [Test]
        public async Task ServerToClientBytes([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connections);

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }
    
        [Test]
        public async Task ServerToClientStream([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(connections);

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }

        [Test]
        public async Task ServerToClientBytesClass([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServerClass(connections.ToList<IModularRpcRemoteConnection>());

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }

        [Test]
        public async Task ServerToClientStreamClass([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServerClass(connections.ToList<IModularRpcRemoteConnection>());

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }

        [Test]
        public async Task ServerToClientBytesStruct([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, false, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServerStruct(new ArraySegment<IModularRpcRemoteConnection>(connections));

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }

        [Test]
        public async Task ServerToClientStreamStruct([Range(0, 3)] int clients)
        {
            LoopbackRpcServersideRemoteConnection[] connections
                = await TestSetup.SetupTestWithMultipleClients<TestClass>(clients, out IServiceProvider server, out _, true, out _disposable);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServerStruct(new ArraySegment<IModularRpcRemoteConnection>(connections));

            Assert.That(TriggeredConnections, Has.Count.EqualTo(connections.Length));
            foreach (LoopbackRpcServersideRemoteConnection c in connections)
            {
                Assert.That(TriggeredConnections, Does.Contain(c.Client));
            }
        }

        public class TestClass
        {
            [RpcSend(nameof(Receive)), RpcFireAndForget]
            public virtual RpcTask InvokeFromServer(IEnumerable<IModularRpcRemoteConnection> connections) => RpcTask.NotImplemented;

            [RpcSend(nameof(Receive)), RpcFireAndForget]
            public virtual RpcTask InvokeFromServerClass(List<IModularRpcRemoteConnection> connections) => RpcTask.NotImplemented;

            [RpcSend(nameof(Receive)), RpcFireAndForget]
            public virtual RpcTask InvokeFromServerStruct(ArraySegment<IModularRpcRemoteConnection> connections) => RpcTask.NotImplemented;

            [RpcReceive]
            private void Receive(IModularRpcRemoteConnection fromConnection)
            {
                TriggeredConnections.Add(fromConnection);
            }
        }
    }
}
