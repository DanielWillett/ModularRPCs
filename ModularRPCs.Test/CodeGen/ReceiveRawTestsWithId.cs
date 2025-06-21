using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable LocalizableElement

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class ReceiveRawTestsWithId
    {
        private static bool _wasInvoked;

        private const decimal Data = 3.5m;
        private static byte[] GetArray(TestClass cl, string name)
        {
            int skipSize = ProxyGenerator.Instance.CalculateOverheadSize(typeof(TestClass).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!.MethodHandle, cl, out _);

            ByteWriter writer = new ByteWriter(skipSize + 16);
            writer.Count += skipSize;
            writer.Write(Data);

            return writer.ToArray();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteArray");
            await proxy.InvokeByteArrayToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteArray");
            await proxy.InvokeByteArrayToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToStreamBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToStream");
            await proxy.InvokeByteArrayToStream(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToStreamStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToStream");
            await proxy.InvokeByteArrayToStream(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToByteReaderBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteReader");
            await proxy.InvokeByteArrayToByteReader(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToByteReaderStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteReader");
            await proxy.InvokeByteArrayToByteReader(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToArraySegmentBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToArraySegment");
            await proxy.InvokeByteArrayToArraySegment(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToArraySegmentStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToArraySegment");
            await proxy.InvokeByteArrayToArraySegment(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToMemoryBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToMemory");
            await proxy.InvokeByteArrayToMemory(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToMemoryStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToMemory");
            await proxy.InvokeByteArrayToMemory(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToReadOnlyMemoryBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToReadOnlyMemory");
            await proxy.InvokeByteArrayToReadOnlyMemory(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToReadOnlyMemoryStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToReadOnlyMemory");
            await proxy.InvokeByteArrayToReadOnlyMemory(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToSpanBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToSpan");
            await proxy.InvokeByteArrayToSpan(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToSpanStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToSpan");
            await proxy.InvokeByteArrayToSpan(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToReadOnlySpanBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToReadOnlySpan");
            await proxy.InvokeByteArrayToReadOnlySpan(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToReadOnlySpanStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToReadOnlySpan");
            await proxy.InvokeByteArrayToReadOnlySpan(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToPointerBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToPointer");
            await proxy.InvokeByteArrayToPointer(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToPointerStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToPointer");
            await proxy.InvokeByteArrayToPointer(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        private static void TestRead(ByteReader reader, bool canTakeOwnership)
        {
            decimal readData = reader.ReadDecimal();
            Assert.That(readData, Is.EqualTo(Data));

            Console.WriteLine($"Can take ownership: {canTakeOwnership}.");
            _wasInvoked = true;
        }

        [RpcClass]
        public class TestClass : IRpcObject<string>
        {
            [RpcDontUseBackingField]
            public string Identifier => "singleton";

            [RpcSend(nameof(ReceiveByteArrayToByteArray), Raw = true)]
            public virtual RpcTask InvokeByteArrayToByteArray(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;

            [RpcSend(nameof(ReceiveByteArrayToStream), Raw = true)]
            public virtual RpcTask InvokeByteArrayToStream(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToByteReader), Raw = true)]
            public virtual RpcTask InvokeByteArrayToByteReader(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToArraySegment), Raw = true)]
            public virtual RpcTask InvokeByteArrayToArraySegment(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToMemory), Raw = true)]
            public virtual RpcTask InvokeByteArrayToMemory(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToReadOnlyMemory), Raw = true)]
            public virtual RpcTask InvokeByteArrayToReadOnlyMemory(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToSpan), Raw = true)]
            public virtual RpcTask InvokeByteArrayToSpan(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToReadOnlySpan), Raw = true)]
            public virtual RpcTask InvokeByteArrayToReadOnlySpan(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArrayToPointer), Raw = true)]
            public virtual RpcTask InvokeByteArrayToPointer(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToByteArray(byte[] bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes);
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToStream(Stream stream, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(stream);
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToByteReader(ByteReader reader, bool canTakeOwnership)
            {
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToArraySegment(ArraySegment<byte> bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes);
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToMemory(Memory<byte> bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes.ToArray());
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToReadOnlyMemory(ReadOnlyMemory<byte> bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes.ToArray());
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToSpan(Span<byte> bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes.ToArray());
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private void ReceiveByteArrayToReadOnlySpan(ReadOnlySpan<byte> bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes.ToArray());
                TestRead(reader, canTakeOwnership);
            }

            [RpcReceive(Raw = true)]
            private unsafe void ReceiveByteArrayToPointer(byte* bytes, int byteCt, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();

                byte[] arr = new byte[byteCt];
                fixed (byte* dst = arr)
                    Buffer.MemoryCopy(bytes, dst, arr.Length, byteCt);

                reader.LoadNew(arr);
                TestRead(reader, canTakeOwnership);
            }
        }
    }
}
