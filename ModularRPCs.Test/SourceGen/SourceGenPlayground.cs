using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Serialization;

namespace ModularRPCs.Test.SourceGen
{
    public class SourceGenPlayground
    {
        internal static int DidInvokeMethod;

        internal const int Val1 = 32;
        internal const string Val2 = "test string";

        [Test]
        public async Task ServerToClientVoid([Values(true, false)] bool useStreams)
        {
            DidInvokeMethod = -1;

            LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out IServiceProvider server, out _, useStreams);

            SourceGenPlaygroundTestClass proxy = server.GetRequiredService<SourceGenPlaygroundTestClass>();

            await proxy.InvokeFromServer(connection);

            Assert.That(DidInvokeMethod, Is.EqualTo(0));
        }

        [Test]
        public async Task ClientToServerVoid([Values(true, false)] bool useStreams)
        {
            DidInvokeMethod = -1;

            await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out _, out IServiceProvider client, useStreams);

            SourceGenPlaygroundTestClass proxy = client.GetRequiredService<SourceGenPlaygroundTestClass>();

            await proxy.InvokeFromClient();

            Assert.That(DidInvokeMethod, Is.EqualTo(0));
        }

        [Test]
        public async Task ServerToClientTask([Values(true, false)] bool useStreams)
        {
            DidInvokeMethod = -1;

            LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out IServiceProvider server, out _, useStreams);

            SourceGenPlaygroundTestClass proxy = server.GetRequiredService<SourceGenPlaygroundTestClass>();

            await proxy.InvokeWithParamsFromServer(Val1, Val2, connection, null);

            Assert.That(DidInvokeMethod, Is.EqualTo(1));
        }

        [Test]
        public async Task ClientToServerTask([Values(true, false)] bool useStreams)
        {
            DidInvokeMethod = -1;

            await TestSetup.SetupTest<SourceGenPlaygroundTestClass>(out _, out IServiceProvider client, useStreams);

            SourceGenPlaygroundTestClass proxy = client.GetRequiredService<SourceGenPlaygroundTestClass>();

            await proxy.InvokeWithParamsFromClient(Val1, Val2, null);

            Assert.That(DidInvokeMethod, Is.EqualTo(1));
        }
    }

    public class Nested<T>
    {
        [RpcSerializable(1, isFixedSize: true)]
        public struct TestIdType<TId> : IEquatable<TestIdType<TId>>, IRpcSerializable
        {
            public TId Id;

            public TestIdType(TId id)
            {
                Id = id;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is TestIdType<TId> t && EqualityComparer<TId>.Default.Equals(Id, t.Id);
            }

            /// <inheritdoc />
            public bool Equals(TestIdType<TId> other)
            {
                return EqualityComparer<TId>.Default.Equals(Id, other.Id);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return EqualityComparer<TId>.Default.GetHashCode(Id);
            }

            public int GetSize(IRpcSerializer serializer)
            {
                return sizeof(int);
            }

            public int Write(Span<byte> writeTo, IRpcSerializer serializer)
            {
                return serializer.WriteObject(Id, writeTo);
            }

            public int Read(Span<byte> readFrom, IRpcSerializer serializer)
            {
                Id = serializer.ReadObject<TId>(readFrom, out int bytesRead);
                return bytesRead;
            }
        }

    }

#nullable enable
    [RpcClass, GenerateRpcSource]
    public sealed partial class SourceGenPlaygroundTestClass : IRpcObject<Nested<int>.TestIdType<double>>
    {
        /// <inheritdoc />
        public Nested<int>.TestIdType<double> Identifier { get; private set; }
    
        [RpcSend(nameof(Receive))]
        public partial RpcTask InvokeFromClient();

        [RpcSend(nameof(Receive))]
        public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

        [RpcReceive]
        private void Receive()
        {
            SourceGenPlayground.DidInvokeMethod = 0;
        }

        [RpcSend(nameof(ReceiveWithParams))]
        public partial RpcTask InvokeWithParamsFromClient(int primitiveLikeValue, string? nonPrimitiveLikeValue, DateTime? nullableParam);

        [RpcSend(nameof(ReceiveWithParams))]
        public partial RpcTask InvokeWithParamsFromServer(int primitiveLikeValue, string? nonPrimitiveLikeValue, IModularRpcRemoteConnection connection, DateTime? nullableParam);

        [RpcReceive]
        private async Task ReceiveWithParams(int primitiveLikeValue, string? nonPrimitiveLikeValue, DateTime? nullableParam)
        {
            await Task.Delay(5);
            SourceGenPlayground.DidInvokeMethod = 1;
            Assert.That(primitiveLikeValue, Is.EqualTo(SourceGenPlayground.Val1));
            Assert.That(nonPrimitiveLikeValue, Is.EqualTo(SourceGenPlayground.Val2));
        }
    }
}