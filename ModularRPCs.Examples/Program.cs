using DanielWillett.ModularRpcs.DependencyInjection;
using DanielWillett.ModularRpcs.Examples;
using DanielWillett.ModularRpcs.Examples.Samples;
using DanielWillett.ModularRpcs.WebSockets;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    //public static async Task Run(string[] args)
    //{
    //    Accessor.LogILTraceMessages = true;
    //    Accessor.LogDebugMessages = true;
    //    Accessor.LogInfoMessages = true;
    //    Accessor.LogWarningMessages = true;
    //    Accessor.LogErrorMessages = true;

    //    IModularRpcRemoteEndpoint clientEndpoint = new LoopbackEndpoint(false);
    //    IModularRpcRemoteEndpoint serverEndpoint = new LoopbackEndpoint(true);

    //    DefaultRpcRouter clientRouter = new DefaultRpcRouter(new DefaultSerializer(), new ClientRpcConnectionLifetime());
    //    DefaultRpcRouter serverRouter = new DefaultRpcRouter(new DefaultSerializer(), new ServerRpcConnectionLifetime());

    //    IModularRpcRemoteConnection loopbackRemote = await clientEndpoint.RequestConnectionAsync(clientRouter, clientRouter.ConnectionLifetime, clientRouter.Serializer, CancellationToken.None);

    //    SampleClass sc0 = ProxyGenerator.Instance.CreateProxy<SampleClass>(clientRouter);

    //    int i = 3;
    //    nint val = 5;
    //    string str = "test";
    //    DateTime dt = DateTime.UtcNow;

    //    int result = await sc0.CallRpcOne(loopbackRemote, [ "str1", "str2", "str3", "dining time" ]);
    //    Console.WriteLine($"Result: {result}.");
    //}

    public static async Task Run(string[] args)
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
            serviceCollection.AddModularRpcs(isServer: false);

            serviceCollection.AddRpcTransient<WebSocketsTestClass>();
            serviceCollection.AddTransient<WebSocketsConnector>();

            serviceCollection.AddLogging();
        });

        IHost host = hostBuilder.Build();


        WebSocketClientsideRemoteRpcConnection? connection = await host.Services.GetRequiredService<WebSocketsConnector>().ConnectAsync();

        if (connection == null)
            throw new Exception("conn not found.");

        WebSocketsTestClass testClass = host.Services.GetRequiredService<WebSocketsTestClass>();

        while (!string.Equals(Console.ReadLine(), "exit", StringComparison.InvariantCultureIgnoreCase))
        {
            try
            {
                Console.WriteLine(await testClass.SendTestFunc());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling RPC");
                Console.WriteLine(ex);
            }
        }

        await Task.Delay(2500);
        await connection.CloseAsync();
    }
}