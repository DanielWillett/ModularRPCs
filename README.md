# ModularRPCs

Modular RPCs is a transparent communication layer between two CLR/.NET programs, optionally over a network.

It uses a Roslyn source generator or dynamic code generation to create stubs for send methods that write data to binary streams and send it over whatever medium is required.

It is currently in a semi-stable preview state, but has the majority of the features needed to be effective and is pretty well tested.
However, the code-base is very complex so there may still be bugs that haven't come to light yet.
I use the library in a few of my projects so it has some use 'in the field'.

## Features
* Dynamic IL code generation (TypeBuilders, DynamicMethods) for scenerios where a source generator can't be used.
  * Use `virtual` instead of `partial` for send methods.
* Remote-cancellation using a `CancellationToken`.
* Optimized 'Raw' send/receive methods that can directly send binary data without the overhead of serialization.
* Define custom serializers for any type.
* Built-in `IServiceProvider` support for fetching instances of types.
* Exception handling.
* Return values and exception handling using `RpcTask[<>]`.
  * An exception in a receive method will be rethrown by the send method if awaited.
* Multi-cast broadcasts
  * Doesn't support return values.
* First-class support for Unity components through the [DanielWillett.ModularRpcs.Unity](https://www.nuget.org/packages/DanielWillett.ModularRpcs.Unity) package.
* Roslyn incremental source generator.
* Legacy support all the way back to **.NET Standard** 2.0.
  * **.NET Framework** 4.6.1+, **.NET Core** 2.0+, **.NET** 5+, **Unity** 2018.1+, **Mono** 5.4+
  * Source generators for Roslyn 3.11, 4.0, 4.4, and 4.8. Source-gen should support [Unity version 2020.2 and later](https://docs.unity3d.com/Manual/roslyn-analyzers.html).
* Supports return types of any awaitable type (or regular values), not just `Task<>`.
  * Doesn't apply to awaitables that use extension members.
* Supports null values, nullable value types, collections, strings (UTF-8), and provides optimizations for primtive types.
* Separate optimized workflows for working with Streams or raw binary data to avoid unnecessary data copying.

### Missing Features
* Static functions.
* Automatic type serialization/deserialization.
* Support for other formats like JSON.
* Dictionary serialization.
* Awaitable types via extension members.
* TcpClient/TcpListener transport mode.
* Loopback optimization in source generator (skip serialization/deserialization step).

## Supported Transport Modes
Third party packages can be made fairly easily to add support for other transport modes. Take a look at some of the existing packages in this repository for an example.
* Loopback
    * Communicate with objects in the same process (app domain).
* Web Sockets
    * Communicate over the `ws://` or `wss://` protocols.
* Named Pipes
    * Communicate with objects usually on the same computer. Uses the [Named Pipes](https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication) API for interprocess communication.

## Supported Primitive Types
All the following types can be serialized/parsed individually or in an enumerable.

*Enum arrays and nullable arrays are not supported by default.*

### Base
* bool, char, double, float, int, long, uint, ulong, short, ushort, sbyte, byte
* nint, nuint (interpreted as 64-bit)
* Half (.NET 5+)
* Int128, UInt128 (.NET 7+)
* decimal
* DateTimeOffset
* DateTime
* TimeSpan
* Guid
* String (any encoding, UTF8 by default)
* All enums
* Nullable value types of any supported value type
* Collections of any supported value type.
* `IRpcSerializable` implementations

### ModularRPCs.Unity
* Vector2, Vector3, Vector4
* Bounds
* Color, Color32
* Quaternion
* Matrix4x4
* Plane
* Ray, Ray2D
* Rect
* Resolution

### MSBuild Properties

| Property                          | Description                          | Default |
| --------------------------------- | ------------------------------------ | ------- |
| DisableModularRPCsSourceGenerator | Disables source generation features. | False   |

## Installation
Install via [NuGet](https://www.nuget.org/packages/DanielWillett.ModularRpcs).
```xml
<ItemGroup>

    <PackageReference Include="DanielWillett.ModularRpcs" Version="*" />

    <!-- One of the following (or a third-party transport mode) -->
    <PackageReference Include="DanielWillett.ModularRpcs.NamedPipes" Version="*" />
    <PackageReference Include="DanielWillett.ModularRpcs.WebSockets" Version="*" />
    
    <!-- If using UnityEngine -->
    <PackageReference Include="DanielWillett.ModularRpcs.Unity" Version="*" />
    
</ItemGroup>
```

---

## Creating RPC Objects
An RPC object or a 'proxy object' is an object that's configured to use RPCs.

They're called proxy objects because the older dynamic code generator creates a subtype that overrides any virtual send methods.
With source generation this is no longer necessary but both types of code generation methods require that objects are created from the `ProxyGenerator`.

With dependency injection, you register types with `AddRpcTransient`, `AddRpcScoped`, and `AddRpcSingleton`, which takes care of everything for you.

Without dependency injection, you have to create RPC types using `ProxyGenerator.Instance.CreateProxy<T>()`. This is necessary to configure the object to use the right services and register all it's receive methods.

### Unity
Unity components are created with the extension methods `GameObject.AddRpcComponent` or `ProxyGenerator.Instance.CreateProxyComponent`.

Instantiating game objects with RPC components can be done using `ProxyGenerator.Instance.CreateInstantiatedProxy`.

## Injections
Special parameter types are automatically 'injected', meaning they're not considered values and are excluded from serialization/deserialization.

The following types are auto-injected for **receive methods**:

| Type                                | Behavior                                                                                                                                            |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| IRpcInvocationPoint                 | Injects the invocation point, usually of type [`RpcEndpoint`](https://github.com/DanielWillett/ModularRPCs/blob/master/ModularRPCs/RpcEndpoint.cs). |
| CancellationToken                   | A token which will be cancelled if the sending connection sends a cancellation notification.                                                        |
| RpcOverhead                         | The overhead information for the incoming message.                                                                                                  |
| RpcFlags                            | The options for the incoming message.                                                                                                               |
| IRpcSerializer                      | The serializer used to deserialize the message's values.                                                                                            |
| IRpcRouter                          | The router used to coordinate incoming and outgoing messages.                                                                                       |
| IModularRpcRemoteConnection         | The connection from which the message was sent from.                                                                                                |
| IServiceProvider                    | The service provider linked to the current IRpcRouter. If the router wasn't set up with dependency injection an error will be thrown.               |
| IEnumerable&lt;IServiceProvider&gt; | The service providers linked to the current IRpcRouter. If the router wasn't set up with dependency injection an error will be thrown.              |

Receive methods can decorate parameters of other types with `[RpcInject]` to have them injected from the service provider linked to the `IRpcRouter` that dispatched the message.

If any service can't be injected a `RpcInjectionException` will be thrown and returned to the sender.

The following types are auto-injected for **send methods**:

| Type                                           | Behavior                                                                                                                                            |
| ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| CancellationToken                              | A token which will be cancelled if the sending connection sends a cancellation notification.                                                        |
| IModularRpcLocalConnection*                    | The connection from which the message was sent from.                                                                                                |
| IEnumerable&lt;IModularRpcLocalConnection&gt;* | The service providers linked to the current IRpcRouter. If the router wasn't set up with dependency injection an error will be thrown.              |

\* Connections can also be injected by having the containing type implement `IRpcSingleConnectionObject` or `IRpcMultipleConnectionsObject` to define the default connections for RPCs in the given object.

For example, a game's primary player object may implement this interface to send messages to only that player by default.

## Examples

### Standard registration of services
#### Server
The following example registers the necessary services on a server-like process using extension methods and sets up the server using the Named Pipes transport mode.
```cs

IServiceCollection collection = new ServiceCollection()
    .AddLogging(l => l.AddConsole())

    // register logging for ReflectionTools package
    .AddReflectionTools(isStaticDefault: true)

    // register all ModRPC services for a server.
    .AddModularRpcs(
        isServer: true,
        // optional
        (services, config, parsers, parserFactories) =>
        {
            // set serialization config properties
            config.StringEncoding = Encoding.ASCII;
            config.MaximumGlobalArraySize = 256;
            config.MaximumStringLength = 8192;
            config.MaximumArraySizes[typeof(byte)] = 16384;

            // or

            services.GetRequiredService<IConfiguration>()
                    .GetSection("ModularRPCs")
                    .Bind(config);

            // register type parsers
            //   note: clients need to have the same parsers as the server
            parsers[typeof(Point)]  = new PointParser();
            parsers[typeof(Size)]   = new SizeParser();
            parsers[typeof(Vector)] = new VectorParser();

            // register IArrayBinaryTypeParser parsers
            parsers.AddManySerializer<Point>(
                new PointParser.Many(config)
            );

            // register IBinaryParserFactory implementations
            parserFactories.Add(new DictionaryParserFactory(config));
        },
        // optional (singleton or scoped?)
        scoped: false
    )
    
    // adds a service that will be initialized as an RPC object
    .AddRpcService<IPostDispatchService, PostDispatchService>();

/// <summary>
/// Hosts the named pipes server using ModularRPCs.
/// </summary>
internal class ModularRpcsServer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private NamedPipeEndpoint? _endpoint;

    public ModularRpcsServer(IServiceProvider serviceProvider)
    {
        // if you'd prefer to not inject the service provider,
        // you can supply the services needed to NamedPipeEndpoint.CreateServerAsync instead

        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const string pipeName = "Company.Product.RPCs";

        _endpoint = NamedPipeEndpoint.AsServer(_serviceProvider, pipeName);
        await _endpoint.CreateServerAsync(cancellationToken);
        
        // server is ready to receive clients
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_endpoint != null)
        {
            // CloseServerAsync is the same as disposing
            await _endpoint.CloseServerAsync(cancellationToken);
            _endpoint = null;
        }
    }
}

```
#### Client
The following example registers the necessary services on a client-like process using extension methods and sets up the client to connect via a Named Pipe.
```cs

IServiceCollection collection = new ServiceCollection()
    .AddLogging(l => l.AddConsole())

    // register logging for ReflectionTools package
    .AddReflectionTools(isStaticDefault: true)

    // register all ModRPC services for a client.
    // for optional configuration, see server example
    //   note: clients need to have the same parsers as the server
    .AddModularRpcs(isServer: false)
    
    // adds a service that will be initialized as an RPC object
    .AddRpcService<PostEmailNotificationService>();

/// <summary>
/// Hosts a connection the server using Named Pipes.
/// </summary>
internal class ModularRpcsClient : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private NamedPipeEndpoint? _endpoint;
    private NamedPipeClientsideRemoteRpcConnection? _connection;

    public ModularRpcsClient(IServiceProvider serviceProvider)
    {
        // if you'd prefer to not inject the service provider,
        // you can supply the services needed to NamedPipeEndpoint.RequestConnectionAsync instead

        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const string pipeName = "Company.Product.RPCs";

        _endpoint = NamedPipeEndpoint.AsClient(_serviceProvider, pipeName);
        _connection = await _endpoint.RequestConnectionAsync(TimeSpan.FromSeconds(15d), cancellationToken); // 15s timeout
        
        // client is connected
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
            _connection = null;
        }
    }
}

```

### Communicate using a specific instance of a class
The following example shows a scenerio where a client in a game can define a RPC linked to a specific animal (using `IRpcObject<>`).

**When done with an `IRpcObject`, you must call the `Release` extension method on it.**<br>
(this is not required on Unity components)
```cs
[GenerateRpcSource]
public partial class Animal : IRpcObject<int>
{
    // part of IRpcObject
    public int Identifier { get; }

    public Animal(int instanceId)
    {
        Identifier = instanceId;
    }
    
    /// <summary>
    /// Sends a request to the server to interact with this animal.
    /// </summary>
    /// <returns>The result of the interaction</returns>
    [RpcSend(nameof(ReceiveJump)]
    public partial RpcTask<string> SendInteractRequestAsync(
        Vector3 interactPoint,              // value parameter
        CancellationToken token = default   // injected parameter
    );

    [RpcReceive]
    private async Task<string> HandleInteractRequest(
        Vector3 interactPoint,              // value parameter (mapped by order)
        IModularRpcLocalConnection client,  // injected parameter
        CancellationToken token             // injected parameter
    )
    {
        // check to make sure the client should be allowed to interact (proximity check perhaps)
        if (!CanClientInteract(client, interactPoint))
            throw new InvalidOperationException();

        // perform the interaction (omitted for brevity)
        InteractionResult r = await InteractAsync(interactPoint, token);
        return r.Message;
    }
}
```

### Communicate between two different services
The following example shows one service in one process broadcasting an event to all clients in the `IRpcConnectionLifetime`.
```cs
namespace Company.Services;

[GenerateRpcSource]
public partial class PostDispatchService : IPostDispatchService
{
    public PostDispatchService(/* ... */)
    {
        // ...
    }

    public async Task HandlePostReceived(Post post)
    {
        BroadcastNewPost(post.Id);
    }
    
    [RpcSend]
    private void BroadcastNewPost(uint postPk);

    // or (allows you to know how many clients your broadcast was sent to)

    [RpcSend, RpcFireAndForget]
    private RpcBroadcastTask BroadcastNewPost(uint postPk);
}

[GenerateRpcSource]
// set default target type assembly-qualified name, can also be supplied in RpcReceive
// can use typeof if the assembly is loaded.
[RpcDefaultTargetType("Company.Services.PostDispatchService, CompanyAssembly")]
public partial class PostEmailNotificationService
{
    // listen for PostDispatchService.BroadcastNewPost to be sent.
    [RpcReceive("BroadcastNewPost")]
    private async Task ReceiveNewPost(uint postPk)
    {
        // query post info from DB and send email
    }
}
```

### Custom serializable types
The following example defines a custom data type that implements `IRpcSerializable`, which allows a type to define their own serialization callbacks.

Defining a type this way automatically adds the binary parser to the serializer and also adds support for collections and nullable values of this type.

Reference types must define a public parameterless constructor if another constructor is present.
Value types always start as their default value (zero'd) and will not call any constructors.
```cs
[RpcSerializable(
    minimumSize: sizeof(int) + sizeof(char) + SerializationHelper.MinimumStringSize,
    
    // isFixedSize indicates whether or not all instances of this type will be the exact same size.
    //  this allows for significant performance boosts for fixed types
    isFixedSize: false
)]
public struct CustomDataType : IRpcSerializable
{
    public int Int32;
    public string String;
    public char Character;

    // calculates the size of this object.
    // must return the exact size that will be written to in Write
    public int GetSize(IRpcSerializer serializer)
    {
        return sizeof(int) + sizeof(char) + serializer.GetSize(String);
    }

    // writes this object's data to the binary buffer
    // returns the number of bytes written for error-checking purposes.
    public int Write(Span<byte> writeTo, IRpcSerializer serializer)
    {
        int w = sizeof(int) + sizeof(char);
        Unsafe.WriteUnaligned(ref writeTo[0], Int32);
        Unsafe.WriteUnaligned(ref writeTo[4], Character);
        w += serializer.WriteObject(String, writeTo.Slice(6));
        return w;
    }
    
    // reads this object's data from the binary buffer
    // returns the number of bytes read for error-checking purposes.
    public int Read(Span<byte> readFrom, IRpcSerializer serializer)
    {
        int r = sizeof(int) + sizeof(char);
        Int32 = Unsafe.ReadUnaligned<int>(ref readFrom[0]);
        Character = Unsafe.ReadUnaligned<char>(ref readFrom[4]);
        String = serializer.ReadObject<string>(readFrom.Slice(6), out int bytesRead);
        r += bytesRead;
        return r;
    }
}
```

### 'Raw' binary
The following example shows an advanced case where it may be beneficial to send raw binary data without the overhead of serialization.

See [documentation for `Raw`](https://github.com/DanielWillett/ModularRPCs/blob/master/ModularRPCs/Annotations/RpcTargetAttribute.cs#L111) for a list of supported byte collection types.

```cs
public async Task SendSomeBinary()
{
    int ovhSize = ProxyGenerator.Instance.CalculateOverheadSize(SendBinary, out int idStartIndex);
    int size = 32 + ovhSize;
    byte[] buffer = new byte[size];
    
    // Uncomment if IRpcObject, WriteIdentifier is an extension method
    // this.WriteIdentifier(buffer + idStartIndex);
    
    for (int i = 0; i < 32; i++)
        buffer[i + ovhSize] = (byte)i;
    
    // the array isn't reused, so canTakeOwnership is true
    // if it was stack-allocated or part of a static buffer, this should be false
    await SendBinary(buffer, size, true);
}

/// <param name="data">The binary data to send.</param>
/// <param name="size">Number of bytes to read (optional).</param>
/// <param name="canTakeOwnership">Whether or not the data can be accessed if there's a context change (like if a method is awaited).</param>
[RpcSend(nameof(ReceiveBinary), Raw = true]
private partial async RpcTask SendBinary(byte[] data, int size, bool canTakeOwnership);

// params mean the same thing as above.
// canTakeOwnership may vary with different transport implementations
// if canTakeOwnership is false, data must be copied to a new buffer BEFORE awaiting
[RpcReceive(Raw = true)]
private void ReceiveBinary(byte[] data, bool canTakeOwnership)
{
    if (!canTakeOwnership)
    {
        byte[] newArray = new byte[data.Length];
        Buffer.BlockCopy(data, 0, newArray, 0, data.Length);
        data = newArray;
    }
    
    await Task.Delay(5000);

    for (int i = 0; i < 32; i++)
        Console.WriteLine(data[i]);
}
```

### Unity Components
The following example shows how to initialize ModularRPCs without a service provider and create a UnityEngine component that can send/receive RPC messages.

The code generater has to create an OnDestory method, so Unity components can implement `IExplicitFinalizerRpcObject` if they also need to run code when the component is destroyed. This is only for source-generated types.

Dynamically generated types must make `Start` and `OnDestroy` virtual if they're defined.

```cs
/* server-side initialization without a service provider */

// optionally add logging
void LogCallback(Type src, LogSeverity severity, Exception? exception, string? message)
{
    Debug.Log($"[{src.Name}][{severity}] {message}{(exception == null ? string.Empty : Environment.NewLine + exception)}";
}

ProxyGenerator.Instance.SetLogger(LogCallback);

ServerRpcConnectionLifetime lifetime = new ServerRpcConnectionLifetime();
// or ClientRpcConnectionLifetime lifetime = new ClientRpcConnectionLifetime();

DefaultSerializer serializer = new DefaultSerializer(/* optional config */);
RpcRouter router = new RpcRouter(serializer, lifetime, ProxyGenerator.Instance);

// optionally add logging
serializer.SetLogger(LogCallback);
router.SetLogger(LogCallback);


/* object creation */
int instanceId = _nextInstanceId++;

GameObject gameObject = new GameObject("Animal");
Animal animal = gameObject.AddRpcComponent<Animal>(router);
```

# Legal
ModularRPCs is licensed under the **GNU Lesser General Public License v3.0 or later** (`LGPL-3.0-or-later`).