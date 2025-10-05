using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Collections.Generic;
using DanielWillett.ModularRpcs.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace ModularRPCs.Test
{
    internal class TestServices : IServiceProvider
    {
        public IRpcSerializer Serializer { get; private set; }
        public IRpcRouter Router { get; private set; }
        public IRpcConnectionLifetime Lifetime { get; private set; }

        public static TestServices ForClient
        {
            get
            {
                IRpcSerializer serializer = new DefaultSerializer();
                IRpcConnectionLifetime connectionLifetime = new ClientRpcConnectionLifetime();
                TestServices t = new TestServices
                {
                    Serializer = serializer,
                    Lifetime = connectionLifetime
                };
                t.Router = new DependencyInjectionRpcRouter(t);
                return t;
            }
        }

        public static TestServices ForServer
        {
            get
            {
                IRpcSerializer serializer = new DefaultSerializer();
                IRpcConnectionLifetime connectionLifetime = new ServerRpcConnectionLifetime();
                TestServices t = new TestServices
                {
                    Serializer = serializer,
                    Lifetime = connectionLifetime
                };
                t.Router = new DependencyInjectionRpcRouter(t);
                return t;
            }
        }

        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<Type, object> _others = new Dictionary<Type, object>();

        private TestServices()
        {
            _loggerFactory = new LoggerFactory(new ILoggerProvider[] { new ConsoleLoggerProvider(new ConsoleLoggerOptionsMonitor()) });
        }

        public TestServices With<T>(T service) where T : class
        {
            _others[typeof(T)] = service;
            return this;
        }

        public TestServices WithProxy<T>() where T : class
        {
            _others[typeof(T)] = ProxyGenerator.Instance.CreateProxy<T>(Router);
            return this;
        }

        public TestServices WithProxy<T>(out T proxy) where T : class
        {
            _others[typeof(T)] = proxy = ProxyGenerator.Instance.CreateProxy<T>(Router);
            return this;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
                return this;

            if (serviceType == typeof(IRpcSerializer))
                return Serializer;

            if (serviceType == typeof(IRpcRouter))
                return Router;
        
            if (serviceType == typeof(IRpcConnectionLifetime))
                return Lifetime;

            if (serviceType == typeof(ProxyGenerator))
                return ProxyGenerator.Instance;

            if (serviceType == typeof(ILoggerFactory))
                return _loggerFactory;

            if (serviceType.IsConstructedGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
            {
                Type logger = serviceType.GetGenericArguments()[0];
                return _loggerFactory.CreateLogger(logger);
            }

            _others.TryGetValue(serviceType, out object value);
            return value;
        }

        private class ConsoleLoggerOptionsMonitor : IOptionsMonitor<ConsoleLoggerOptions>, IDisposable
        {
            private readonly ConsoleLoggerOptions _options = new ConsoleLoggerOptions
            {
                DisableColors = false,
                IncludeScopes = true,
                LogToStandardErrorThreshold = LogLevel.None,
                TimestampFormat = "s"
            };

            public ConsoleLoggerOptions Get(string name) => _options;
            public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener) => this;
            public ConsoleLoggerOptions CurrentValue => _options;
            public void Dispose() { }
        }
    }
}
