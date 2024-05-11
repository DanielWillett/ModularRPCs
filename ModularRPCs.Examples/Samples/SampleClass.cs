using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Invocation;
using DanielWillett.ModularRpcs.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;
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
    internal virtual RpcTask CallRpcOne(nint arg1, ref nint arg2) => RpcTask.NotImplemented;

    [RpcReceive]
    private async Task RpcOne(RpcInvocationContext ctx, int value, int dt, string str)
    {
        Console.WriteLine("Start");
        await Task.Delay(TimeSpan.FromSeconds(5d));
        Console.WriteLine("Done");
    }
    protected virtual bool Release()
    {
        Console.WriteLine("base release called");
        return false;
    }

    ~SampleClass()
    {
        Console.WriteLine("test");
    }
}