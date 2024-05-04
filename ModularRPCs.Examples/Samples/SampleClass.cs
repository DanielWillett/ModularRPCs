using System;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Invocation;
using DanielWillett.ModularRpcs.Protocol;
// ReSharper disable LocalizableElement

namespace DanielWillett.ModularRpcs.Examples.Samples;

[RpcClass]
public class SampleClass : IRpcObject<int>
{
    private static int _identifier;
    public int Identifier { get; set; }
    public SampleClass()
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine("Called base ctor");
    }
    public SampleClass(string test1)
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine($"Called base ctor ({test1})");
    }
    public SampleClass(string test1, bool test2)
    {
        Identifier = Interlocked.Increment(ref _identifier);
        Console.WriteLine($"Called base ctor ({test1}, {test2})");
    }

    [RpcTimeout(10 * RpcTimeoutAttribute.Seconds)]
    [RpcSend]
    internal virtual RpcTask CallRpcOne() => RpcTask.NotImplemented;

    [RpcReceive]
    private async Task RpcOne(RpcInvocationContext ctx)
    {
        Console.WriteLine("Start");
        await Task.Delay(TimeSpan.FromSeconds(5d));
        Console.WriteLine("Done");
    }
}