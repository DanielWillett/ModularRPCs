using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class CommonReflectionCache
{
    public static MethodInfo GetNullableHasValueGetter(Type nullableType)
    {
        return nullableType.GetProperty(nameof(Nullable<int>.HasValue)
                   , BindingFlags.Instance | BindingFlags.Public)?.GetMethod
               ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.HasValue))
                   .DeclaredIn(nullableType, isStatic: false)
                   .WithNoSetter()
                   .WithPropertyType<bool>());
    }

    public static MethodInfo GetNullableValueGetter(Type nullableType)
    {
        return nullableType.GetProperty(nameof(Nullable<int>.Value)
                   , BindingFlags.Instance | BindingFlags.Public)?.GetMethod
               ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Nullable<int>.Value))
                   .DeclaredIn(nullableType, isStatic: false)
                   .WithNoSetter()
                   .WithPropertyType(Nullable.GetUnderlyingType(nullableType) ?? typeof(object)));
    }

    public static ConstructorInfo GetNullableConstructor(Type nullableType)
    {
        return nullableType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]
               ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nullableType, false)
                   .WithParameter(Nullable.GetUnderlyingType(nullableType) ?? typeof(object), "value"));
    }

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
    /// <see cref="DBNull.Value"/>.
    /// </summary>
    internal static readonly FieldInfo GetDbNullValue = typeof(DBNull).GetField(nameof(DBNull.Value), BindingFlags.Public | BindingFlags.Static)
                                                        ?? throw new UnexpectedMemberAccessException(new FieldDefinition(nameof(DBNull.Value))
                                                            .DeclaredIn<DBNull>(isStatic: true)
                                                            .AsReadOnly()
                                                            .WithFieldType<DBNull>()
                                                        );

    /// <summary>
    /// <see cref="Action(object?, IntPtr)"/>.
    /// </summary>
    internal static readonly ConstructorInfo ActionConstructor = typeof(Action).GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];

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
    /// <see cref="IRpcRouter.InvokeRpc(object?,IRpcSerializer,RuntimeMethodHandle,CancellationToken,byte*,int,uint,ref RpcCallMethodInfo,RpcInvokeOptions)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterInvokeRpcBytes = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.InvokeRpc), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
                                                                      [ typeof(object), typeof(IRpcSerializer), typeof(RuntimeMethodHandle), typeof(CancellationToken), typeof(byte*), typeof(int), typeof(uint), typeof(RpcCallMethodInfo).MakeByRefType(), typeof(RpcInvokeOptions) ], null)
                                                                  ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.InvokeRpc))
                                                                      .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                      .WithParameter<object>("connections")
                                                                      .WithParameter<IRpcSerializer>("serializer")
                                                                      .WithParameter<RuntimeMethodHandle>("sourceMethodHandle")
                                                                      .WithParameter<CancellationToken>("token")
                                                                      .WithParameter(typeof(byte*), "bytesSt")
                                                                      .WithParameter<int>("byteCt")
                                                                      .WithParameter<uint>("dataCt")
                                                                      .WithParameter<RpcCallMethodInfo>("callMethodInfo", ByRefTypeMode.Ref)
                                                                      .WithParameter<RpcInvokeOptions>("options")
                                                                      .Returning<RpcTask>()
                                                                  );

    /// <summary>
    /// <see cref="IRpcRouter.InvokeRpc(object?,IRpcSerializer,RuntimeMethodHandle,CancellationToken,ArraySegment{byte},Stream,bool,uint,ref RpcCallMethodInfo)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterInvokeRpcStream = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.InvokeRpc), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any,
                                                                       [ typeof(object), typeof(IRpcSerializer), typeof(RuntimeMethodHandle), typeof(CancellationToken), typeof(ArraySegment<byte>), typeof(Stream), typeof(bool), typeof(uint), typeof(RpcCallMethodInfo).MakeByRefType() ], null)
                                                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.InvokeRpc))
                                                                       .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                       .WithParameter<object>("connections")
                                                                       .WithParameter<IRpcSerializer>("serializer")
                                                                       .WithParameter<RuntimeMethodHandle>("sourceMethodHandle")
                                                                       .WithParameter<CancellationToken>("token")
                                                                       .WithParameter<ArraySegment<byte>>("overheadBuffer")
                                                                       .WithParameter<Stream>("dataStream")
                                                                       .WithParameter<bool>("leaveOpen")
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
    /// <see cref="IRpcRouter.HandleInvokeVoidReturn"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeVoidReturn = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeVoidReturn), BindingFlags.Public | BindingFlags.Instance)
                                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeVoidReturn))
                                                                              .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                              .WithParameter<RpcOverhead>("overhead")
                                                                              .WithParameter<IRpcSerializer>("serializer")
                                                                              .ReturningVoid()
                                                                          );

    /// <summary>
    /// <see cref="IRpcRouter.HandleInvokeException"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeException = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeException), BindingFlags.Public | BindingFlags.Instance)
                                                                         ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeException))
                                                                             .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                             .WithParameter<Exception>("exception")
                                                                             .WithParameter<RpcOverhead>("overhead")
                                                                             .WithParameter<IRpcSerializer>("serializer")
                                                                             .ReturningVoid()
                                                                         );

    /// <summary>
    /// <see cref="IRpcRouter.HandleInvokeReturnValue{TReturnType}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeReturnValue = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeReturnValue), BindingFlags.Public | BindingFlags.Instance)
                                                                           ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeReturnValue))
                                                                               .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                               .WithGenericParameterDefinition("TReturnType")
                                                                               .WithParameterUsingGeneric(0, "value")
                                                                               .WithParameter<RpcOverhead>("overhead")
                                                                               .WithParameter<IRpcSerializer>("serializer")
                                                                               .ReturningVoid()
                                                                           );

    /// <summary>
    /// <see cref="IRpcRouter.HandleInvokeNullableReturnValue{TReturnType}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeNullableReturnValue = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeNullableReturnValue), BindingFlags.Public | BindingFlags.Instance)
                                                                                   ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeNullableReturnValue))
                                                                                       .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                                       .WithGenericParameterDefinition("TReturnType")
                                                                                       .WithParameterUsingGeneric(0, "value?")
                                                                                       .WithParameter<RpcOverhead>("overhead")
                                                                                       .WithParameter<IRpcSerializer>("serializer")
                                                                                       .ReturningVoid()
                                                                                   );

    /// <summary>
    /// <see cref="IRpcRouter.HandleInvokeSerializableReturnValue{TSerializable}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeSerializableReturnValue = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeSerializableReturnValue), BindingFlags.Public | BindingFlags.Instance)
                                                                                       ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeSerializableReturnValue))
                                                                                           .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                                           .WithGenericParameterDefinition("TSerializable")
                                                                                           .WithParameterUsingGeneric(0, "value")
                                                                                           .WithParameter<object>("collection")
                                                                                           .WithParameter<RpcOverhead>("overhead")
                                                                                           .WithParameter<IRpcSerializer>("serializer")
                                                                                           .ReturningVoid()
                                                                                       );

    /// <summary>
    /// <see cref="IRpcRouter.HandleInvokeNullableSerializableReturnValue{TSerializable}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcRouterHandleInvokeNullableSerializableReturnValue = typeof(IRpcRouter).GetMethod(nameof(IRpcRouter.HandleInvokeNullableSerializableReturnValue), BindingFlags.Public | BindingFlags.Instance)
                                                                                               ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcRouter.HandleInvokeNullableSerializableReturnValue))
                                                                                                   .DeclaredIn<IRpcRouter>(isStatic: false)
                                                                                                   .WithGenericParameterDefinition("TSerializable")
                                                                                                   .WithParameterUsingGeneric(0, "value?")
                                                                                                   .WithParameter<object>("collection")
                                                                                                   .WithParameter<RpcOverhead>("overhead")
                                                                                                   .WithParameter<IRpcSerializer>("serializer")
                                                                                                   .ReturningVoid()
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
    /// <see cref="RpcOverhead.MessageSize"/>.
    /// </summary>
    internal static readonly MethodInfo RpcOverheadGetMessageSize = typeof(RpcOverhead).GetProperty(nameof(RpcOverhead.MessageSize), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true)
                                                                    ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverhead.MessageSize))
                                                                        .DeclaredIn<RpcOverhead>(isStatic: false)
                                                                        .WithPropertyType<uint>()
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
    /// <see cref="RpcOverflowException.ErrorCode"/>.
    /// </summary>
    internal static readonly MethodInfo SetRpcOverflowExceptionErrorCode = typeof(RpcOverflowException).GetProperty(nameof(RpcOverflowException.ErrorCode), BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(true)
                                                                           ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(RpcOverflowException.ErrorCode))
                                                                               .DeclaredIn<RpcOverflowException>(isStatic: false)
                                                                               .WithPropertyType<int>()
                                                                               .WithNoGetter()
                                                                           );

    /// <summary>
    /// <see cref="RpcOverflowException(string)"/>.
    /// </summary>
    internal static readonly ConstructorInfo RpcOverflowExceptionCtorMessage = typeof(RpcOverflowException).GetConstructor([typeof(string)])
                                                                               ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(RpcOverflowException))
                                                                                   .WithParameter<string>("message")
                                                                               );

    /// <summary>
    /// <see cref="TimeSpan(long)"/>
    /// </summary>
    internal static readonly ConstructorInfo TimeSpanTicksCtor = typeof(TimeSpan).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(long) ], null)
                                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(TimeSpan))
                                                                     .WithParameter<long>("ticks")
                                                                 );

    /// <summary>
    /// <see cref="ProxyGenerator.CallerInfoFieldNameAttribute(string)"/>
    /// </summary>
    internal static readonly ConstructorInfo CallerInfoFieldNameAttributeCtor = typeof(ProxyGenerator.CallerInfoFieldNameAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [ typeof(string) ], null)
                                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(ProxyGenerator.CallerInfoFieldNameAttribute))
                                                                     .WithParameter<string>("fieldName")
                                                                 );

    /// <summary>
    /// <see cref="ReadOnlyMemory{T}.Length"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo GetReadOnlyMemoryLength = typeof(ReadOnlyMemory<byte>).GetProperty(nameof(ReadOnlyMemory<byte>.Length), BindingFlags.Public | BindingFlags.Instance)
                                                                      ?.GetGetMethod(true)
                                                                 ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ReadOnlyMemory<byte>.Length))
                                                                     .DeclaredIn<ReadOnlyMemory<byte>>(isStatic: false)
                                                                     .WithPropertyType<int>()
                                                                     .WithNoSetter()
                                                                 );

    /// <summary>
    /// <see cref="Memory{T}.Length"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo GetMemoryLength = typeof(Memory<byte>).GetProperty(nameof(Memory<byte>.Length), BindingFlags.Public | BindingFlags.Instance)
                                                                      ?.GetGetMethod(true)
                                                                 ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Memory<byte>.Length))
                                                                     .DeclaredIn<Memory<byte>>(isStatic: false)
                                                                     .WithPropertyType<int>()
                                                                     .WithNoSetter()
                                                                 );

    /// <summary>
    /// <see cref="ReadOnlySpan{T}.Length"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo GetReadOnlySpanLength = typeof(ReadOnlySpan<byte>).GetProperty(nameof(ReadOnlySpan<byte>.Length), BindingFlags.Public | BindingFlags.Instance)
                                                                      ?.GetGetMethod(true)
                                                                 ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ReadOnlySpan<byte>.Length))
                                                                     .DeclaredIn(typeof(ReadOnlySpan<byte>), isStatic: false)
                                                                     .WithPropertyType<int>()
                                                                     .WithNoSetter()
                                                                 );

    /// <summary>
    /// <see cref="Span{T}.Length"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo GetSpanLength = typeof(Span<byte>).GetProperty(nameof(Span<byte>.Length), BindingFlags.Public | BindingFlags.Instance)
                                                                      ?.GetGetMethod(true)
                                                                 ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Span<byte>.Length))
                                                                     .DeclaredIn(typeof(Span<byte>), isStatic: false)
                                                                     .WithPropertyType<int>()
                                                                     .WithNoSetter()
                                                                 );

    /// <summary>
    /// <see cref="ReadOnlyMemory{T}.Span"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo GetReadOnlyMemorySpan = typeof(ReadOnlyMemory<byte>).GetProperty(nameof(ReadOnlyMemory<byte>.Span), BindingFlags.Public | BindingFlags.Instance)
                                                                    ?.GetGetMethod(true)
                                                                ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ReadOnlyMemory<byte>.Span))
                                                                    .DeclaredIn<ReadOnlyMemory<byte>>(isStatic: false)
                                                                    .WithPropertyType(typeof(ReadOnlySpan<byte>))
                                                                    .WithNoSetter()
                                                                );

    /// <summary>
    /// <see cref="Memory{T}.op_Implicit(Memory{T})"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo MemoryToReadOnlyMemory = typeof(Memory<byte>).GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(Memory<byte>) ], null)
                                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition("op_Implicit")
                                                                     .DeclaredIn<Memory<byte>>(isStatic: true)
                                                                     .WithParameter<Memory<byte>>("memory")
                                                                     .Returning<ReadOnlyMemory<byte>>()
                                                                 );

    /// <summary>
    /// <see cref="Span{T}.op_Implicit(Span{T})"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo SpanToReadOnlySpan = typeof(Span<byte>).GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, [ typeof(Span<byte>) ], null)
                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition("op_Implicit")
                                                                 .DeclaredIn(typeof(Span<byte>), isStatic: true)
                                                                 .WithParameter(typeof(Span<byte>), "span")
                                                                 .Returning(typeof(ReadOnlySpan<byte>))
                                                             );
    /// <summary>
    /// <see cref="ReadOnlySpan{T}.GetPinnableReference"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo PinReadOnlySpan = typeof(ReadOnlySpan<byte>).GetMethod(nameof(ReadOnlySpan<byte>.GetPinnableReference), BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, Type.EmptyTypes, null)
                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ReadOnlySpan<byte>.GetPinnableReference))
                                                              .DeclaredIn(typeof(ReadOnlySpan<byte>), isStatic: false)
                                                              .WithNoParameters()
                                                              .Returning<byte>(ByRefTypeMode.RefReadOnly)
                                                          );

    /// <summary>
    /// <see cref="MemoryStream(byte[], int, int, bool, bool)"/>
    /// </summary>
    internal static readonly ConstructorInfo CtorFullMemoryStream = typeof(MemoryStream).GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, CallingConventions.Any, [ typeof(byte[]), typeof(int), typeof(int), typeof(bool), typeof(bool) ], null)
                                                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(MemoryStream))
                                                                        .WithParameter<byte[]>("buffer")
                                                                        .WithParameter<int>("index")
                                                                        .WithParameter<int>("count")
                                                                        .WithParameter<bool>("writable")
                                                                        .WithParameter<bool>("publiclyVisible")
                                                                    );

    /// <summary>
    /// <see cref="InvalidOperationException(string)"/>
    /// </summary>
    internal static readonly ConstructorInfo CtorInvalidOperationException = typeof(InvalidOperationException).GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, CallingConventions.Any, [ typeof(string) ], null)
                                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(InvalidOperationException))
                                                                                 .WithParameter<string>("message")
                                                                             );

    /// <summary>
    /// <see cref="ArraySegment{T}.Array"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo ByteArraySegmentArray = typeof(ArraySegment<byte>).GetProperty(nameof(ArraySegment<byte>.Array), BindingFlags.Public | BindingFlags.Instance)
                                                                    ?.GetGetMethod(true)
                                                                ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ArraySegment<byte>.Array))
                                                                    .DeclaredIn<ArraySegment<byte>>(isStatic: false)
                                                                    .WithPropertyType<byte[]>()
                                                                    .WithNoSetter()
                                                                );

    /// <summary>
    /// <see cref="ArraySegment{T}.Count"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo ByteArraySegmentCount = typeof(ArraySegment<byte>).GetProperty(nameof(ArraySegment<byte>.Count), BindingFlags.Public | BindingFlags.Instance)
                                                                    ?.GetGetMethod(true)
                                                                ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ArraySegment<byte>.Count))
                                                                    .DeclaredIn<ArraySegment<byte>>(isStatic: false)
                                                                    .WithPropertyType<int>()
                                                                    .WithNoSetter()
                                                                );

    /// <summary>
    /// <see cref="ArraySegment{T}.Offset"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo ByteArraySegmentOffset = typeof(ArraySegment<byte>).GetProperty(nameof(ArraySegment<byte>.Offset), BindingFlags.Public | BindingFlags.Instance)
                                                                     ?.GetGetMethod(true)
                                                                 ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ArraySegment<byte>.Offset))
                                                                     .DeclaredIn<ArraySegment<byte>>(isStatic: false)
                                                                     .WithPropertyType<int>()
                                                                     .WithNoSetter()
                                                                 );

    /// <summary>
    /// <see cref="ArraySegment{T}(T[])"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly ConstructorInfo CtorByteArraySegmentJustArray = typeof(ArraySegment<byte>).GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, CallingConventions.Any, [ typeof(byte[]) ], null)
                                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(typeof(ArraySegment<byte>))
                                                                                 .WithParameter<byte[]>("array")
                                                                             );

    /// <summary>
    /// <see cref="Stream.Length"/>
    /// </summary>
    internal static readonly MethodInfo StreamLength = typeof(Stream).GetProperty(nameof(Stream.Length), BindingFlags.Public | BindingFlags.Instance)
                                                           ?.GetGetMethod(true)
                                                       ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Stream.Length))
                                                           .DeclaredIn<Stream>(isStatic: false)
                                                           .WithPropertyType<long>()
                                                           .WithNoSetter()
                                                       );

    /// <summary>
    /// <see cref="Stream.Position"/>
    /// </summary>
    internal static readonly MethodInfo StreamPosition = typeof(Stream).GetProperty(nameof(Stream.Position), BindingFlags.Public | BindingFlags.Instance)
                                                           ?.GetGetMethod(true)
                                                       ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(Stream.Position))
                                                           .DeclaredIn<Stream>(isStatic: false)
                                                           .WithPropertyType<long>()
                                                           .WithNoSetter()
                                                       );

    /// <summary>
    /// <see cref="ICollection{T}.Count"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo ByteCollectionCount = typeof(ICollection<byte>).GetProperty(nameof(ICollection<byte>.Count), BindingFlags.Public | BindingFlags.Instance)
                                                                  ?.GetGetMethod(true)
                                                              ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(ICollection<byte>.Count))
                                                                  .DeclaredIn<ICollection<byte>>(isStatic: false)
                                                                  .WithPropertyType<int>()
                                                                  .WithNoSetter()
                                                              );

    /// <summary>
    /// <see cref="IReadOnlyCollection{T}.Count"/> of <see cref="byte"/>
    /// </summary>
    internal static readonly MethodInfo ByteReadOnlyCollectionCount = typeof(IReadOnlyCollection<byte>).GetProperty(nameof(IReadOnlyCollection<byte>.Count), BindingFlags.Public | BindingFlags.Instance)
                                                                          ?.GetGetMethod(true)
                                                                      ?? throw new UnexpectedMemberAccessException(new PropertyDefinition(nameof(IReadOnlyCollection<byte>.Count))
                                                                          .DeclaredIn<IReadOnlyCollection<byte>>(isStatic: false)
                                                                          .WithPropertyType<int>()
                                                                          .WithNoSetter()
                                                                      );

    /// <summary>
    /// <see cref="ICriticalNotifyCompletion.UnsafeOnCompleted"/>
    /// </summary>
    internal static readonly MethodInfo CriticalNotifyCompletionOnCompleted = typeof(ICriticalNotifyCompletion).GetMethod(nameof(ICriticalNotifyCompletion.UnsafeOnCompleted), BindingFlags.Public | BindingFlags.Instance)
                                                                              ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(ICriticalNotifyCompletion.UnsafeOnCompleted))
                                                                                  .DeclaredIn<ICriticalNotifyCompletion>(isStatic: false)
                                                                                  .WithParameter<Action>("continuation")
                                                                                  .ReturningVoid()
                                                                              );

    /// <summary>
    /// <see cref="INotifyCompletion.OnCompleted"/>
    /// </summary>
    internal static readonly MethodInfo NotifyCompletionOnCompleted = typeof(INotifyCompletion).GetMethod(nameof(INotifyCompletion.OnCompleted), BindingFlags.Public | BindingFlags.Instance)
                                                                      ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(INotifyCompletion.OnCompleted))
                                                                          .DeclaredIn<INotifyCompletion>(isStatic: false)
                                                                          .WithParameter<Action>("continuation")
                                                                          .ReturningVoid()
                                                                      );

    /// <summary>
    /// <see cref="IRpcSerializer.GetSerializableSize{TSerializable}(in TSerializable)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerGetSerializableObjectSize;

    /// <summary>
    /// <see cref="IRpcSerializer.GetNullableSerializableSize{TSerializable}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerGetNullableSerializableObjectSize;

    /// <summary>
    /// <see cref="IRpcSerializer.GetSerializablesSize{TSerializable}(IEnumerable{TSerializable})"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerGetSerializableObjectsSize;

    /// <summary>
    /// <see cref="IRpcSerializer.GetNullableSerializablesSize{TSerializable}"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerGetNullableSerializableObjectsSize;

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
    /// <see cref="IRpcSerializer.GetSize{T}(in Nullable{T})"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerGetSizeNullableByVal;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject(TypedReference,byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteObjectByTRefBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteSerializableObject{TSerializable}(in TSerializable,byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteSerializableObjectBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteNullableSerializableObject{TSerializable}(in Nullable{TSerializable},byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteNullableSerializableObjectBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteSerializableObjects{TSerializable}(IEnumerable{TSerializable},byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteSerializableObjectsBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteNullableSerializableObjects{TSerializable}(IEnumerable{Nullable{TSerializable}},byte*,uint)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteNullableSerializableObjectsBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(T,byte*,uint)"/>.
    /// </summary>
    /// <remarks>Generic method definition.</remarks>
    internal static readonly MethodInfo RpcSerializerWriteObjectByValBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(in Nullable{T},byte*,uint)"/>.
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
    /// <see cref="IRpcSerializer.WriteSerializableObject{TSerializable}(in TSerializable,Stream)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteSerializableObjectStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteSerializableObject{TSerializable}(in Nullable{TSerializable},Stream)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteNullableSerializableObjectStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteSerializableObjects{TSerializable}(IEnumerable{TSerializable},Stream)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteSerializableObjectsStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteNullableSerializableObjects{TSerializable}(IEnumerable{Nullable{TSerializable}},Stream)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerWriteNullableSerializableObjectsStream;

    /// <summary>
    /// <see cref="IRpcSerializer.WriteObject{T}(in Nullable{T},Stream)"/>.
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
    /// <see cref="IRpcSerializer.ReadSerializableObject{TSerializable}(byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullableSerializableObject{TSerializable}(byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadNullableSerializableObjectBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadSerializableObjects{TSerializable}(byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectsArrayBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadSerializableObjects{TSerializable,TCollectionType}(byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectsAnyBytes;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullableSerializableObjects{TSerializable,TCollectionType}(byte*,uint,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadNullableSerializableObjectsAnyBytes;

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
    /// <see cref="IRpcSerializer.ReadSerializableObject{TSerializable}(Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullableSerializableObject{TSerializable}(Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadNullableSerializableObjectStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadSerializableObjects{TSerializable}(Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectsArrayStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadSerializableObjects{TSerializable,TCollectionType}(Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadSerializableObjectsAnyStream;

    /// <summary>
    /// <see cref="IRpcSerializer.ReadNullableSerializableObjects{TSerializable,TCollectionType}(Stream,out int)"/>.
    /// </summary>
    internal static readonly MethodInfo RpcSerializerReadNullableSerializableObjectsAnyStream;


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

        RpcSerializerGetSerializableObjectSize = iRpcSerializerMethods
                                                     .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSerializableSize)
                                                                          && x.IsGenericMethod
                                                                          && x.GetParameters() is { Length: 1 } p
                                                                          && !typeof(IEnumerable).IsAssignableFrom(p[0].ParameterType)
                                                     )
                                                 ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetSerializableSize))
                                                     .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                     .WithGenericParameterDefinition("TSerializable")
                                                     .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                     .Returning<int>()
                                                 );

        RpcSerializerGetNullableSerializableObjectSize = iRpcSerializerMethods
                                                             .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetNullableSerializableSize)
                                                                 && x.IsGenericMethod
                                                                 && x.GetParameters() is { Length: 1 } p
                                                                 && !typeof(IEnumerable).IsAssignableFrom(p[0].ParameterType)
                                                             )
                                                         ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetNullableSerializableSize))
                                                             .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                             .WithGenericParameterDefinition("TSerializable")
                                                             .WithParameterUsingGeneric(0, "value?", byRefMode: ByRefTypeMode.In)
                                                             .Returning<int>()
                                                         );

        RpcSerializerGetSerializableObjectsSize = iRpcSerializerMethods
                                                      .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetSerializablesSize)
                                                                           && x.IsGenericMethod
                                                                           && x.GetParameters() is { Length: 1 } p
                                                                           && typeof(IEnumerable).IsAssignableFrom(p[0].ParameterType)
                                                      )
                                                  ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetSerializablesSize))
                                                      .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                      .WithGenericParameterDefinition("TSerializable")
                                                      .WithParameterUsingGeneric(0, "IEnumerable<value>")
                                                      .Returning<int>()
                                                  );

        RpcSerializerGetNullableSerializableObjectsSize = iRpcSerializerMethods
                                                              .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.GetNullableSerializablesSize)
                                                                  && x.IsGenericMethod
                                                                  && x.GetParameters() is { Length: 1 } p
                                                                  && typeof(IEnumerable).IsAssignableFrom(p[0].ParameterType)
                                                              )
                                                          ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.GetNullableSerializablesSize))
                                                              .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                              .WithGenericParameterDefinition("TSerializable")
                                                              .WithParameterUsingGeneric(0, "IEnumerable<value?>")
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

        RpcSerializerWriteSerializableObjectBytes = iRpcSerializerMethods
                                                        .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteSerializableObject)
                                                                             && x.IsGenericMethod
                                                                             && x.GetParameters() is { Length: 3 }
                                                        )
                                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteSerializableObject))
                                                        .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                        .WithGenericParameterDefinition("TSerializable")
                                                        .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                        .WithParameter(typeof(byte*), "bytes")
                                                        .WithParameter<uint>("maxSize")
                                                        .Returning<int>()
                                                    );

        RpcSerializerWriteNullableSerializableObjectBytes = iRpcSerializerMethods
                                                                .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteNullableSerializableObject)
                                                                    && x.IsGenericMethod
                                                                    && x.GetParameters() is { Length: 3 }
                                                                )
                                                            ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteNullableSerializableObject))
                                                                .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                .WithGenericParameterDefinition("TSerializable")
                                                                .WithParameterUsingGeneric(0, "value?", byRefMode: ByRefTypeMode.In)
                                                                .WithParameter(typeof(byte*), "bytes")
                                                                .WithParameter<uint>("maxSize")
                                                                .Returning<int>()
                                                            );

        RpcSerializerWriteSerializableObjectsBytes = iRpcSerializerMethods
                                                        .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteSerializableObjects)
                                                                             && x.IsGenericMethod
                                                                             && x.GetParameters() is { Length: 3 }
                                                        )
                                                    ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteSerializableObjects))
                                                        .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                        .WithGenericParameterDefinition("TSerializable")
                                                        .WithParameterUsingGeneric(0, "IEnumerable<value>")
                                                        .WithParameter(typeof(byte*), "bytes")
                                                        .WithParameter<uint>("maxSize")
                                                        .Returning<int>()
                                                    );

        RpcSerializerWriteNullableSerializableObjectsBytes = iRpcSerializerMethods
                                                                 .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteNullableSerializableObjects)
                                                                                      && x.IsGenericMethod
                                                                                      && x.GetParameters() is { Length: 3 }
                                                                 )
                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteNullableSerializableObjects))
                                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                 .WithGenericParameterDefinition("TSerializable")
                                                                 .WithParameterUsingGeneric(0, "IEnumerable<value?>")
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

        RpcSerializerWriteSerializableObjectStream = iRpcSerializerMethods
                                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteSerializableObject)
                                                                              && x.IsGenericMethod
                                                                              && x.GetParameters() is { Length: 2 }
                                                         )
                                                     ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteSerializableObject))
                                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                         .WithGenericParameterDefinition("TSerializable")
                                                         .WithParameterUsingGeneric(0, "value", byRefMode: ByRefTypeMode.In)
                                                         .WithParameter<Stream>("stream")
                                                         .Returning<int>()
                                                     );

        RpcSerializerWriteNullableSerializableObjectStream = iRpcSerializerMethods
                                                                 .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteNullableSerializableObject)
                                                                                      && x.IsGenericMethod
                                                                                      && x.GetParameters() is { Length: 2 }
                                                                 )
                                                             ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteNullableSerializableObject))
                                                                 .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                 .WithGenericParameterDefinition("TSerializable")
                                                                 .WithParameterUsingGeneric(0, "value?", byRefMode: ByRefTypeMode.In)
                                                                 .WithParameter<Stream>("stream")
                                                                 .Returning<int>()
                                                             );

        RpcSerializerWriteSerializableObjectsStream = iRpcSerializerMethods
                                                         .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteSerializableObjects)
                                                                              && x.IsGenericMethod
                                                                              && x.GetParameters() is { Length: 2 }
                                                         )
                                                     ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteSerializableObjects))
                                                         .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                         .WithGenericParameterDefinition("TSerializable")
                                                         .WithParameterUsingGeneric(0, "IEnumerable<value>")
                                                         .WithParameter<Stream>("stream")
                                                         .Returning<int>()
                                                     );
        
        RpcSerializerWriteNullableSerializableObjectsStream = iRpcSerializerMethods
                                                                  .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.WriteNullableSerializableObjects)
                                                                                       && x.IsGenericMethod
                                                                                       && x.GetParameters() is { Length: 2 }
                                                                  )
                                                              ?? throw new UnexpectedMemberAccessException(new MethodDefinition(nameof(IRpcSerializer.WriteNullableSerializableObjects))
                                                                  .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                  .WithGenericParameterDefinition("TSerializable")
                                                                  .WithParameterUsingGeneric(0, "IEnumerable<value?>")
                                                                  .WithParameter<Stream>("stream")
                                                                  .Returning<int>()
                                                              );
        
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

        RpcSerializerReadSerializableObjectBytes = iRpcSerializerMethods
                                                       .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObject)
                                                                            && x.IsGenericMethod
                                                                            && x.GetParameters().Length == 3
                                                       )
                                                   ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObject))
                                                       .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                       .WithGenericParameterDefinition("TSerializable")
                                                       .WithParameter(typeof(byte*), "bytes")
                                                       .WithParameter<uint>("maxSize")
                                                       .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                       .ReturningUsingGeneric(0)
                                                   )}");

        RpcSerializerReadNullableSerializableObjectBytes = iRpcSerializerMethods
                                                               .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullableSerializableObject)
                                                                   && x.IsGenericMethod
                                                                   && x.GetParameters().Length == 3
                                                               )
                                                           ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObject))
                                                               .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                               .WithGenericParameterDefinition("TSerializable?")
                                                               .WithParameter(typeof(byte*), "bytes")
                                                               .WithParameter<uint>("maxSize")
                                                               .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                               .ReturningUsingGeneric(0)
                                                           )}");

        RpcSerializerReadSerializableObjectsArrayBytes = iRpcSerializerMethods
                                                             .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObjects)
                                                                 && x.IsGenericMethod
                                                                 && x.GetParameters().Length == 3
                                                                 && x.ReturnType.IsArray
                                                             )
                                                         ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObjects))
                                                             .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                             .WithGenericParameterDefinition("TSerializable")
                                                             .WithParameter(typeof(byte*), "bytes")
                                                             .WithParameter<uint>("maxSize")
                                                             .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                             .ReturningUsingGeneric(0, elements: e => e.AddArray())
                                                         )}");

        RpcSerializerReadSerializableObjectsAnyBytes = iRpcSerializerMethods
                                                           .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObjects)
                                                               && x.IsGenericMethod
                                                               && x.GetParameters().Length == 3
                                                               && !x.ReturnType.IsArray
                                                           )
                                                       ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObjects))
                                                           .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                           .WithGenericParameterDefinition("TSerializable")
                                                           .WithGenericParameterDefinition("TCollectionType")
                                                           .WithParameter(typeof(byte*), "bytes")
                                                           .WithParameter<uint>("maxSize")
                                                           .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                           .ReturningUsingGeneric(1)
                                                       )}");

        RpcSerializerReadNullableSerializableObjectsAnyBytes = iRpcSerializerMethods
                                                                   .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullableSerializableObjects)
                                                                       && x.IsGenericMethod
                                                                       && x.GetParameters().Length == 3
                                                                       && !x.ReturnType.IsArray
                                                                   )
                                                               ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadNullableSerializableObjects))
                                                                   .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                   .WithGenericParameterDefinition("TSerializable")
                                                                   .WithGenericParameterDefinition("TCollectionType")
                                                                   .WithParameter(typeof(byte*), "bytes")
                                                                   .WithParameter<uint>("maxSize")
                                                                   .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                   .ReturningUsingGeneric(1)
                                                               )}");

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

        RpcSerializerReadSerializableObjectStream = iRpcSerializerMethods
                                                        .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObject)
                                                                             && x.IsGenericMethod
                                                                             && x.GetParameters().Length == 2
                                                        )
                                                    ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObject))
                                                        .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                        .WithGenericParameterDefinition("TSerializable")
                                                        .WithParameter<Stream>("stream")
                                                        .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                        .ReturningUsingGeneric(0)
                                                    )}");

        RpcSerializerReadNullableSerializableObjectStream = iRpcSerializerMethods
                                                                .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullableSerializableObject)
                                                                    && x.IsGenericMethod
                                                                    && x.GetParameters().Length == 2
                                                                )
                                                            ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadNullableSerializableObject))
                                                                .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                .WithGenericParameterDefinition("TSerializable")
                                                                .WithParameter<Stream>("stream")
                                                                .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                .ReturningUsingGeneric(0)
                                                            )}");

        RpcSerializerReadSerializableObjectsArrayStream = iRpcSerializerMethods
                                                              .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObjects)
                                                                  && x.IsGenericMethod
                                                                  && x.GetParameters().Length == 2
                                                                  && x.ReturnType.IsArray
                                                              )
                                                          ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObjects))
                                                              .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                              .WithGenericParameterDefinition("TSerializable")
                                                              .WithParameter<Stream>("stream")
                                                              .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                              .ReturningUsingGeneric(0, elements: e => e.AddArray())
                                                          )}");

        RpcSerializerReadSerializableObjectsAnyStream = iRpcSerializerMethods
                                                            .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadSerializableObjects)
                                                                && x.IsGenericMethod
                                                                && x.GetParameters().Length == 2
                                                                && !x.ReturnType.IsArray
                                                            )
                                                        ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadSerializableObjects))
                                                            .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                            .WithGenericParameterDefinition("TSerializable")
                                                            .WithGenericParameterDefinition("TCollectionType")
                                                            .WithParameter<Stream>("stream")
                                                            .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                            .ReturningUsingGeneric(1)
                                                        )}");

        RpcSerializerReadNullableSerializableObjectsAnyStream = iRpcSerializerMethods
                                                                    .FirstOrDefault(x => x.Name == nameof(IRpcSerializer.ReadNullableSerializableObjects)
                                                                        && x.IsGenericMethod
                                                                        && x.GetParameters().Length == 2
                                                                        && !x.ReturnType.IsArray
                                                                    )
                                                                ?? throw new MemberAccessException($"Failed to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IRpcSerializer.ReadNullableSerializableObjects))
                                                                    .DeclaredIn<IRpcSerializer>(isStatic: false)
                                                                    .WithGenericParameterDefinition("TSerializable")
                                                                    .WithGenericParameterDefinition("TCollectionType")
                                                                    .WithParameter<Stream>("stream")
                                                                    .WithParameter<int>("bytesRead", ByRefTypeMode.Out)
                                                                    .ReturningUsingGeneric(1)
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
