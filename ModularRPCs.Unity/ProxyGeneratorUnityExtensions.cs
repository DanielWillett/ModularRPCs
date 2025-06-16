extern alias Unity;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using Unity::UnityEngine;
using Object = Unity::UnityEngine.Object;

namespace DanielWillett.ModularRpcs;

/// <summary>
/// Extensions that allow creating RPC proxies as Unity components.
/// </summary>
public static class ProxyGeneratorUnityExtensions
{
    /// <summary>
    /// Create an instance of the RPC proxy of <typeparamref name="TComponentType"/> by adding it as a <see cref="Component"/> to a <paramref name="parentObject"/>.
    /// </summary>
    public static TComponentType CreateProxyComponent<TComponentType>(this ProxyGenerator proxyGenerator, GameObject parentObject, IRpcRouter router) where TComponentType : Component
        => (TComponentType)CreateProxyComponent(proxyGenerator, parentObject, typeof(TComponentType), router);

    /// <summary>
    /// Create an instance of the RPC proxy of <paramref name="componentType"/> by adding it as a <see cref="Component"/> to a <paramref name="parentObject"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="componentType"/> is not a subclass of <see cref="Component"/>.</exception>
    public static Component CreateProxyComponent(this ProxyGenerator proxyGenerator, GameObject parentObject, Type componentType, IRpcRouter router)
    {
        ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(componentType);
        if (typeInfo is { IsGenerated: false, SetUnityRouterField: null } || !componentType.IsSubclassOf(typeof(Component)))
        {
            throw new ArgumentException(
                string.Format(
                    Properties.Exceptions.ComponentTypeNotUnityType,
                    Accessor.ExceptionFormatter.Format(componentType)
                ),
                nameof(componentType)
            );
        }

        Component comp = parentObject.AddComponent(typeInfo.Type);
        if (typeInfo.IsGenerated)
        {
            IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)comp;
            genType.SetupGeneratedProxyInfo(new GeneratedProxyTypeInfo(router, proxyGenerator));
        }
        else
        {
            typeInfo.SetUnityRouterField!(comp, router);
        }
        return comp;
    }

    /// <summary>
    /// Create an instance of the RPC proxy of <typeparamref name="TComponentType"/> by adding it as a <see cref="Component"/> to a <paramref name="gameObject"/>.
    /// </summary>
    public static TComponentType AddRpcComponent<TComponentType>(this GameObject gameObject, IRpcRouter router) where TComponentType : Component
    {
        return (TComponentType)ProxyGenerator.Instance.CreateProxyComponent(gameObject, typeof(TComponentType), router);
    }

    /// <summary>
    /// Create an instance of the RPC proxy of <paramref name="componentType"/> by adding it as a <see cref="Component"/> to a <paramref name="gameObject"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="componentType"/> is not a subclass of <see cref="Component"/>.</exception>
    public static Component AddRpcComponent(this GameObject gameObject, Type componentType, IRpcRouter router)
    {
        return ProxyGenerator.Instance.CreateProxyComponent(gameObject, componentType, router);
    }

    /// <summary>
    /// Create an instance of an RPC proxy of another <typeparamref name="TObjectType"/> by calling <see cref="Object.Instantiate(Object, Vector3, Quaternion, Transform)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static TObjectType CreateInstantiatedProxy<TObjectType>(this ProxyGenerator proxyGenerator, TObjectType objectToInstantiate, Vector3 position, Quaternion rotation, Transform parent, IRpcRouter router) where TObjectType : Object
        => (TObjectType)CreateInstantiatedProxy(proxyGenerator, (Object)objectToInstantiate, position, rotation, parent, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <see cref="Object"/> by calling <see cref="Object.Instantiate(Object, Vector3, Quaternion, Transform)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static Object CreateInstantiatedProxy(this ProxyGenerator proxyGenerator, Object objectToInstantiate, Vector3 position, Quaternion rotation, Transform parent, IRpcRouter router)
    {
        if (objectToInstantiate is GameObject gameObject)
        {
            return CreateInstantiatedProxyGameObject(proxyGenerator, gameObject, position, rotation, parent, router);
        }

        Type type = objectToInstantiate.GetType();
        ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(type);
        if (typeInfo is { IsGenerated: false, SetUnityRouterField: null })
        {
            throw new ArgumentException(
                string.Format(
                    Properties.Exceptions.InstantiatedTypeNotUnityType,
                    Accessor.ExceptionFormatter.Format(type)
                ),
                nameof(objectToInstantiate)
            );
        }

        Object newObj = Object.Instantiate(objectToInstantiate, position, rotation, parent);
        if (typeInfo.IsGenerated)
        {
            IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)newObj;
            proxyGenerator.SetupGeneratedProxyInfo(genType, new GeneratedProxyTypeInfo(router, proxyGenerator));
        }
        else
        {
            typeInfo.SetUnityRouterField!(newObj, router);
        }
        return newObj;
    }

    /// <summary>
    /// Create an instance of an RPC proxy of another <typeparamref name="TObjectType"/> by calling <see cref="Object.Instantiate(Object, Transform, bool)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static TObjectType CreateInstantiatedProxy<TObjectType>(this ProxyGenerator proxyGenerator, TObjectType objectToInstantiate, Transform transform, bool instantiateInWorldSpace, IRpcRouter router) where TObjectType : Object
        => (TObjectType)CreateInstantiatedProxy(proxyGenerator, (Object)objectToInstantiate, transform, instantiateInWorldSpace, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <see cref="Object"/> by calling <see cref="Object.Instantiate(Object, Transform, bool)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static Object CreateInstantiatedProxy(this ProxyGenerator proxyGenerator, Object objectToInstantiate, Transform transform, bool instantiateInWorldSpace, IRpcRouter router)
    {
        if (objectToInstantiate is GameObject gameObject)
        {
            return CreateInstantiatedProxyGameObject(proxyGenerator, gameObject, transform, instantiateInWorldSpace, router);
        }

        Type type = objectToInstantiate.GetType();
        ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(type);
        if (typeInfo is { IsGenerated: false, SetUnityRouterField: null })
        {
            throw new ArgumentException(
                string.Format(
                    Properties.Exceptions.InstantiatedTypeNotUnityType,
                    Accessor.ExceptionFormatter.Format(type)
                ),
                nameof(objectToInstantiate)
            );
        }

        Object newObj = Object.Instantiate(objectToInstantiate, transform, instantiateInWorldSpace);
        if (typeInfo.IsGenerated)
        {
            IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)newObj;
            genType.SetupGeneratedProxyInfo(new GeneratedProxyTypeInfo(router, proxyGenerator));
        }
        else
        {
            typeInfo.SetUnityRouterField!(newObj, router);
        }
        return newObj;
    }

    /// <summary>
    /// Create an instance of an RPC proxy of another <typeparamref name="TObjectType"/> by calling <see cref="Object.Instantiate(Object, Transform)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static TObjectType CreateInstantiatedProxy<TObjectType>(this ProxyGenerator proxyGenerator, TObjectType objectToInstantiate, Transform transform, IRpcRouter router) where TObjectType : Object
        => (TObjectType)CreateInstantiatedProxy(proxyGenerator, (Object)objectToInstantiate, transform, false, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <see cref="Object"/> by calling <see cref="Object.Instantiate(Object, Transform)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static Object CreateInstantiatedProxy(this ProxyGenerator proxyGenerator, Object objectToInstantiate, Transform transform, IRpcRouter router)
        => CreateInstantiatedProxy(proxyGenerator, objectToInstantiate, transform, false, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <typeparamref name="TObjectType"/> by calling <see cref="Object.Instantiate(Object)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static TObjectType CreateInstantiatedProxy<TObjectType>(this ProxyGenerator proxyGenerator, TObjectType objectToInstantiate, IRpcRouter router) where TObjectType : Object
        => (TObjectType)CreateInstantiatedProxy(proxyGenerator, (Object)objectToInstantiate, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <see cref="Object"/> by calling <see cref="Object.Instantiate(Object)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static Object CreateInstantiatedProxy(this ProxyGenerator proxyGenerator, Object objectToInstantiate, IRpcRouter router)
    {
        if (objectToInstantiate is GameObject gameObject)
        {
            return CreateInstantiatedProxyGameObject(proxyGenerator, gameObject, router);
        }

        Type type = objectToInstantiate.GetType();
        ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(type);
        if (typeInfo is { IsGenerated: false, SetUnityRouterField: null })
        {
            throw new ArgumentException(
                string.Format(
                    Properties.Exceptions.InstantiatedTypeNotUnityType,
                    Accessor.ExceptionFormatter.Format(type)
                ),
                nameof(objectToInstantiate)
            );
        }

        Object newObj = Object.Instantiate(objectToInstantiate);
        if (typeInfo.IsGenerated)
        {
            IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)newObj;
            genType.SetupGeneratedProxyInfo(new GeneratedProxyTypeInfo(router, proxyGenerator));
        }
        else
        {
            typeInfo.SetUnityRouterField!(newObj, router);
        }
        return newObj;
    }

    /// <summary>
    /// Create an instance of an RPC proxy of another <typeparamref name="TObjectType"/> by calling <see cref="Object.Instantiate(Object, Vector3, Quaternion)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    public static TObjectType CreateInstantiatedProxy<TObjectType>(this ProxyGenerator proxyGenerator, TObjectType objectToInstantiate, Vector3 position, Quaternion rotation, IRpcRouter router) where TObjectType : Object
        => (TObjectType)CreateInstantiatedProxy(proxyGenerator, (Object)objectToInstantiate, position, rotation, router);

    /// <summary>
    /// Create an instance of an RPC proxy of another <see cref="Object"/> by calling <see cref="Object.Instantiate(Object, Vector3, Quaternion)"/> on <paramref name="objectToInstantiate"/>.
    /// </summary>
    /// <remarks>Only components that were already RPC proxy components will be made into RPC proxy components in the new object.</remarks>
    public static Object CreateInstantiatedProxy(this ProxyGenerator proxyGenerator, Object objectToInstantiate, Vector3 position, Quaternion rotation, IRpcRouter router)
    {
        if (objectToInstantiate is GameObject gameObject)
        {
            return CreateInstantiatedProxyGameObject(proxyGenerator, gameObject, position, rotation, router);
        }

        Type type = objectToInstantiate.GetType();
        ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(type);
        if (typeInfo is { IsGenerated: false, SetUnityRouterField: null })
        {
            throw new ArgumentException(
                string.Format(
                    Properties.Exceptions.InstantiatedTypeNotUnityType,
                    Accessor.ExceptionFormatter.Format(type)
                ),
                nameof(objectToInstantiate)
            );
        }

        Object newObj = Object.Instantiate(objectToInstantiate, position, rotation);
        if (typeInfo.IsGenerated)
        {
            IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)newObj;
            genType.SetupGeneratedProxyInfo(new GeneratedProxyTypeInfo(router, proxyGenerator));
        }
        else
        {
            typeInfo.SetUnityRouterField!(newObj, router);
        }
        return newObj;
    }
    private static void InitInstantiatedGameObjectRecursive(ProxyGenerator proxyGenerator, GameObject objParent, IRpcRouter router, List<Component> workingComponentList)
    {
        objParent.GetComponents(workingComponentList);
        for (int i = 0; i < workingComponentList.Count; ++i)
        {
            Component component = workingComponentList[i];
            Type compType = component.GetType();

            if (!proxyGenerator.HasProxyType(compType))
                continue;

            ProxyGenerator.ProxyTypeInfo typeInfo = proxyGenerator.GetProxyTypeInfo(compType.BaseType!);
            if (typeInfo is { IsGenerated: false, SetUnityRouterField: null })
                continue;

            if (typeInfo.IsGenerated)
            {
                IRpcGeneratedProxyType genType = (IRpcGeneratedProxyType)component;
                genType.SetupGeneratedProxyInfo(new GeneratedProxyTypeInfo(router, proxyGenerator));
            }
            else
            {
                typeInfo.SetUnityRouterField!(component, router);
            }
        }

        workingComponentList.Clear();
        foreach (Transform child in objParent.transform)
        {
            InitInstantiatedGameObjectRecursive(proxyGenerator, child.gameObject, router, workingComponentList);
        }
    }
    private static GameObject CreateInstantiatedProxyGameObject(ProxyGenerator proxyGenerator, GameObject objectToInstantiate, Vector3 position, Quaternion rotation, Transform parent, IRpcRouter router)
    {
        GameObject newObj = (GameObject)Object.Instantiate((Object)objectToInstantiate, position, rotation, parent);

        List<Component> workingComponentList = new List<Component>();
        InitInstantiatedGameObjectRecursive(proxyGenerator, newObj, router, workingComponentList);
        return newObj;
    }
    private static GameObject CreateInstantiatedProxyGameObject(ProxyGenerator proxyGenerator, GameObject objectToInstantiate, Vector3 position, Quaternion rotation, IRpcRouter router)
    {
        GameObject newObj = (GameObject)Object.Instantiate((Object)objectToInstantiate, position, rotation);

        List<Component> workingComponentList = new List<Component>();
        InitInstantiatedGameObjectRecursive(proxyGenerator, newObj, router, workingComponentList);
        return newObj;
    }
    private static GameObject CreateInstantiatedProxyGameObject(ProxyGenerator proxyGenerator, GameObject objectToInstantiate, Transform transform, bool instantiateInWorldSpace, IRpcRouter router)
    {
        GameObject newObj = (GameObject)Object.Instantiate((Object)objectToInstantiate, transform, instantiateInWorldSpace);

        List<Component> workingComponentList = new List<Component>();
        InitInstantiatedGameObjectRecursive(proxyGenerator, newObj, router, workingComponentList);
        return newObj;
    }
    private static GameObject CreateInstantiatedProxyGameObject(ProxyGenerator proxyGenerator, GameObject objectToInstantiate, IRpcRouter router)
    {
        GameObject newObj = (GameObject)Object.Instantiate((Object)objectToInstantiate);

        List<Component> workingComponentList = new List<Component>();
        InitInstantiatedGameObjectRecursive(proxyGenerator, newObj, router, workingComponentList);
        return newObj;
    }
}