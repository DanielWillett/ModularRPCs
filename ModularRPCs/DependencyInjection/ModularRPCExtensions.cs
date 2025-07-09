using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.DependencyInjection;

/// <summary>
/// Extensions for registering ModularRPC services with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ModularRpcExtensions
{
    private static void AddProxyGenerator(IServiceCollection serviceCollection)
    {
        if (serviceCollection.Any(d => d.ServiceType == typeof(ProxyGenerator)))
            return;

        serviceCollection.Add(new ServiceDescriptor(typeof(ProxyGenerator),
            serviceProvider =>
            {
                ProxyGenerator proxyGenerator = ProxyGenerator.Instance;
                serviceProvider.ApplyLoggerTo(proxyGenerator);
                return ProxyGenerator.Instance;
            },
            ServiceLifetime.Singleton)
        );
    }

    /// <summary>
    /// Adds the singleton instance at <see cref="ProxyGenerator.Instance"/> to the <paramref name="serviceCollection"/>, along with <see cref="IRpcRouter"/>, <see cref="IRpcSerializer"/>, and <see cref="IRpcConnectionLifetime"/>.
    /// </summary>
    /// <param name="serviceCollection">The collection to add services to.</param>
    /// <param name="isServer">Will this side be acting as the server or client? Affects which type of <see cref="IRpcConnectionLifetime"/> is added.</param>
    /// <param name="searchedAssemblies">Assemblies to be searched for <see cref="RpcReceiveAttribute"/>'s that are set up as broadcast listeners (have a specific send method specified). If this is left null, it will be defaulted to the calling assembly along with any direct references it has.</param>
    /// <param name="configureSerialization">Configure how <see cref="IRpcSerializer"/> behaves, including adding custom parsers and parser factories.</param>
    /// <param name="scoped">Determines whether or not <see cref="IRpcSerializer"/>, <see cref="IRpcConnectionLifetime"/>, and <see cref="IRpcRouter"/> are Scoped or Singletons.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddModularRpcs(this IServiceCollection serviceCollection, bool isServer,
        Action<IServiceProvider, SerializationConfiguration, IDictionary<Type, IBinaryTypeParser>, IList<IBinaryParserFactory>>? configureSerialization = null,
        IEnumerable<Assembly>? searchedAssemblies = null, bool scoped = false)
    {
        // proxy generator
        AddProxyGenerator(serviceCollection);

        // serializer
        if (serviceCollection.All(d => d.ServiceType != typeof(IRpcSerializer)))
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IRpcSerializer), serviceProvider =>
            {
                if (((IRefSafeLoggable)ProxyGenerator.Instance).LoggerType == LoggerType.None)
                    serviceProvider.ApplyLoggerTo(ProxyGenerator.Instance);

                if (configureSerialization != null)
                {
                    return new DefaultSerializer(
                        (conf, parsers, factories) => configureSerialization(serviceProvider, conf, parsers, factories)
                    );
                }

                return new DefaultSerializer();
            }, scoped ? ServiceLifetime.Scoped : ServiceLifetime.Singleton));
        }

        // connection lifetime
        if (serviceCollection.All(d => d.ServiceType != typeof(IRpcConnectionLifetime)))
        {
            serviceCollection.Add(
                new ServiceDescriptor(typeof(IRpcConnectionLifetime),
                    isServer
                    ? serviceProvider =>
                    {
                        if (((IRefSafeLoggable)ProxyGenerator.Instance).LoggerType == LoggerType.None)
                            serviceProvider.ApplyLoggerTo(ProxyGenerator.Instance);

                        ServerRpcConnectionLifetime lifetime = new ServerRpcConnectionLifetime();
                        serviceProvider.ApplyLoggerTo(lifetime);
                        return lifetime;
                    }
                    : serviceProvider =>
                    {
                        if (((IRefSafeLoggable)ProxyGenerator.Instance).LoggerType == LoggerType.None)
                            serviceProvider.ApplyLoggerTo(ProxyGenerator.Instance);

                        ClientRpcConnectionLifetime lifetime = new ClientRpcConnectionLifetime();
                        serviceProvider.ApplyLoggerTo(lifetime);
                        return lifetime;
                    }
                    , scoped ? ServiceLifetime.Scoped : ServiceLifetime.Singleton
                )
            );
        }

        Assembly? asm = searchedAssemblies != null ? null : Assembly.GetCallingAssembly();

        if (serviceCollection.All(d => d.ServiceType != typeof(IRpcRouter)))
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IRpcRouter), serviceProvider =>
            {
                DependencyInjectionRpcRouter router =
                    searchedAssemblies != null
                    ? new DependencyInjectionRpcRouter(serviceProvider, searchedAssemblies)
                    : new DependencyInjectionRpcRouter(serviceProvider, asm!);

                if (((IRefSafeLoggable)ProxyGenerator.Instance).LoggerType == LoggerType.None)
                    serviceProvider.ApplyLoggerTo(ProxyGenerator.Instance);

                serviceProvider.ApplyLoggerTo(router);
                return router;
            }, scoped ? ServiceLifetime.Scoped : ServiceLifetime.Singleton));
        }

        return serviceCollection;
    }

    /// <summary>
    /// Uses the registered <see cref="ILoggerFactory"/> to create and assign an <see cref="ILogger{ProxyGenerator}"/> for the <see cref="ProxyGenerator"/> singleton.
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no service of type <see cref="ILoggerFactory"/>.</exception>
    public static IServiceProvider ApplyLoggerTo(this IServiceProvider serviceProvider, IRefSafeLoggable loggable)
    {
        ILoggerFactory logFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = logFactory.CreateLogger(loggable.GetType());

        loggable.SetLogger(logger);

        return serviceProvider;
    }

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with the given <paramref name="lifetime"/>.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcService<TService>(this IServiceCollection serviceCollection, ServiceLifetime lifetime) where TService : class
    {
        return serviceCollection.AddRpcService<TService, TService>(lifetime);
    }

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with the given <paramref name="lifetime"/>.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type of <typeparamref name="TImplementation"/> generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcService<TService, TImplementation>(this IServiceCollection serviceCollection, ServiceLifetime lifetime) where TService : class where TImplementation : class
    {
        AddProxyGenerator(serviceCollection);

        ProxyGenerator.ProxyTypeInfo typeInfo = ProxyGenerator.Instance.GetProxyTypeInfo(typeof(TImplementation));

        if (typeInfo.IsGenerated)
        {
            if (serviceCollection.All(x => x.ServiceType != typeof(GeneratedRpcServiceFactory<TImplementation>)))
            {
                serviceCollection.Add(
                    new ServiceDescriptor(
                        typeof(GeneratedRpcServiceFactory<TImplementation>),
                        typeof(GeneratedRpcServiceFactory<TImplementation>),
                        ServiceLifetime.Singleton
                    )
                );
            }

            serviceCollection.Add(new ServiceDescriptor(typeof(TService),
                serviceProvider => serviceProvider
                    .GetRequiredService<GeneratedRpcServiceFactory<TImplementation>>()
                    .Create(serviceProvider), lifetime)
            );
        }
        else
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(TService), typeInfo.Type, lifetime));
            if (typeInfo.Type != typeof(TService))
                serviceCollection.Add(new ServiceDescriptor(typeInfo.Type, static serviceProvider => serviceProvider.GetRequiredService<TService>(), lifetime));
        }

        return serviceCollection;
    }

    private class GeneratedRpcServiceFactory<TRpcService>
    {
        private readonly ObjectFactory _factory = ActivatorUtilities.CreateFactory(typeof(TRpcService), Type.EmptyTypes);

        public object Create(IServiceProvider serviceProvider)
        {
            object instance = _factory(serviceProvider, Array.Empty<object>());
            ((IRpcGeneratedProxyType)instance).__ModularRpcsGeneratedSetupGeneratedProxyInfo(
                new GeneratedProxyTypeInfo(serviceProvider.GetRequiredService<IRpcRouter>(), ProxyGenerator.Instance));

            return instance;
        }
    }

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="singleton"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcSingleton<TService>(this IServiceCollection serviceCollection) where TService : class
        => AddRpcService<TService>(serviceCollection, ServiceLifetime.Singleton);

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="singleton"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type of <typeparamref name="TImplementation"/> generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcSingleton<TService, TImplementation>(this IServiceCollection serviceCollection) where TService : class where TImplementation : class
        => AddRpcService<TService, TImplementation>(serviceCollection, ServiceLifetime.Singleton);

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="scoped"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcScoped<TService>(this IServiceCollection serviceCollection) where TService : class
        => AddRpcService<TService>(serviceCollection, ServiceLifetime.Scoped);

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="scoped"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type of <typeparamref name="TImplementation"/> generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcScoped<TService, TImplementation>(this IServiceCollection serviceCollection) where TService : class where TImplementation : class
        => AddRpcService<TService, TImplementation>(serviceCollection, ServiceLifetime.Scoped);

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="transient"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcTransient<TService>(this IServiceCollection serviceCollection) where TService : class
        => AddRpcService<TService>(serviceCollection, ServiceLifetime.Transient);

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with a <see langword="transient"/> lifetime.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type of <typeparamref name="TImplementation"/> generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcTransient<TService, TImplementation>(this IServiceCollection serviceCollection) where TService : class where TImplementation : class
        => AddRpcService<TService, TImplementation>(serviceCollection, ServiceLifetime.Transient);
}