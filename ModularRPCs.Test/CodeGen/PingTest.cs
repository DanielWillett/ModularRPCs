using DanielWillett.ModularRpcs.Annotations;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.Loopback;

namespace ModularRPCs.Test.CodeGen
{
    [NonParallelizable, TestFixture, GenerateRpcSource]
    public partial class PingTest
    {
        private IDisposable _disposable;

        [TearDown]
        public void TearDown()
        {
            _disposable?.Dispose();
        }

        [Test]
        public async Task Ping([Values(true, false)] bool useStreams)
        {
            LoopbackRpcServersideRemoteConnection remote =
                await TestSetup.SetupTest<PingTest>(out _, out _, useStreams, out _disposable);

            TimeSpan pingDuration = await remote.PingAsync();

            Console.WriteLine(pingDuration);
        }
    }
}
