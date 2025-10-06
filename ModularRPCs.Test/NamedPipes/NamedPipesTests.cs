using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.NamedPipes;
using NUnit.Framework;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Routing;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable LocalizableElement

namespace ModularRPCs.Test.NamedPipes
{
    [TestFixture, GenerateRpcSource, NonParallelizable, RpcTimeout(Timeouts.Days)]
    public partial class NamedPipesTests
    {
        private static int _invokes;

        private const string PipeName = "_MODRPCS_NUnit_TestPipe";

        private class TestClient
        {
            private readonly TestServices _serverServices;

            public readonly TestServices ServiceProvider;
            public readonly NamedPipeEndpoint Endpoint;
            public NamedPipeClientsideRemoteRpcConnection Clientside;
            public NamedPipeServersideRemoteRpcConnection Serverside;
            public readonly NamedPipesTests Proxy;

            public int Hash => ServiceProvider.Router.GetHashCode();

            public TestClient(TestServices serverServices)
            {
                ServiceProvider = TestServices.ForClient.WithProxy(out Proxy);
                Endpoint = NamedPipeEndpoint.AsClient(ServiceProvider, PipeName);
                _serverServices = serverServices;
            }

            public async Task ConnectAsync()
            {
                _serverServices.Lifetime.ConnectionAdded += ConnectionAdded;
                try
                {
                    Clientside = await Endpoint.RequestConnectionAsync(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);

                    DateTime start = DateTime.UtcNow;
                    while (Serverside == null && (DateTime.UtcNow - start).TotalSeconds < 0.1)
                        await Task.Yield();

                    Assert.That(Clientside, Is.Not.Null);
                    Assert.That(Clientside.IsClosed, Is.False);
                }
                finally
                {
                    _serverServices.Lifetime.ConnectionAdded -= ConnectionAdded;
                }

                Assert.That(Serverside, Is.Not.Null);
                Assert.That(Serverside.IsClosed, Is.False);

            }

            private void ConnectionAdded(IRpcConnectionLifetime lifetime, IModularRpcRemoteConnection connection)
            {
                Serverside = (NamedPipeServersideRemoteRpcConnection)connection;
            }
        }


        [Test]
        public async Task TestOneConnection()
        {
            using StreamWriter writer = new StreamWriter(new FileStream(@"C:\Users\danny\OneDrive\Desktop\log.txt", FileMode.Create, FileAccess.Write, FileShare.Read, 1024, FileOptions.WriteThrough)) { AutoFlush = true };
            writer.WriteLine("Start");
            _invokes = 0;

            writer.WriteLine("1");
            TestServices serverServices = TestServices.ForServer.WithProxy(out NamedPipesTests serverProxy);
            writer.WriteLine("2");

            using NamedPipeServer server = NamedPipeEndpoint.AsServer(serverServices, PipeName);
            writer.WriteLine("3");

            try
            {
                await server.CreateServerAsync();
                writer.WriteLine("4");

                TestClient client = new TestClient(serverServices);
                await client.ConnectAsync();
                writer.WriteLine("5");

                await serverProxy.InvokeOnClient(client.Serverside);
                writer.WriteLine("6");

                Assert.That(_invokes, Is.EqualTo(1));
                _invokes = 0;

                await client.Proxy.InvokeOnServerLargeMessage(CreateLargeMessage());
                Assert.That(_invokes, Is.EqualTo(1));

                await client.Clientside.CloseAsync();

                Assert.That(client.Clientside.IsClosed, Is.True);
                await EnsureCloses(client.Serverside);
            }
            finally
            {
                await server.CloseServerAsync();
                _invokes = 0;
            }
        }

