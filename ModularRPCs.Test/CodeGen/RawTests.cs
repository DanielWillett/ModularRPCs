using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.SpeedBytes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
// ReSharper disable LocalizableElement

namespace ModularRPCs.Test.CodeGen;
public class RawTests
{
    private static bool _wasInvoked;

    private const decimal Data = 3.5m;

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ByteArrayToByteArrayBytes(bool canTakeOwnership)
    {
        _wasInvoked = false;
        
        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, false);

        TestClass proxy = client.GetRequiredService<TestClass>();

        ByteWriter writer = new ByteWriter(16);
        writer.Write(Data);

        await proxy.InvokeByteArrayToByteArray(writer.ToArray(), canTakeOwnership);

        Assert.That(_wasInvoked, Is.True);
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    [Ignore("")]
    public async Task ByteArrayToByteArrayStream(bool canTakeOwnership)
    {
        _wasInvoked = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, true);

        TestClass proxy = client.GetRequiredService<TestClass>();

        ByteWriter writer = new ByteWriter(16);
        writer.Write(Data);

        await proxy.InvokeByteArrayToByteArray(writer.ToArray(), canTakeOwnership);

        Assert.That(_wasInvoked, Is.True);
    }

    [RpcClass]
    public class TestClass
    {
        [RpcSend(nameof(ReceiveByteArrayToByteArray), Raw = true)]
        public virtual RpcTask InvokeByteArrayToByteArray(byte[] bytes, bool canTakeOwnership) => RpcTask.NotImplemented;

        [RpcReceive(Raw = true)]
        private void ReceiveByteArrayToByteArray(byte[] bytes, bool canTakeOwnership)
        {
            ByteReader reader = new ByteReader();
            reader.LoadNew(bytes);

            decimal readData = reader.ReadDecimal();
            Assert.That(readData, Is.EqualTo(Data));

            Console.WriteLine($"Can take ownership: {canTakeOwnership}.");
        }
    }
}
