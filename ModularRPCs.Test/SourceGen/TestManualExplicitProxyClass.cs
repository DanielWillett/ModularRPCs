using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Loopback;
using DanielWillett.ModularRpcs.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModularRPCs.Test.CodeGen;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantNameQualifier

namespace ModularRpcs.Test.SourceGen;
public class TestManualExplicitProxyClass
{
    private static int _invokedMethod;
    private static bool _invokeMethodRan;

    private const int Val1 = 32;
    private const string Val2 = "test string";

    [Test]
    public async Task ServerToClientVoid([Values(true, false)] bool useStreams)
    {
        _invokedMethod = -1;
        _invokeMethodRan = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, useStreams);

        TestClass proxy = server.GetRequiredService<TestClass>();

        await proxy.InvokeFromServer(connection);

        Assert.That(_invokedMethod, Is.EqualTo(0));
        Assert.That(_invokeMethodRan, Is.True);
    }

    [Test]
    public async Task ClientToServerVoid([Values(true, false)] bool useStreams)
    {
        _invokedMethod = -1;
        _invokeMethodRan = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, useStreams);

        TestClass proxy = client.GetRequiredService<TestClass>();

        await proxy.InvokeFromClient();

        Assert.That(_invokedMethod, Is.EqualTo(0));
        Assert.That(_invokeMethodRan, Is.True);
    }

    [Test]
    public async Task ServerToClientTask([Values(true, false)] bool useStreams)
    {
        _invokedMethod = -1;
        _invokeMethodRan = false;

        LoopbackRpcServersideRemoteConnection connection = await TestSetup.SetupTest<TestClass>(out IServiceProvider server, out _, useStreams);

        TestClass proxy = server.GetRequiredService<TestClass>();

        await proxy.InvokeWithParamsFromServer(Val1, Val2, connection);

        Assert.That(_invokedMethod, Is.EqualTo(1));
        Assert.That(_invokeMethodRan, Is.True);
    }

    [Test]
    public async Task ClientToServerTask([Values(true, false)] bool useStreams)
    {
        _invokedMethod = -1;
        _invokeMethodRan = false;

        await TestSetup.SetupTest<TestClass>(out _, out IServiceProvider client, useStreams);

        TestClass proxy = client.GetRequiredService<TestClass>();

        await proxy.InvokeWithParamsFromClient(Val1, Val2);

        Assert.That(_invokedMethod, Is.EqualTo(1));
        Assert.That(_invokeMethodRan, Is.True);
    }



    [RpcClass]
    public sealed partial class TestClass
    {
        [RpcSend(nameof(Receive))]
        public partial RpcTask InvokeFromClient();

        [RpcSend(nameof(Receive))]
        public partial RpcTask InvokeFromServer(IModularRpcRemoteConnection connection);

        [RpcReceive]
        private void Receive()
        {
            _invokedMethod = 0;
        }

        [RpcSend(nameof(ReceiveWithParams))]
        public partial RpcTask InvokeWithParamsFromClient(int primitiveLikeValue, string nonPrimitiveLikeValue);

        [RpcSend(nameof(ReceiveWithParams))]
        public partial RpcTask InvokeWithParamsFromServer(int primitiveLikeValue, string nonPrimitiveLikeValue, IModularRpcRemoteConnection connection);

        [RpcReceive]
        private async Task ReceiveWithParams(int primitiveLikeValue, string nonPrimitiveLikeValue)
        {
            await Task.Delay(5);
            _invokedMethod = 1;
            Assert.That(primitiveLikeValue, Is.EqualTo(Val1));
            Assert.That(nonPrimitiveLikeValue, Is.EqualTo(Val2));
        }
    }

    [global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyTypeAttribute]
    [global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyReceiveMethodAttribute(
        nameof(@TestClass.Receive),
        nameof(@TestClass.ModularRpcsGeneratedInvokeReceiveOvl0Bytes), nameof(@TestClass.ModularRpcsGeneratedInvokeReceiveOvl0Stream)
    )]
    [global::DanielWillett.ModularRpcs.Annotations.RpcGeneratedProxyReceiveMethodAttribute(
        nameof(@TestClass.ReceiveWithParams),
        nameof(@TestClass.ModularRpcsGeneratedInvokeReceiveWithParamsOvl0Bytes), nameof(@TestClass.ModularRpcsGeneratedInvokeReceiveWithParamsOvl0Stream)
    )]
    partial class @TestClass : global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType
    {
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo _modularRpcsGeneratedProxyTypeInfo;
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private global::DanielWillett.ModularRpcs.Reflection.ProxyContext _modularRpcsGeneratedProxyContext;

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        void global::DanielWillett.ModularRpcs.Reflection.IRpcGeneratedProxyType.SetupGeneratedProxyInfo(
            global::DanielWillett.ModularRpcs.Reflection.GeneratedProxyTypeInfo info)
        {
            this._modularRpcsGeneratedProxyTypeInfo = info;
            info.Router.GetDefaultProxyContext(typeof(@TestClass), out this._modularRpcsGeneratedProxyContext);
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private int ModularRpcsGeneratedCalculateOverheadSize(
            global::System.RuntimeMethodHandle method,
            ref global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo callInfo,
            out int sizeWithoutId)
        {
            uint overheadSize = this._modularRpcsGeneratedProxyContext.Router.GetOverheadSize(method, ref callInfo);
            sizeWithoutId = (int)overheadSize;

            // no ID
            ++overheadSize;
            return (int)overheadSize;
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private unsafe int ModularRpcsGeneratedWriteIdentifier(byte* bytes, int maxSize)
        {
            *bytes = 1;
            return 0;
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static unsafe void ModularRpcsGeneratedInvokeReceiveOvl0Bytes(
            object serviceProvider,
            object targetObject,
            global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,
            global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,
            byte* bytes,
            uint maxSize,
            global::System.Threading.CancellationToken token)
        {
            _invokeMethodRan = true;
            @TestClass targetCasted = (@TestClass)targetObject;
            if (targetCasted == null)
            {
                throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(
                    string.Format(
                        global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull,
                        nameof(@TestClass),
                        nameof(@TestClass.Receive)
                    )
                );
            }

            targetCasted.Receive();
            router.HandleInvokeVoidReturn(overhead, serializer);
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static unsafe void ModularRpcsGeneratedInvokeReceiveOvl0Stream(
            object serviceProvider,
            object targetObject,
            global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,
            global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,
            global::System.IO.Stream stream,
            global::System.Threading.CancellationToken token)
        {
            _invokeMethodRan = true;
            @TestClass targetCasted = (@TestClass)targetObject;
            if (targetCasted == null)
            {
                throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(
                    string.Format(
                        global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull,
                        nameof(@TestClass),
                        nameof(@TestClass.Receive)
                    )
                );
            }

            targetCasted.Receive();
            router.HandleInvokeVoidReturn(overhead, serializer);
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static unsafe void ModularRpcsGeneratedInvokeReceiveWithParamsOvl0Bytes(
            object serviceProvider,
            object targetObject,
            global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,
            global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,
            byte* bytes,
            uint maxSize,
            global::System.Threading.CancellationToken token)
        {
            _invokeMethodRan = true;
            @TestClass targetCasted = (@TestClass)targetObject;
            if (targetCasted == null)
            {
                throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(
                    string.Format(
                        global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull,
                        nameof(@TestClass),
                        nameof(@TestClass.ReceiveWithParams)
                    )
                );
            }

            int index = 0;

            int bytesRead;


            bool canPreCalc = serializer.CanFastReadPrimitives;
            int param0;
            if (canPreCalc)
            {
                if (index + sizeof(int) > maxSize)
                {
                    throw new global::DanielWillett.ModularRpcs.Exceptions.RpcParseException(
                        string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcParseExceptionBufferRunOutFastRead,
                            "int", "primitiveLikeValue")
                    );
                }
                byte* target = bytes + index;
                if ((nint)target % sizeof(int) == 0)
                {
                    param0 = *(int*)target;
                }
                else
                {
                    param0 = global::System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>(target);
                }
                index += sizeof(int);
            }
            else
            {
                param0 = serializer.ReadObject<int>(bytes + index, maxSize - (uint)index, out bytesRead);
                index += bytesRead;
            }

            string param1 = serializer.ReadObject<string>(bytes + index, maxSize - (uint)index, out bytesRead);
            index += bytesRead;

            global::System.Threading.Tasks.Task result = targetCasted.ReceiveWithParams(param0, param1);

            global::System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = result.ConfigureAwait(false).GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                    router.HandleInvokeVoidReturn(overhead, serializer);
                }
                catch (Exception ex)
                {
                    router.HandleInvokeException(ex, overhead, serializer);
                }
                return;
            }

            awaiter.UnsafeOnCompleted(() =>
            {
                try
                {
                    awaiter.GetResult();
                    router.HandleInvokeVoidReturn(overhead, serializer);
                }
                catch (Exception ex)
                {
                    router.HandleInvokeException(ex, overhead, serializer);
                }
            });
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static unsafe void ModularRpcsGeneratedInvokeReceiveWithParamsOvl0Stream(
            object serviceProvider,
            object targetObject,
            global::DanielWillett.ModularRpcs.Protocol.RpcOverhead overhead,
            global::DanielWillett.ModularRpcs.Routing.IRpcRouter router,
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer serializer,
            global::System.IO.Stream stream,
            global::System.Threading.CancellationToken token)
        {
            _invokeMethodRan = true;
            @TestClass targetCasted = (@TestClass)targetObject;
            if (targetCasted == null)
            {
                throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(
                    string.Format(
                        global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInstanceNull,
                        nameof(@TestClass),
                        nameof(@TestClass.ReceiveWithParams)
                    )
                );
            }

            bool canPreCalc = serializer.CanFastReadPrimitives;

            int param0 = serializer.ReadObject<int>(stream, out _);
            string param1 = serializer.ReadObject<string>(stream, out _);

            global::System.Threading.Tasks.Task result = targetCasted.ReceiveWithParams(param0, param1);

            global::System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = result.ConfigureAwait(false).GetAwaiter();

            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                    router.HandleInvokeVoidReturn(overhead, serializer);
                }
                catch (Exception ex)
                {
                    router.HandleInvokeException(ex, overhead, serializer);
                }
                return;
            }

            awaiter.UnsafeOnCompleted(() =>
            {
                try
                {
                    awaiter.GetResult();
                    router.HandleInvokeVoidReturn(overhead, serializer);
                }
                catch (Exception ex)
                {
                    router.HandleInvokeException(ex, overhead, serializer);
                }
            });
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo _modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0
            = global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo.FromCallMethod(
                global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance,
                global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<global::System.Action<@TestClass>>(
                    @TestClass => @TestClass.@InvokeFromClient()
                ),
                false
            );

        [global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CallerInfoFieldName(nameof(_modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0))]
        public partial global::DanielWillett.ModularRpcs.Async.RpcTask @InvokeFromClient()
        {
            uint size = 0;
            uint overheadSize = this._modularRpcsGeneratedProxyContext.Router.GetOverheadSize(
                @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0.MethodHandle,
                ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0
            );

            uint preOverheadSize = overheadSize;

            // ID
            ++overheadSize;

            size += overheadSize;

            unsafe
            {
                if (size > global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance.MaxSizeForStackalloc)
                {
                    byte[] lclByteBuffer = new byte[size];

                    fixed (byte* ptr = lclByteBuffer)
                    {
                        // no ID
                        ptr[preOverheadSize] = 0;

                        // uint index = 1;

                        return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(null,
                            this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                            @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0.MethodHandle,
                            default,
                            ptr,
                            (int)size,
                            size - overheadSize,
                            ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0);
                    }
                }
                else
                {
                    byte* ptr = stackalloc byte[(int)size];
                    // no ID
                    ptr[preOverheadSize] = 0;

                    // uint index = 1;

                    return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(null,
                        this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                        @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0.MethodHandle,
                        default,
                        ptr,
                        (int)size,
                        size - overheadSize,
                        ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromClientOvl0);
                }
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static RpcCallMethodInfo _modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0 = RpcCallMethodInfo.FromCallMethod(
            global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance,
            global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<global::System.Action<@TestClass>>(
                @TestClass => @TestClass.@InvokeFromServer(null)
            ),
            false
        );

        [global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CallerInfoFieldName(nameof(_modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0))]
        public partial global::DanielWillett.ModularRpcs.Async.RpcTask @InvokeFromServer(
            global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection connection)
        {
            uint size = 0;
            uint overheadSize = this._modularRpcsGeneratedProxyContext.Router.GetOverheadSize(
                @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0.MethodHandle,
                ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0
            );

            uint preOverheadSize = overheadSize;

            // ID
            ++overheadSize;

            size += overheadSize;

            unsafe
            {
                if (size > global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance.MaxSizeForStackalloc)
                {
                    byte[] lclByteBuffer = new byte[size];

                    fixed (byte* ptr = lclByteBuffer)
                    {
                        // no ID
                        ptr[preOverheadSize] = 0;

                        // uint index = 1;

                        return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(connection,
                            this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                            @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0.MethodHandle,
                            default,
                            ptr,
                            (int)size,
                            size - overheadSize,
                            ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0);
                    }
                }
                else
                {
                    byte* ptr = stackalloc byte[(int)size];
                    // no ID
                    ptr[preOverheadSize] = 0;

                    // uint index = 1;

                    return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(connection,
                        this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                        @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0.MethodHandle,
                        default,
                        ptr,
                        (int)size,
                        size - overheadSize,
                        ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeFromServerOvl0);
                }
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo _modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0
            = global::DanielWillett.ModularRpcs.Reflection.RpcCallMethodInfo.FromCallMethod(
                global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance,
                global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<global::System.Action<@TestClass>>(
                    @TestClass => @TestClass.@InvokeWithParamsFromClient(0, null)
                ),
                false
            );

        [global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CallerInfoFieldName(nameof(_modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0))]
        public partial global::DanielWillett.ModularRpcs.Async.RpcTask @InvokeWithParamsFromClient(int primitiveLikeValue, string nonPrimitiveLikeValue)
        {
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer modularRpcsGeneratedSerializer = this._modularRpcsGeneratedProxyContext.DefaultSerializer;

            bool modularRpcsGeneratedCanPreCalc = modularRpcsGeneratedSerializer.CanFastReadPrimitives;

            int modularRpcsGeneratedSize = sizeof(int) * (modularRpcsGeneratedCanPreCalc ? 1 : 0);

            if (!modularRpcsGeneratedCanPreCalc)
            {
                modularRpcsGeneratedSize += modularRpcsGeneratedSerializer.GetSize<int>(primitiveLikeValue);
            }

            modularRpcsGeneratedSize += modularRpcsGeneratedSerializer.GetSize(nonPrimitiveLikeValue);

            uint modularRpcsGeneratedOverheadSize = this._modularRpcsGeneratedProxyContext.Router.GetOverheadSize(
                @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0.MethodHandle,
                ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0
            );

            uint modularRpcsGeneratedPreOverheadSize = modularRpcsGeneratedOverheadSize;

            // ID
            ++modularRpcsGeneratedOverheadSize;

            modularRpcsGeneratedSize += (int)modularRpcsGeneratedOverheadSize;

            unsafe
            {
                if (modularRpcsGeneratedSize > global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance.MaxSizeForStackalloc)
                {
                    fixed (byte* modularRpcsGeneratedPtr = new byte[modularRpcsGeneratedSize])
                    {
                        // will be implemented in src generator.
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    byte* modularRpcsGeneratedPtr = stackalloc byte[modularRpcsGeneratedSize];

                    modularRpcsGeneratedPtr[modularRpcsGeneratedPreOverheadSize] = 0;

                    uint modularRpcsGeneratedIndex = modularRpcsGeneratedOverheadSize;

                    if (modularRpcsGeneratedCanPreCalc)
                    {
                        byte* target = modularRpcsGeneratedPtr + modularRpcsGeneratedIndex;
                        if ((nint)target % sizeof(int) == 0)
                            *(int*)target = primitiveLikeValue;
                        else
                            global::System.Runtime.CompilerServices.Unsafe.WriteUnaligned(target, primitiveLikeValue);
                        modularRpcsGeneratedIndex += sizeof(int);
                    }
                    else
                    {
                        modularRpcsGeneratedIndex += (uint)modularRpcsGeneratedSerializer.WriteObject(
                            primitiveLikeValue,
                            modularRpcsGeneratedPtr + modularRpcsGeneratedIndex,
                            (uint)modularRpcsGeneratedSize// - modularRpcsGeneratedIndex
                        );
                    }

                    modularRpcsGeneratedIndex += (uint)modularRpcsGeneratedSerializer.WriteObject(
                        nonPrimitiveLikeValue,
                        modularRpcsGeneratedPtr + modularRpcsGeneratedIndex,
                        (uint)modularRpcsGeneratedSize - modularRpcsGeneratedIndex
                    );

                    return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(null,
                        this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                        @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0.MethodHandle,
                        default,
                        modularRpcsGeneratedPtr,
                        (int)modularRpcsGeneratedIndex,
                        modularRpcsGeneratedIndex - modularRpcsGeneratedOverheadSize,
                        ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0);
                }
            }
        }


        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        private static RpcCallMethodInfo _modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromServerOvl0 = RpcCallMethodInfo.FromCallMethod(
            global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance,
            global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<global::System.Action<@TestClass>>(
                @TestClass => @TestClass.@InvokeWithParamsFromServer(0, null, null)
            ),
            false
        );

        [global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.CallerInfoFieldName(nameof(_modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromServerOvl0))]
        public partial global::DanielWillett.ModularRpcs.Async.RpcTask @InvokeWithParamsFromServer(int primitiveLikeValue, string nonPrimitiveLikeValue,
            global::DanielWillett.ModularRpcs.Abstractions.IModularRpcRemoteConnection connection)
        {
            global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer modularRpcsGeneratedSerializer = _modularRpcsGeneratedProxyContext.DefaultSerializer;

            bool modularRpcsGeneratedCanPreCalc = modularRpcsGeneratedSerializer.CanFastReadPrimitives;

            int modularRpcsGeneratedSize = sizeof(int) * (modularRpcsGeneratedCanPreCalc ? 1 : 0);

            if (!modularRpcsGeneratedCanPreCalc)
            {
                modularRpcsGeneratedSize += modularRpcsGeneratedSerializer.GetSize<int>(primitiveLikeValue);
            }

            modularRpcsGeneratedSize += modularRpcsGeneratedSerializer.GetSize(nonPrimitiveLikeValue);

            uint modularRpcsGeneratedOverheadSize = this._modularRpcsGeneratedProxyContext.Router.GetOverheadSize(
                @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0.MethodHandle,
                ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0
            );

            uint modularRpcsGeneratedPreOverheadSize = modularRpcsGeneratedOverheadSize;

            // ID
            ++modularRpcsGeneratedOverheadSize;

            modularRpcsGeneratedSize += (int)modularRpcsGeneratedOverheadSize;

            unsafe
            {
                if (modularRpcsGeneratedSize > global::DanielWillett.ModularRpcs.Reflection.ProxyGenerator.Instance.MaxSizeForStackalloc)
                {
                    fixed (byte* modularRpcsGeneratedPtr = new byte[modularRpcsGeneratedSize])
                    {
                        // will be implemented in src generator.
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    byte* modularRpcsGeneratedPtr = stackalloc byte[modularRpcsGeneratedSize];

                    modularRpcsGeneratedPtr[modularRpcsGeneratedPreOverheadSize] = 0;

                    uint modularRpcsGeneratedIndex = modularRpcsGeneratedOverheadSize;

                    if (modularRpcsGeneratedCanPreCalc)
                    {
                        byte* target = modularRpcsGeneratedPtr + modularRpcsGeneratedIndex;
                        if ((nint)target % sizeof(int) == 0)
                            *(int*)target = primitiveLikeValue;
                        else
                            global::System.Runtime.CompilerServices.Unsafe.WriteUnaligned(target, primitiveLikeValue);
                        modularRpcsGeneratedIndex += sizeof(int);
                    }
                    else
                    {
                        modularRpcsGeneratedIndex += (uint)modularRpcsGeneratedSerializer.WriteObject(
                            primitiveLikeValue,
                            modularRpcsGeneratedPtr + modularRpcsGeneratedIndex,
                            (uint)modularRpcsGeneratedSize// - modularRpcsGeneratedIndex
                        );
                    }

                    modularRpcsGeneratedIndex += (uint)modularRpcsGeneratedSerializer.WriteObject(
                        nonPrimitiveLikeValue,
                        modularRpcsGeneratedPtr + modularRpcsGeneratedIndex,
                        (uint)modularRpcsGeneratedSize - modularRpcsGeneratedIndex
                    );

                    return this._modularRpcsGeneratedProxyContext.Router.InvokeRpc(connection,
                        this._modularRpcsGeneratedProxyContext.DefaultSerializer,
                        @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0.MethodHandle,
                        default,
                        modularRpcsGeneratedPtr,
                        (int)modularRpcsGeneratedIndex,
                        modularRpcsGeneratedIndex - modularRpcsGeneratedOverheadSize,
                        ref @TestClass._modularRpcsGeneratedCallMethodInfoInvokeWithParamsFromClientOvl0);
                }
            }
        }
    }
}