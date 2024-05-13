using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using DanielWillett.ModularRpcs.Routing;

// ReSharper disable LocalizableElement

public class Program
{
    public static void Main(string[] args)
    {
        Run(args);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

        Thread.Sleep(10000);

        Console.ReadLine();
    }
    public static void Run(string[] args)
    {
        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        IRpcRouter router = new DefaultRpcRouter(new DefaultSerializer());
        
        SampleClass sc0 = ProxyGenerator.Instance.CreateProxy<SampleClass>(router);

        int i = 3;
        nint val = 5;
        string str = "test";
        DateTime dt = DateTime.UtcNow;
        //Type serializerType = ProxyGenerator.Instance.SerializerGenerator.GetSerializerType(3).MakeGenericType(typeof(int), typeof(SpinLock), typeof(string));
        //
        //RuntimeHelpers.RunClassConstructor(serializerType.TypeHandle);
        //
        //FieldInfo field = serializerType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
        //Delegate method = (Delegate)field.GetValue(null);
        //MethodInfo actualMethod = method.Method;
        //actualMethod = (DynamicMethod)actualMethod.GetType().GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actualMethod);
        //
        //int size = (int)actualMethod.Invoke(method, [ new Serializer(), 3, default(SpinLock), "test" ]);

        RpcTask task = sc0.CallRpcOne(i);

        bool didRelease = sc0.Release();
        Console.WriteLine($"released: {didRelease}.");

        didRelease = sc0.Release();
        Console.WriteLine($"released: {didRelease}.");
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