        [Test]
        public async Task TestMultipleSimultaneousConnections()
        {
            Console.WriteLine("A2");
            await Task.Delay(1000);
            //Debugger.Launch();
            _invokes = 0;

            TestServices serverServices = TestServices.ForServer.WithProxy(out NamedPipesTests serverProxy);

            using NamedPipeServer server = NamedPipeEndpoint.AsServer(serverServices, PipeName);

            try
            {
                await server.CreateServerAsync();

                TestClient client1 = new TestClient(serverServices);
                await client1.ConnectAsync();

                TestClient client2 = new TestClient(serverServices);
                await client2.ConnectAsync();

                Console.WriteLine($"Server: {serverServices.GetRequiredService<IRpcRouter>().GetHashCode()}");
                Console.WriteLine($"Client1: {client1.Hash}, Client2: {client2.Hash}");

                await serverProxy.InvokeOnClient(client1.Serverside);

                Assert.That(_invokes, Is.EqualTo(1));
                _invokes = 0;

                RpcTask t1 = client1.Proxy.InvokeOnServerLargeMessage(CreateLargeMessage());
                RpcTask t2 = client2.Proxy.InvokeOnServerLargeMessage(CreateLargeMessage());
                await t1;
                await t2;
                Assert.That(_invokes, Is.EqualTo(2));
                _invokes = 0;

                await serverProxy.InvokeOnClient(client2.Serverside);

                Assert.That(_invokes, Is.EqualTo(1));
                _invokes = 0;

                await serverProxy.InvokeOnClientCheckHashCode(client1.Serverside, client1.Proxy.GetHashCode());
                Assert.That(_invokes, Is.EqualTo(1));
                _invokes = 0;

                await serverProxy.InvokeOnClientCheckHashCode(client2.Serverside, client2.Proxy.GetHashCode());
                Assert.That(_invokes, Is.EqualTo(1));
                _invokes = 0;

                await client1.Clientside.CloseAsync();
                await client2.Clientside.CloseAsync();

                Assert.That(client1.Clientside.IsClosed, Is.True);
                Assert.That(client2.Clientside.IsClosed, Is.True);
                await EnsureCloses(client1.Serverside);
                await EnsureCloses(client2.Serverside);
            }
            finally
            {
                await server.CloseServerAsync();
                _invokes = 0;
            }
        }

        private static async Task EnsureCloses(IModularRpcServersideConnection connection)
        {
            DateTime start = DateTime.UtcNow;
            while (!connection.IsClosed && (DateTime.UtcNow - start).TotalMilliseconds < 500)
            {
                await Task.Yield();
            }

            Assert.That(connection.IsClosed);
        }

        [RpcSend(nameof(ReceiveInvokeFromServer))]
        private partial RpcTask InvokeOnClient(IModularRpcRemoteConnection client);

        [RpcReceive]
        private void ReceiveInvokeFromServer()
        {
            Interlocked.Increment(ref _invokes);
        }

        [RpcSend(nameof(ReceiveInvokeFromClient))]
        private partial RpcTask InvokeOnServer();

        [RpcReceive]
        private void ReceiveInvokeFromClient()
        {
            Interlocked.Increment(ref _invokes);
        }

        private const int LargeMessageLength = 59102;

        private static byte[] CreateLargeMessage()
        {
            byte[] largeMessage = new byte[LargeMessageLength];
            for (int i = 0; i < largeMessage.Length; ++i)
            {
                largeMessage[i] = unchecked( (byte)(i * 2) );
            }

            return largeMessage;
        }

        [RpcSend(nameof(ReceiveInvokeFromClientLargeMessage))]
        private partial RpcTask InvokeOnServerLargeMessage(byte[] largeMessage);

        [RpcReceive]
        private void ReceiveInvokeFromClientLargeMessage(byte[] largeMessage)
        {
            Assert.That(largeMessage, Has.Length.EqualTo(LargeMessageLength));
            for (int i = 0; i < largeMessage.Length; ++i)
            {
                Assert.That(largeMessage[i], Is.EqualTo(unchecked ( (byte)(i * 2) )));
            }
            Interlocked.Increment(ref _invokes);
        }

        [RpcSend(nameof(ReceiveInvokeOnClientCheckHashCode))]
        private partial RpcTask InvokeOnClientCheckHashCode(IModularRpcRemoteConnection client, int hashCode);

        [RpcReceive]
        private void ReceiveInvokeOnClientCheckHashCode(int hashCode)
        {
            Assert.That(GetHashCode(), Is.EqualTo(hashCode));
            Interlocked.Increment(ref _invokes);
        }
    }
}