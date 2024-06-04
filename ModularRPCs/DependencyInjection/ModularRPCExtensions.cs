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
    /// Adds the singleton instance at <see cref="ProxyGenerator.Instance"/> to the <paramref name="serviceCollection"/>.
    /// </summary>
    /// <param name="isServer">Will this side be acting as the server or client? Affects which type of <see cref="IRpcConnectionLifetime"/> is added.</param>
    public static IServiceCollection AddModularRpcs(this IServiceCollection serviceCollection, bool isServer,
        Action<IServiceProvider, SerializationConfiguration, IDictionary<Type, IBinaryTypeParser>, IList<IBinaryParserFactory>>? configureSerialization = null,
        IEnumerable<Assembly>? searchedAssemblies = null)
    {
        // proxy generator
        AddProxyGenerator(serviceCollection);

        // serializer
        if (serviceCollection.All(d => !typeof(IRpcSerializer).IsAssignableFrom(d.ServiceType)
                                       && d.ImplementationType != typeof(IRpcSerializer)))
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IRpcSerializer), serviceProvider =>
            {
                if (configureSerialization != null)
                {
                    return new DefaultSerializer(
                        (conf, parsers, factories) => configureSerialization(serviceProvider, conf, parsers, factories)
                    );
                }

                return new DefaultSerializer();
            }, ServiceLifetime.Singleton));
        }

        // connection lifetime
        if (serviceCollection.All(d => !typeof(IRpcConnectionLifetime).IsAssignableFrom(d.ServiceType)
                                       && d.ImplementationType != typeof(IRpcConnectionLifetime)))
        {
            serviceCollection.Add(
                new ServiceDescriptor(typeof(IRpcConnectionLifetime),
                    isServer
                    ? serviceProvider =>
                    {
                        ServerRpcConnectionLifetime lifetime = new ServerRpcConnectionLifetime();
                        serviceProvider.ApplyLoggerTo(lifetime);
                        return lifetime;
                    }
                    : serviceProvider =>
                    {
                        ClientRpcConnectionLifetime lifetime = new ClientRpcConnectionLifetime();
                        serviceProvider.ApplyLoggerTo(lifetime);
                        return lifetime;
                    }
                    , ServiceLifetime.Singleton
                )
            );
        }

        if (serviceCollection.All(d => d.ServiceType != typeof(DefaultRpcRouter)
                                       && d.ServiceType != typeof(DependencyInjectionRpcRouter)
                                       && d.ImplementationType != typeof(IRpcRouter)))
        {
            serviceCollection.Add(new ServiceDescriptor(typeof(IRpcRouter), serviceProvider =>
            {
                DependencyInjectionRpcRouter router =
                    searchedAssemblies != null
                    ? new DependencyInjectionRpcRouter(serviceProvider, searchedAssemblies)
                    : new DependencyInjectionRpcRouter(serviceProvider);

                serviceProvider.ApplyLoggerTo(router);
                return router;
            }, ServiceLifetime.Singleton));
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
        AddProxyGenerator(serviceCollection);

        Type type = ProxyGenerator.Instance.GetProxyType<TService>();

        serviceCollection.Add(new ServiceDescriptor(typeof(TService), type, lifetime));
        serviceCollection.Add(new ServiceDescriptor(type, static serviceProvider => serviceProvider.GetRequiredService<TService>(), lifetime));
        return serviceCollection;
    }

    /// <summary>
    /// Adds a class with RPC methods decorated with the <see cref="RpcSendAttribute"/> to an <see cref="IServiceCollection"/> with the given <paramref name="lifetime"/>.
    /// </summary>
    /// <remarks>This method adds a service of type <typeparamref name="TService"/> to the collection with an implementation type of the proxy type of <typeparamref name="TImplementation"/> generated by <see cref="ProxyGenerator"/>.</remarks>
    public static IServiceCollection AddRpcService<TService, TImplementation>(this IServiceCollection serviceCollection, ServiceLifetime lifetime) where TService : class where TImplementation : class
    {
        AddProxyGenerator(serviceCollection);

        Type type = ProxyGenerator.Instance.GetProxyType<TImplementation>();

        serviceCollection.Add(new ServiceDescriptor(typeof(TService), type, lifetime));
        serviceCollection.Add(new ServiceDescriptor(type, static serviceProvider => serviceProvider.GetRequiredService<TService>(), lifetime));
        return serviceCollection;
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