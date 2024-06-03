using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class CommonReflectionCache
{
    /// <summary>
    /// <see cref="ProxyContext.DefaultSerializer"/>.
    /// </summary>
    internal static readonly FieldInfo ProxyContextSerializerField = typeof(ProxyContext).GetField(nameof(ProxyContext.DefaultSerializer), BindingFlags.Public | BindingFlags.Instance)
                                                                     ?? throw new UnexpectedMemberAccessException(new FieldDefinition(nameof(ProxyContext.DefaultSerializer))
                                                                         .DeclaredIn<ProxyContext>(isStatic: false)
                                                                         .WithFieldType<IRpcSerializer>()
                                                                     );

    /// <summary>
    /// <see cref="ProxyContext.Router"/>.
    /// </summary>
    internal static readonly FieldInfo ProxyContextRouterField = typeof(ProxyContext).GetField(nameof(ProxyContext.Router), BindingFlags.Public | BindingFlags.Instance)
                                                                 ?? throw new UnexpectedMemberAccessException(new FieldDefinition(nameof(ProxyContext.Router))
                                                                     .DeclaredIn<ProxyContext>(isStatic: false)
                                                                     .WithFieldType<IRpcRouter>()
                                                                 );

    /// <summary>
    /// <see cref="IRpcSerializer.CanFastReadPrimitives"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerCanFastReadPrimitives = typeof(IRpcSerializer).GetProperty(nameof(IRpcSerializer.CanFastReadPrimitives), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                  ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(IRpcSerializer.CanFastReadPrimitives))
                                                                      .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                      .WithPropertyType<bool>()
                                                                      .WithNoSetter()
                                                                  );

    /// <summary>
    /// <see cref="IRpcRouter.GetDefaultProxyContext"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterGetDefaultProxyContext = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.GetDefaultProxyContext), BindingFlags.Public | BindingFlags.Instance)
                                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.GetDefaultProxyContext))
                                                                              .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                              .WithParameter<Type>("proxyType")
                                                                              .WithParameter<ProxyContext>("context", ByRefTypeMode.Out)
                                                                              .ReturningVoid()
                                                                          );

    /// <summary>
    /// <see cref="string.Length"/>.
    /// </summary>
    internal static readonly MethodInfo GetStringLength = typeof(string).GetProperty(nameof(string.Length), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                          ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(string.Length))
                                                              .DeclaredIn<string>(isStatic: false)
                                                              .WithPropertyType<int>()
                                                              .WithNoSetter()
                                                          );

    /// <summary>
    /// <see cref="object.Equals(object)"/>.
    /// </summary>
    internal static readonly MethodInfo ObjectEqualsObject = typeof(object).GetMethod(nameof(Equals), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(object) ], null)
                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(Equals))
                                                                 .DeclaredIn(typeof(object), isStatic: false)
                                                                 .WithParameter<object?>("obj")
                                                                 .Returning<bool>()
                                                             );

    /// <summary>
    /// <see cref="WeakReference(object)"/>.
    /// </summary>
    internal static readonly ConstructorInfo WeakReferenceConstructor = typeof(WeakReference).GetConstructor([ typeof(object) ])
                                                                        ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(WeakReference))
                                                                            .WithParameter(typeof(object), "target")
                                                                        );

    /// <summary>
    /// <see cref="Interlocked.Exchange(ref int, int)"/>.
    /// </summary>
    internal static readonly MethodInfo InterlockedExchangeInt = typeof(Interlocked).GetMethod(nameof(Interlocked.Exchange), BindingFlags.Static | BindingFlags.Public, null, [ typeof(int).MakeByRefType(), typeof(int) ], null)
                                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(Interlocked.Exchange))
                                                                     .DeclaredIn(typeof(Interlocked), isStatic: true)
                                                                     .Returning<int>()
                                                                     .WithParameter(typeof(int), "location1", ByRefTypeMode.Ref)
                                                                     .WithParameter(typeof(int), "value")
                                                                 );

    /// <summary>
    /// <see cref="IRpcSingleConnectionObject.Connection"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSingleConnectionObjectConnection = typeof(IRpcSingleConnectionObject).GetProperty(nameof(IRpcSingleConnectionObject.Connection), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                              ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(IRpcSingleConnectionObject.Connection))
                                                                                  .DeclaredIn<IRpcSingleConnectionObject>(isStatic: false)
                                                                                  .WithPropertyType<IModularRpcRemoteConnection>()
                                                                                  .WithNoSetter()
                                                                              );

    /// <summary>
    /// <see cref="IRpcMultipleConnectionsObject.Connections"/>.
    /// </summary>
    internal static readonly MethodInfo RpcMultipleConnectionsObjectConnections = typeof(IRpcMultipleConnectionsObject).GetProperty(nameof(IRpcMultipleConnectionsObject.Connections), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                                  ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(IRpcMultipleConnectionsObject.Connections))
                                                                                      .DeclaredIn<IRpcMultipleConnectionsObject>(isStatic: false)
                                                                                      .WithPropertyType<IEnumerable<IModularRpcRemoteConnection>>()
                                                                                      .WithNoSetter()
                                                                                  );
    
    /// <summary>
    /// <see cref="IRpcRouter.InvokeRpc"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterInvokeRpc = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.InvokeRpc), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
                                                                 [ typeof(object), typeof(IRpcSerializer), typeof(RuntimeMethodHandle), typeof(byte*), typeof(int), typeof(uint), typeof(RpcCallMethodInfo).MakeByRefType() ], null)
                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.InvokeRpc))
                                                                 .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                 .WithParameter<object>("connections")
                                                                 .WithParameter<IRpcSerializer>("serializer")
                                                                 .WithParameter<RuntimeMethodHandle>("sourceMethodHandle")
                                                                 .WithParameter(typeof(byte*), "bytesSt")
                                                                 .WithParameter<int>("byteCt")
                                                                 .WithParameter<uint>("dataCt")
                                                                 .WithParameter<RpcCallMethodInfo>("callMethodInfo", ByRefTypeMode.Ref)
                                                                 .Returning<RpcTask>()
                                                             );

    /// <summary>
    /// <see cref="IRpcRouter.GetOverheadSize"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterGetOverheadSize = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.GetOverheadSize), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
                                                                       [ typeof(RuntimeMethodHandle), typeof(RpcCallMethodInfo).MakeByRefType() ], null)
                                                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.GetOverheadSize))
                                                                       .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                       .WithParameter<RuntimeMethodHandle>("sourceMethodHandle")
                                                                       .WithParameter<RpcCallMethodInfo>("callMethodInfo", ByRefTypeMode.Ref)
                                                                       .Returning<int>()
                                                                   );

    /// <summary>
    /// <see cref="IRpcSerializer.TryGetKnownTypeId"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerTryGetKnownTypeId = typeof(IRpcSerializer).GetMethod(nameof(IRpcSerializer.TryGetKnownTypeId), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
                                                                             [ typeof(Type), typeof(uint).MakeByRefType() ], null)
                                                                         ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.TryGetKnownTypeId))
                                                                             .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                             .WithParameter<Type>("knownType")
                                                                             .WithParameter<uint>("knownTypeId", ByRefTypeMode.Out)
                                                                             .Returning<int>()
                                                                         );

    /// <summary>
    /// <see cref="Type.GetMethod(string, BindingFlags)"/>.
    /// </summary>
    internal static readonly MethodInfo TypeGetMethodNameFlags = typeof(Type).GetMethod(nameof(Type.GetMethod), BindingFlags.Public | BindingFlags.Instance, null, [ typeof(string), typeof(BindingFlags) ], null)
                                                               ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Type.GetMethod))
                                                                   .DeclaredIn<Type>(isStatic: false)
                                                                   .WithParameter<string>("name")
                                                                   .WithParameter<BindingFlags>("bindingAttr")
                                                                   .Returning<MethodInfo>()
                                                               )}");

    /// <summary>
    /// <see cref="MethodBase.Invoke(object, object[])"/>.
    /// </summary>
    internal static readonly MethodInfo MethodBaseInvoke = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), BindingFlags.Public | BindingFlags.Instance, null, [ typeof(object), typeof(object[]) ], null)
                                                           ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(Type.GetMethod))
                                                               .DeclaredIn<MethodBase>(isStatic: false)
                                                               .WithParameter<object>("obj")
                                                               .WithParameter<object[]>("parameters")
                                                               .Returning<object>()
                                                           )}");

    /// <summary>
    /// <see cref="RpcOverhead.Rpc"/>.
    /// </summary>
    internal static readonly MethodInfo RpcOverheadGetRpc = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.Rpc), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverhead.Rpc))
                                                                .DeclaredIn<RpcOverhead>(isStatic: false)
                                                                .WithPropertyType<IRpcInvocationPoint>()
                                                                .WithNoSetter()
                                                            );

    /// <summary>
    /// <see cref="RpcOverhead.Flags"/>.
    /// </summary>
    internal static readonly MethodInfo RpcOverheadGetFlags = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.Flags), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                              ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverhead.Flags))
                                                                  .DeclaredIn<RpcOverhead>(isStatic: false)
                                                                  .WithPropertyType<RpcFlags>()
                                                                  .WithNoSetter()
                                                              );

    /// <summary>
    /// <see cref="RpcOverhead.ReceivingConnection"/>.
    /// </summary>
    internal static readonly MethodInfo RpcOverheadGetReceivingConnection = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.ReceivingConnection), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                            ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverhead.ReceivingConnection))
                                                                                .DeclaredIn<RpcOverhead>(isStatic: false)
                                                                                .WithPropertyType<IModularRpcLocalConnection>()
                                                                                .WithNoSetter()
                                                                            );

    /// <summary>
    /// <see cref="RpcOverhead.SendingConnection"/>.
    /// </summary>
    internal static readonly MethodInfo RpcOverheadGetSendingConnection = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.SendingConnection), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                          ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverhead.SendingConnection))
                                                                              .DeclaredIn<RpcOverhead>(isStatic: false)
                                                                              .WithPropertyType<IModularRpcRemoteConnection>()
                                                                              .WithNoSetter()
                                                                          );

    /// <summary>
    /// <see cref="string.Format(string, object, object)"/>.
    /// </summary>
    internal static readonly MethodInfo StringFormat2 = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(string), typeof(object), typeof(object) ], null)
                                                        ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(string.Format))
                                                            .WithParameter<string>("format")
                                                            .WithParameter<object>("arg0")
                                                            .WithParameter<object>("arg1")
                                                            .Returning<string>()
                                                        );

    /// <summary>
    /// <see cref="string.Format(string, object, object, object)"/>.
    /// </summary>
    internal static readonly MethodInfo StringFormat3 = typeof(string).GetMethod(nameof(string.Format), BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(string), typeof(object), typeof(object), typeof(object) ], null)
                                                        ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(string.Format))
                                                            .WithParameter<string>("format")
                                                            .WithParameter<object>("arg0")
                                                            .WithParameter<object>("arg1")
                                                            .WithParameter<object>("arg2")
                                                            .Returning<string>()
                                                        );

    /// <summary>
    /// <see cref="RpcInjectionException(string)"/>.
    /// </summary>
    internal static readonly ConstructorInfo RpcInjectionExceptionCtorMessage = typeof(RpcInjectionException).GetConstructor([ typeof(string) ])
                                                                                ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(RpcInjectionException))
                                                                                    .WithParameter<string>("message")
                                                                                );

    /// <summary>
    /// <see cref="RpcParseException(string)"/>.
    /// </summary>
    internal static readonly ConstructorInfo RpcParseExceptionCtorMessage = typeof(RpcParseException).GetConstructor([ typeof(string) ])
                                                                            ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(RpcParseException))
                                                                                .WithParameter<string>("message")
                                                                            );

    /// <summary>
    /// <see cref="RpcParseException.ErrorCode"/>.
    /// </summary>
    internal static readonly MethodInfo SetRpcParseExceptionErrorCode = typeof(RpcParseException).GetProperty(nameof(RpcParseException.ErrorCode), BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(true)
                                                                        ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcParseException.ErrorCode))
                                                                            .DeclaredIn<RpcParseException>(isStatic: false)
                                                                            .WithPropertyType<int>()
                                                                            .WithNoGetter()
                                                                        );

    /// <summary>
    /// <see cref="TimeSpan(long)"/>
    /// </summary>
    internal static readonly ConstructorInfo TimeSpanTicksCtor = typeof(TimeSpan).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(long) ], null)
                                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(TimeSpan))
                                                                     .WithParameter<long>("ticks")
                                                                 );

    /// <summary>
    /// <see cref="IRpcSerializer.GetSize(TypedReference)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerGetSizeByTRef;

    /// <summary>
    /// <see cref="IRpcSerializer.GetSize{T}(T)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerGetSizeByVal;

    /// <summary>
    /// <see cref="IRpcSerializer.GetSize{T}(in T?)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerGetSizeNullableByVal;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject(TypedReference,byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteObjectByTRefBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(T,byte*,uint)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerWriteObjectByValBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(in T?,byte*,uint)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerWriteNullableObjectByValBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject(TypedReference,Stream)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteObjectByTRefStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(T,Stream)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerWriteObjectByValStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(in T?,Stream)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerWriteNullableObjectByValStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadObject(TypedReference,byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadObjectByTRefBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadObject{T}(byte*,uint,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadObjectByValBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullable{T}(byte*,uint,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadNullableObjectByValBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullable{T}(TypedReference,byte*,uint,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadNullableObjectByTRefBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadObject(TypedReference,Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadObjectByTRefStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadObject{T}(Stream,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadObjectByValStream;


    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullable{T}(Stream,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadNullableObjectByValStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullable{T}(TypedReference,Stream,out int)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerReadNullableObjectByTRefStream;

    static CommonReflectionCache()
    {
        MethodInfo[] iRpcSerializerMethods = typeof(IRpcSerializer).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        RpcSerializerGetSizeByTRef = iRpcSerializerMethods
                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize)
                                                              && !x.IsGenericMethod
                                                              && x.GetParameters() is { Length: 1 } p
                                                              && p[0].ParameterType == typeof(TypedReference)
                                                        )
                                     ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                         .WithParameter(typeof(TypedReference), "value")
                                         .Returning<int>()
                                     );

        RpcSerializerGetSizeByVal = iRpcSerializerMethods
                                        .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize)
                                                             && x.IsGenericMethod
                                                             && x.GetParameters() is { Length: 1 } p
                                                             && p[0].ParameterType == x.GetGenericArguments()[0]
                                                       )
                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                                        .DeclaredIn<IRpcSerializer>(isStatic: false)
                                        .WithGenericParameterDefinition("T")
                                        .WithParameterUsingGeneric(0, "value")
                                        .Returning<int>()
                                    );

        RpcSerializerGetSizeNullableByVal = iRpcSerializerMethods
                                                .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSize)
                                                                     && x.IsGenericMethod
                                                                     && x.GetParameters() is { Length: 1 } p
                                                                     && p[0].ParameterType.IsByRef
                                                                     && p[0].ParameterType.GetElementType()! == typeof(Nullable<>).MakeGenericType(x.GetGenericArguments()[0])
                                                )
                                            ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetSize))
                                                .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                .WithGenericParameterDefinition("T")
                                                .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                .Returning<int>()
                                            );

        RpcSerializerWriteObjectByTRefBytes = iRpcSerializerMethods
                                                  .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                       && !x.IsGenericMethod
                                                                       && x.GetParameters() is { Length: 3 } p
                                                                       && p[0].ParameterType == typeof(TypedReference)
                                                                 )
                                              ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                  .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                  .WithParameter(typeof(TypedReference), "value")
                                                  .WithParameter(typeof(byte*), "bytes")
                                                  .WithParameter<uint>("maxSize")
                                                  .Returning<int>()
                                              );

        RpcSerializerWriteObjectByValBytes = iRpcSerializerMethods
                                                 .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                      && x.IsGenericMethod
                                                                      && x.GetParameters() is { Length: 3 } p
                                                                      && p[0].ParameterType == x.GetGenericArguments()[0]
                                                                )
                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                 .WithGenericParameterDefinition("T")
                                                 .WithParameterUsingGeneric(0, "value")
                                                 .WithParameter(typeof(byte*), "bytes")
                                                 .WithParameter<uint>("maxSize")
                                                 .Returning<int>()
                                             );

        RpcSerializerWriteNullableObjectByValBytes = iRpcSerializerMethods
                                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                              && x.IsGenericMethod
                                                                              && x.GetParameters() is { Length: 3 } p
                                                                              && p[0].ParameterType.IsByRef
                                                                              && p[0].ParameterType.GetElementType()! == typeof(Nullable<>).MakeGenericType(x.GetGenericArguments()[0])
                                                         )
                                                     ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                         .WithGenericParameterDefinition("T")
                                                         .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                         .WithParameter(typeof(byte*), "bytes")
                                                         .WithParameter<uint>("maxSize")
                                                         .Returning<int>()
                                                     );

        RpcSerializerWriteObjectByTRefStream = iRpcSerializerMethods
                                                   .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                        && !x.IsGenericMethod
                                                                        && x.GetParameters() is { Length: 2 } p
                                                                        && p[0].ParameterType == typeof(TypedReference)
                                                                  )
                                               ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                   .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                   .WithParameter(typeof(TypedReference), "value")
                                                   .WithParameter<Stream>("stream")
                                                   .Returning<int>()
                                               )}");

        RpcSerializerWriteObjectByValStream = iRpcSerializerMethods
                                                  .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                       && x.IsGenericMethod
                                                                       && x.GetParameters() is { Length: 2 } p
                                                                       && p[0].ParameterType == x.GetGenericArguments()[0]
                                                                 )
                                              ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                  .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                  .WithGenericParameterDefinition("T")
                                                  .WithParameterUsingGeneric(0, "value")
                                                  .WithParameter<Stream>("stream")
                                                  .Returning<int>()
                                              )}");

        RpcSerializerWriteNullableObjectByValStream = iRpcSerializerMethods
                                                          .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteObject)
                                                                               && x.IsGenericMethod
                                                                               && x.GetParameters() is { Length: 2 } p
                                                                               && p[0].ParameterType.IsByRef
                                                                               && p[0].ParameterType.GetElementType()! == typeof(Nullable<>).MakeGenericType(x.GetGenericArguments()[0])
                                                          )
                                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.WriteObject))
                                                          .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                          .WithGenericParameterDefinition("T")
                                                          .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                          .WithParameter<Stream>("stream")
                                                          .Returning<int>()
                                                      )}");

        RpcSerializerReadObjectByTRefBytes = iRpcSerializerMethods
                                                  .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadObject)
                                                                       && !x.IsGenericMethod
                                                                       && x.GetParameters() is { Length: 4 } p
                                                                       && p[0].ParameterType == typeof(TypedReference)
                                                                       && x.ReturnType == typeof(void)
                                                                 )
                                              ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.ReadObject))
                                                  .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                  .WithParameter(typeof(TypedReference), "refValue")
                                                  .WithParameter(typeof(byte*), "bytes")
                                                  .WithParameter<uint>("maxSize")
                                                  .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                  .ReturningVoid()
                                              );

        RpcSerializerReadNullableObjectByTRefBytes = iRpcSerializerMethods
                                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullable)
                                                                              && x.IsGenericMethod
                                                                              && x.GetParameters() is { Length: 4 } p
                                                                              && p[0].ParameterType == typeof(TypedReference)
                                                                              && p[1].ParameterType == typeof(byte*)
                                                                              && x.ReturnType == typeof(void)
                                                         )
                                                     ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.ReadNullable))
                                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                         .WithGenericParameterDefinition("T")
                                                         .WithParameter(typeof(TypedReference), "refValue")
                                                         .WithParameter(typeof(byte*), "bytes")
                                                         .WithParameter<uint>("maxSize")
                                                         .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                         .ReturningVoid()
                                                     );

        RpcSerializerReadObjectByValBytes = iRpcSerializerMethods
                                                 .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadObject)
                                                                      && x.IsGenericMethod
                                                                      && x.GetParameters() is { Length: 3 } p
                                                                      && p[0].ParameterType == typeof(byte*)
                                                                      && x.ReturnType == x.GetGenericArguments()[0]
                                                                )
                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.ReadObject))
                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                 .WithGenericParameterDefinition("T")
                                                 .WithParameter(typeof(byte*), "bytes")
                                                 .WithParameter<uint>("maxSize")
                                                 .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                 .ReturningUsingGeneric("T")
                                             );

        RpcSerializerReadNullableObjectByValBytes = iRpcSerializerMethods
                                                        .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullable)
                                                                             && x.IsGenericMethod
                                                                             && x.GetParameters() is { Length: 3 } p
                                                                             && p[0].ParameterType == typeof(byte*)
                                                        )
                                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.ReadNullable))
                                                        .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                        .WithGenericParameterDefinition("T")
                                                        .WithParameter(typeof(byte*), "bytes")
                                                        .WithParameter<uint>("maxSize")
                                                        .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                    );

        RpcSerializerReadObjectByTRefStream = iRpcSerializerMethods
                                                   .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadObject)
                                                                        && !x.IsGenericMethod
                                                                        && x.GetParameters() is { Length: 3 } p
                                                                        && p[0].ParameterType == typeof(TypedReference)
                                                                        && x.ReturnType == typeof(void)
                                                                  )
                                               ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadObject))
                                                   .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                   .WithParameter(typeof(TypedReference), "refValue")
                                                   .WithParameter<Stream>("stream")
                                                   .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                   .ReturningVoid()
                                               )}");

        RpcSerializerReadObjectByValStream = iRpcSerializerMethods
                                                  .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadObject)
                                                                       && x.IsGenericMethod
                                                                       && x.GetParameters() is { Length: 2 } p
                                                                       && p[0].ParameterType == typeof(Stream)
                                                                       && x.ReturnType == x.GetGenericArguments()[0]
                                                                 )
                                              ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadObject))
                                                  .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                  .WithGenericParameterDefinition("T")
                                                  .WithParameter<Stream>("stream")
                                                  .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                  .ReturningUsingGeneric("T")
                                              )}");

        RpcSerializerReadNullableObjectByValStream = iRpcSerializerMethods
                                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullable)
                                                                              && x.IsGenericMethod
                                                                              && x.GetParameters() is { Length: 2 } p
                                                                              && p[0].ParameterType == typeof(Stream)
                                                                        )
                                                     ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadNullable))
                                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                         .WithGenericParameterDefinition("T")
                                                         .WithParameter<Stream>("stream")
                                                         .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                     )}");

        RpcSerializerReadNullableObjectByTRefStream = iRpcSerializerMethods
                                                          .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullable)
                                                                               && x.IsGenericMethod
                                                                               && x.GetParameters() is { Length: 3 } p
                                                                               && p[0].ParameterType == typeof(TypedReference)
                                                                               && p[1].ParameterType == typeof(Stream)
                                                          )
                                                      ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadNullable))
                                                          .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                          .WithGenericParameterDefinition("T")
                                                          .WithParameter<Stream>("stream")
                                                          .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                      )}");
    }
}
