using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable LocalizableElement

namespace ModularRPCs.Test.SourceGen
{
    [NonParallelizable, TestFixture]
    public partial class ReceiveRawTestsWithId
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

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

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

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

        [GenerateRpcSource]
        public partial class TestClass : IRpcObject<string>
        {
            [RpcDontUseBackingField]
            public string Identifier => "singleton";

            [RpcSend(nameof(ReceiveByteArrayToByteArray), Raw = true)]
            public partial RpcTask InvokeByteArrayToByteArray(byte[] bytes, bool canTakeOwnership);

            [RpcSend(nameof(ReceiveByteArrayToStream), Raw = true)]
            public partial RpcTask InvokeByteArrayToStream(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToByteReader), Raw = true)]
            public partial RpcTask InvokeByteArrayToByteReader(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToArraySegment), Raw = true)]
            public partial RpcTask InvokeByteArrayToArraySegment(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToMemory), Raw = true)]
            public partial RpcTask InvokeByteArrayToMemory(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToReadOnlyMemory), Raw = true)]
            public partial RpcTask InvokeByteArrayToReadOnlyMemory(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToSpan), Raw = true)]
            public partial RpcTask InvokeByteArrayToSpan(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToReadOnlySpan), Raw = true)]
            public partial RpcTask InvokeByteArrayToReadOnlySpan(byte[] bytes, bool canTakeOwnership);
        
            [RpcSend(nameof(ReceiveByteArrayToPointer), Raw = true)]
            public partial RpcTask InvokeByteArrayToPointer(byte[] bytes, bool canTakeOwnership);

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
