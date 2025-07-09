using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable LocalizableElement

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture]
    public class SendRawTestsWithId
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

        private static bool _wasInvoked;

        private const decimal Data = 3.5m;
        private static byte[] GetArray(TestClass cl, string name, bool withHeader)
        {
            int skipSize = ProxyGenerator.Instance.CalculateOverheadSize(typeof(TestClass).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!.MethodHandle, cl, out _);

            if (withHeader)
            {
                ByteWriter writer = new ByteWriter(skipSize + 16);
                writer.Count += skipSize;
                writer.Write(Data);

                return writer.ToArray();
            }
            else
            {
                ByteWriter writer = new ByteWriter(16);
                writer.Write(Data);

                return writer.ToArray();
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteArrayToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteArray", true);
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

            byte[] arr = GetArray(proxy, "InvokeByteArrayToByteArray", true);
            await proxy.InvokeByteArrayToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task StreamToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeStreamToByteArray", false);
            await proxy.InvokeStreamToByteArray(new MemoryStream(arr, false), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task StreamToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeStreamToByteArray", false);
            await proxy.InvokeStreamToByteArray(new MemoryStream(arr, false), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteReaderToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteReaderToByteArray", false);
            ByteReader reader = new ByteReader();
            reader.LoadNew(arr);
            await proxy.InvokeByteReaderToByteArray(reader, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteReaderToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteReaderToByteArray", false);
            ByteReader reader = new ByteReader();
            reader.LoadNew(arr);
            await proxy.InvokeByteReaderToByteArray(reader, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteWriterToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteWriterToByteArray", true);
            ByteWriter writer = new ByteWriter();
            writer.WriteBlock(arr);
            await proxy.InvokeByteWriterToByteArray(writer, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ByteWriterToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeByteWriterToByteArray", true);
            ByteWriter writer = new ByteWriter();
            writer.WriteBlock(arr);
            await proxy.InvokeByteWriterToByteArray(writer, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ArraySegmentToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeArraySegmentToByteArray", true);
            await proxy.InvokeArraySegmentToByteArray(new ArraySegment<byte>(arr), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ArraySegmentToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeArraySegmentToByteArray", true);
            await proxy.InvokeArraySegmentToByteArray(new ArraySegment<byte>(arr), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task MemoryToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeMemoryToByteArray", true);
            await proxy.InvokeMemoryToByteArray(arr.AsMemory(), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task MemoryToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeMemoryToByteArray", true);
            await proxy.InvokeMemoryToByteArray(arr.AsMemory(), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReadOnlyMemoryToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeReadOnlyMemoryToByteArray", false);
            await proxy.InvokeReadOnlyMemoryToByteArray(arr.AsMemory(), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReadOnlyMemoryToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeReadOnlyMemoryToByteArray", false);
            await proxy.InvokeReadOnlyMemoryToByteArray(arr.AsMemory(), canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task SpanToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeSpanToByteArray", true);
            await proxy.InvokeSpanToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task SpanToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeSpanToByteArray", true);
            await proxy.InvokeSpanToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReadOnlySpanToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeReadOnlySpanToByteArray", false);
            await proxy.InvokeReadOnlySpanToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ReadOnlySpanToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            byte[] arr = GetArray(proxy, "InvokeReadOnlySpanToByteArray", false);
            await proxy.InvokeReadOnlySpanToByteArray(arr, canTakeOwnership);

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task PointerToByteArrayBytes(bool canTakeOwnership)
        {
            _wasInvoked = false;
        
            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            RpcTask task;
            unsafe
            {
                byte[] arr = GetArray(proxy, "InvokePointerToByteArray", true);
                fixed (byte* ptr = arr)
                    task = proxy.InvokePointerToByteArray(ptr, arr.Length, canTakeOwnership);
            }

            await task;

            Assert.That(_wasInvoked, Is.True);
            proxy.Release();
        }
    
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task PointerToByteArrayStream(bool canTakeOwnership)
        {
            _wasInvoked = false;

            await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true, out _disposable);

            TestClass proxy = client.GetRequiredService<TestClass>();

            RpcTask task;
            unsafe
            {
                byte[] arr = GetArray(proxy, "InvokePointerToByteArray", true);
                fixed (byte* ptr = arr)
                    task = proxy.InvokePointerToByteArray(ptr, arr.Length, canTakeOwnership);
            }

            await task;

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

        
        public class TestClass : IRpcObject<string>
        {
            [RpcDontUseBackingField]
            public string Identifier => "singleton";

            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeByteArrayToByteArray(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;

            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeStreamToByteArray(Stream stream, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeByteReaderToByteArray(ByteReader reader, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeByteWriterToByteArray(ByteWriter writer, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeArraySegmentToByteArray(ArraySegment<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeMemoryToByteArray(Memory<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeReadOnlyMemoryToByteArray(ReadOnlyMemory<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeSpanToByteArray(Span<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeReadOnlySpanToByteArray(ReadOnlySpan<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual unsafe RpcTask InvokePointerToByteArray(byte* bytes, int byteCt, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeEnumerableWithCountToByteArray(IEnumerable<byte> bytes, int byteCt, bool canTakeOwnership) => RpcTask.NotImplemented;
        
            [RpcSend(nameof(ReceiveByteArray), Raw = true)]
            public virtual RpcTask InvokeEnumerableWithoutCountToByteArray(IEnumerable<byte> bytes, bool canTakeOwnership) => RpcTask.NotImplemented;

            [RpcReceive(Raw = true)]
            private void ReceiveByteArray(byte[] bytes, bool canTakeOwnership)
            {
                ByteReader reader = new ByteReader();
                reader.LoadNew(bytes);
                TestRead(reader, canTakeOwnership);
            }
        }
    }
}
