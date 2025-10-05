using System;
using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.NamedPipes;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ModularRPCs.Test.NamedPipes
{
    [TestFixture, GenerateRpcSource, NonParallelizable, RpcTimeout(Timeouts.Days)]
    public partial class NamedPipesTests
    {
        private static bool _didInvoke;

        private const string PipeName = "_MODRPCS_NUnit_TestPipe";


        [Test]
        public async Task TestOneConnection()
        {
            _didInvoke = false;

            IServiceProvider serverServices = TestServices.ForServer.WithProxy(out NamedPipesTests serverProxy);

            using NamedPipeServer server = NamedPipeEndpoint.AsServer(serverServices, PipeName);

            try
            {
                await server.CreateServerAsync();

                TestServices clientServices = TestServices.ForClient.WithProxy(out NamedPipesTests clientProxy);
                NamedPipeEndpoint clientEndpoint = NamedPipeEndpoint.AsClient(clientServices, PipeName);
                using NamedPipeClientsideRemoteRpcConnection client = await clientEndpoint.RequestConnectionAsync().ConfigureAwait(false);

                Assert.That(client, Is.Not.Null);
                Assert.That(client.IsClosed, Is.False);

                await serverProxy.InvokeOnClient(client);

                Assert.That(_didInvoke, Is.True);
                _didInvoke = false;

                //await clientProxy.InvokeOnServerLargeMessage(CreateLargeMessage());
                //Assert.That(_didInvoke, Is.True);

                await client.CloseAsync();

                Assert.That(client.IsClosed, Is.True);
            }
            finally
            {
                await server.CloseServerAsync();
            }
        }

        [RpcSend(nameof(ReceiveInvokeFromServer))]
        private partial RpcTask InvokeOnClient(IModularRpcRemoteConnection client);

        [RpcReceive]
        private void ReceiveInvokeFromServer()
        {
            _didInvoke = true;
        }

        [RpcSend(nameof(ReceiveInvokeFromClient))]
        private partial RpcTask InvokeOnServer();

        [RpcReceive]
        private void ReceiveInvokeFromClient()
        {
            _didInvoke = true;
        }

        private const int LargeMessageLength = 59102;

        private byte[] CreateLargeMessage()
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
            _didInvoke = true;
            Assert.That(largeMessage, Has.Length.EqualTo(LargeMessageLength));
            for (int i = 0; i < largeMessage.Length; ++i)
            {
                Assert.That(largeMessage[i], Is.EqualTo(unchecked ( (byte)(i * 2) )));
            }
        }
    }
}
