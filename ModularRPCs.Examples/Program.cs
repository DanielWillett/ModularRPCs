using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable LocalizableElement

public class Program
{
    public static async Task Main(string[] args)
    {
        await Run(args);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

        Thread.Sleep(10000);

        Console.ReadLine();
    }
    public static async Task Run(string[] args)
    {
        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        IModularRpcRemoteEndpoint clientEndpoint = new LoopbackEndpoint(false);
        IModularRpcRemoteEndpoint serverEndpoint = new LoopbackEndpoint(true);

        DefaultRpcRouter clientRouter = new DefaultRpcRouter(new DefaultSerializer(), new ClientRpcConnectionLifetime());
        DefaultRpcRouter serverRouter = new DefaultRpcRouter(new DefaultSerializer(), new ServerRpcConnectionLifetime());

        IModularRpcRemoteConnection loopbackRemote = await clientEndpoint.RequestConnectionAsync(clientRouter, clientRouter.ConnectionLifetime, clientRouter.Serializer, CancellationToken.None);

        SampleClass sc0 = ProxyGenerator.Instance.CreateProxy<SampleClass>(clientRouter);

        int i = 3;
        nint val = 5;
        string str = "test";
        DateTime dt = DateTime.UtcNow;

        int result = await sc0.CallRpcOne(loopbackRemote, null, val, null, dt);
        Console.WriteLine($"Result: {result}.");
    }

    //public static void Run(string[] args)
    //{
    //    IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
    //
    //    Accessor.LogILTraceMessages = true;
    //    Accessor.LogDebugMessages = true;
    //    Accessor.LogInfoMessages = true;
    //    Accessor.LogWarningMessages = true;
    //    Accessor.LogErrorMessages = true;
    //
    //    hostBuilder.ConfigureServices(serviceCollection =>
    //    {
    //        serviceCollection.AddReflectionTools();
    //        serviceCollection.AddProxyGenerator();
    //
    //        serviceCollection.AddRpcTransient<SampleClass>();
    //        //serviceCollection.AddRpcTransient<SampleKeysNullable>();
    //        //serviceCollection.AddRpcTransient<SampleKeysNullableOtherField>();
    //        //serviceCollection.AddRpcTransient<SampleKeysNullableNoField>();
    //        //serviceCollection.AddRpcTransient<SampleKeysString>();
    //    });
    //
    //    IHost host = hostBuilder.Build();
    //
    //    SampleClass sc0 = host.Services.GetRequiredService<SampleClass>();
    //
    //    int i = 3;
    //    SpinLock sl = default;
    //    RpcTask task = sc0.CallRpcOne(ref i, ref sl, "test");
    //
    //    bool didRelease = RpcObjectExtensions.Release(sc0);
    //    Console.WriteLine($"released: {didRelease}.");
    //    didRelease = RpcObjectExtensions.Release(sc0);
    //    Console.WriteLine($"released: {didRelease}.");
    //    host.Dispose();
    //}
}