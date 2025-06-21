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
    public class AssortedParameterInvocationTests
    {
        private static bool _wasInvoked;
        private const int Arg1 = 3;
        private const decimal Arg2 = 459395.957357m;
        private const string Arg5 = "test string1";
        private static readonly int[] Arg6 = new[] { 3, 4, 5, 6 };
        private static readonly DateTime[] Arg7 = new[]
        {
            new DateTime(2022, 3, 5, 12, 11, 53, DateTimeKind.Local), new DateTime(2024, 1, 19, 1, 8, 20, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Unspecified)
        };
        private static readonly string[] Arg8 = new[] { "test string1", "test string2", null, "test string4" };

        [Test]
        public async Task ServerToClientBytes()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, false);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(Arg1, Arg2, null, null, Arg5, Arg6, Arg7, Arg8, connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerBytes()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient(Arg1, Arg2, null, null, Arg5, Arg6, Arg7, Arg8);

            Assert.That(_wasInvoked, Is.True);
        }

        [Test]
        public async Task ServerToClientStream()
        {
            _wasInvoked = false;

            LoopbackRpcServersideRemoteConnection connection
                = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, true);

            TestClass proxy = server.GetRequiredService<TestClass>();

            await proxy.InvokeFromServer(Arg1, Arg2, null, null, Arg5, Arg6, Arg7, Arg8, connection);

            Assert.That(_wasInvoked, Is.True);
        }
    
        [Test]
        public async Task ClientToServerStream()
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            await proxy.InvokeFromClient(Arg1, Arg2, null, null, Arg5, Arg6, Arg7, Arg8);

            Assert.That(_wasInvoked, Is.True);
        }

        [RpcClass]
        public class TestClass
        {
            [RpcSend(nameof(Receive))]
            public virtual RpcTask InvokeFromClient(
                int valueType,
                decimal? nullableValueType,
                decimal? nullValueType,
                string nullableRefType, 
                string nonNullRefType,
                int[] valueArray,
                DateTime[] dtArray,
                string[] refArray)
            {
                return RpcTask.NotImplemented;
            }

            [RpcSend(nameof(Receive))]
            public virtual RpcTask InvokeFromServer(
                int valueType,
                decimal? nullableValueType,
                decimal? nullValueType,
                string nullableRefType,
                string nonNullRefType,
                int[] valueArray,
                DateTime[] dtArray,
                string[] refArray,
                IModularRpcRemoteConnection connection)
            {
                return RpcTask.NotImplemented;
            }

            [RpcReceive]
            private void Receive(
                int valueType,
                decimal? nullableValueType,
                decimal? nullValueType,
                string nullableRefType,
                string nonNullRefType,
                int[] valueArray,
                DateTime[] dtArray,
                string[] refArray
            )
            {
                _wasInvoked = true;
                Assert.That(valueType, Is.EqualTo(Arg1));
                Assert.That(nullableValueType, Is.EqualTo((decimal?)Arg2));
                Assert.That(nullValueType, Is.EqualTo(default(decimal?)));
                Assert.That(nullableRefType, Is.Null);
                Assert.That(nonNullRefType, Is.EqualTo(Arg5));
                Assert.That(valueArray, Is.EqualTo(Arg6));
                Assert.That(dtArray, Is.EqualTo(Arg7));
                Assert.That(refArray, Is.EqualTo(Arg8));
            }
        }
    }
}
