using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using DanielWillett.ModularRpcs.Reflection;

public class Program
{
    public static void Main(string[] args)
    {
        Run(args);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        Thread.Sleep(10000);

        Console.ReadLine();
    }

    public static void Run(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        hostBuilder.ConfigureServices(serviceCollection =>
        {
            serviceCollection.AddReflectionTools();
            serviceCollection.AddProxyGenerator();

            serviceCollection.AddRpcTransient<SampleClass>();
            //serviceCollection.AddRpcTransient<SampleKeysNullable>();
            //serviceCollection.AddRpcTransient<SampleKeysNullableOtherField>();
            //serviceCollection.AddRpcTransient<SampleKeysNullableNoField>();
            //serviceCollection.AddRpcTransient<SampleKeysString>();
        });

        IHost host = hostBuilder.Build();

        SampleClass sc0 = host.Services.GetRequiredService<SampleClass>();
        //SampleKeysNullable sc1 = host.Services.GetRequiredService<SampleKeysNullable>();
        //SampleKeysNullableNoField sc2 = host.Services.GetRequiredService<SampleKeysNullableNoField>();
        //SampleKeysString sc3 = host.Services.GetRequiredService<SampleKeysString>();
        //SampleKeysNullableOtherField sc4 = host.Services.GetRequiredService<SampleKeysNullableOtherField>();

        //SampleKeysString? testCs3 = (SampleKeysString?)ProxyGenerator.Instance.GetObjectByIdentifier(typeof(SampleKeysString), "test")?.Target;
        //
        //if (!ReferenceEquals(testCs3, sc3))
        //    throw new Exception("bad");
        //
        //bool didRelease = sc1.Release();
        //Console.WriteLine($"released: {didRelease}.");
        //didRelease = sc1.Release();
        //Console.WriteLine($"released: {didRelease}.");

        bool didRelease = RpcObjectExtensions.Release(sc0);
        Console.WriteLine($"released: {didRelease}.");
        didRelease = RpcObjectExtensions.Release(sc0);
        Console.WriteLine($"released: {didRelease}.");
        host.Dispose();
    }
}