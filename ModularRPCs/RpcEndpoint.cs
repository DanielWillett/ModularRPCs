using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.ModularRpcs.Serialization;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs;

/// <summary>
/// Represents either a send or receive method.
/// </summary>
public class RpcEndpoint : IRpcInvocationPoint
{
    private uint _sizeWithoutIdentifier;
    protected bool IgnoreSignatureHash;
    protected int SignatureHash;

    /// <summary>
    /// Unique ID corresponding to this endpoint set by the server.
    /// </summary>
    public uint? EndpointId { get; set; }

    /// <summary>
    /// Is the invocation point expecting a target object?
    /// </summary>
    public bool IsStatic { get; }

    /// <summary>
    /// Is the invocation point a broadcast, meaning it should be looking for a receive listening for the given information instead of looking for the method at the given information?
    /// </summary>
    public bool IsBroadcast { get; }

    /// <summary>
    /// Do <see cref="ParameterTypes"/> or <see cref="ParameterTypeNames"/> only include binded parameters. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Binded parameters means non-injected parameters, meaning only parameters that are replicated.
    /// Other injected parameters such as <see cref="CancellationToken"/>, <see cref="IRpcRouter"/>, etc, along with any other parameters decorated with the <see cref="RpcInjectAttribute"/>, will not be checked as part of the parameter type arrays.
    /// </remarks>
    public bool ParametersAreBindedParametersOnly { get; }

    /// <summary>
    /// The identifier, if any, to identify the instance with.
    /// </summary>
    public object? Identifier { get; }

    /// <summary>
    /// The fully declared name of the declaring type.
    /// </summary>
    public string DeclaringTypeName { get; }

    /// <summary>
    /// The method name of the declaring type.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The fully declared names of the argument types.
    /// </summary>
    public string[]? ParameterTypeNames { get; }

    /// <summary>
    /// The argument types of the method, if available.
    /// </summary>
    public Type?[]? ParameterTypes { get; }

    /// <summary>
    /// The method, if available.
    /// </summary>
    public MethodInfo? Method { get; }
    
    /// <summary>
    /// The declaring type, if available.
    /// </summary>
    public Type? DeclaringType { get; }

    /// <summary>
    /// Size in bytes of this endpoint if it were written to a buffer.
    /// </summary>
    public uint Size { get; private set; }
    bool IRpcInvocationPoint.CanCache => true;
    protected RpcEndpoint(IRpcSerializer serializer, RpcEndpoint other, object? identifier)
    {
        if (identifier is DBNull)
            identifier = null;

        Identifier = identifier;

        EndpointId = other.EndpointId;
        IsStatic = other.IsStatic;
        IsBroadcast = other.IsBroadcast;
        DeclaringTypeName = other.DeclaringTypeName;
        MethodName = other.MethodName;
        ParameterTypeNames = other.ParameterTypeNames;
        ParameterTypes = other.ParameterTypes;
        Method = other.Method;
        DeclaringType = other.DeclaringType;
        Size = _sizeWithoutIdentifier = other._sizeWithoutIdentifier;
        ParametersAreBindedParametersOnly = other.ParametersAreBindedParametersOnly;
        SignatureHash = other.SignatureHash;
        if (identifier != null)
        {
            Size += CalculateIdentifierSize(serializer, identifier);
        }
    }
    internal RpcEndpoint(uint knownId, string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash)
        : this(declaringTypeName, methodName, parameterTypeNames, argsAreBindOnly, isBroadcast, signatureHash, ignoreSignatureHash)
    {
        EndpointId = knownId;
    }
    internal RpcEndpoint(string declaringTypeName, string methodName, string[]? parameterTypeNames, bool argsAreBindOnly, bool isBroadcast, int signatureHash, bool ignoreSignatureHash)
    {
        IsStatic = false;
        DeclaringTypeName = declaringTypeName;
        DeclaringType = Type.GetType(DeclaringTypeName, false, false);
        MethodName = methodName;
        SignatureHash = signatureHash;
        IgnoreSignatureHash = ignoreSignatureHash;
        ParametersAreBindedParametersOnly = argsAreBindOnly;
        ParameterTypeNames = parameterTypeNames;
        IsBroadcast = isBroadcast;

        if (TypeUtility.TryResolveMethod(null, methodName, null, declaringTypeName, null, parameterTypeNames, argsAreBindOnly, out MethodInfo? foundMethod, out _))
        {
            Method = foundMethod;
            IsStatic = Method.IsStatic;
        }

        CalculateSize();
    }

    [Pure]
    public override string ToString()
    {
        if (Method != null)
            return Accessor.ExceptionFormatter.Format(Method);

        MethodDefinition def = new MethodDefinition(MethodName ?? string.Empty);
        if (DeclaringType != null)
            def.DeclaredIn(DeclaringType, IsStatic);
        else
            def.DeclaredIn(DeclaringTypeName, IsStatic);
        
        if (ParameterTypes != null)
        {
            for (int i = 0; i < ParameterTypes.Length; ++i)
                def.WithParameter(ParameterTypes[i]!, "arg" + i.ToString(CultureInfo.InvariantCulture));
        }
        else if (ParameterTypeNames != null)
        {
            for (int i = 0; i < ParameterTypeNames.Length; ++i)
                def.WithParameter(ParameterTypeNames[i], "arg" + i.ToString(CultureInfo.InvariantCulture));
        }

        return (IsBroadcast ? "(Broadcast) " : string.Empty) + Accessor.ExceptionFormatter.Format(def);
    }

    private protected virtual unsafe object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerBytes handlerBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, byte* bytes, uint maxSize, CancellationToken token)
    {
        return handlerBytes(null, targetObject, overhead, router, serializer, bytes, maxSize, token);
    }
    private protected virtual object? InvokeInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        return handlerStream(null, targetObject, overhead, router, serializer, stream, token);
    }
    private protected virtual object? InvokeRawInvokeMethod(ProxyGenerator.RpcInvokeHandlerRawBytes handlerRawBytes, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token)
    {
        return handlerRawBytes(null, targetObject, overhead, router, serializer, rawData, canTakeOwnership, token);
    }
    private protected virtual object? InvokeRawInvokeMethod(ProxyGenerator.RpcInvokeHandlerStream handlerRawStream, object? targetObject, RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token)
    {
        return handlerRawStream(null, targetObject, overhead, router, serializer, stream, token);
    }
    public virtual unsafe ValueTask Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, ReadOnlyMemory<byte> rawData, bool canTakeOwnership, CancellationToken token = default)
    {
        if (!IsBroadcast)
        {
            MethodInfo? toInvoke = Method;

            if (toInvoke == null || !toInvoke.TryGetAttributeSafe(out RpcReceiveAttribute targetAttribute))
                throw new RpcEndpointNotFoundException(this);
            
            if (!IgnoreSignatureHash && !targetAttribute.Raw)
            {
                int paramHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(toInvoke);

                if (SignatureHash != paramHash)
                {
                    throw new RpcEndpointNotFoundException(this, Properties.Exceptions.RpcEndpointNotFoundExceptionMismatchHash);
                }
            }

            object? targetObject = null;
            if (!IsStatic)
            {
                targetObject = GetTargetObject(toInvoke);
            }

            object? returnedValue;
            if (targetAttribute.Raw)
            {
                ProxyGenerator.RpcInvokeHandlerRawBytes invokeRawMethod = ProxyGenerator.Instance.GetInvokeRawBytesMethod(toInvoke);
                returnedValue = InvokeRawInvokeMethod(invokeRawMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, rawData, canTakeOwnership, token);
            }
            else
            {
                ProxyGenerator.RpcInvokeHandlerBytes invokeMethod = ProxyGenerator.Instance.GetInvokeBytesMethod(toInvoke);

                fixed (byte* ptr = rawData.Span)
                {
                    returnedValue = InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, ptr, (uint)rawData.Length, token);
                }
            }

            return ConvertReturnedValueToValueTask(returnedValue);
        }

        bool any = false;
        List<Exception>? exceptions = null;
        object? firstRtnValue = null;
        fixed (byte* ptr = rawData.Span)
        {
            foreach (MethodInfo method in FindBroadcastListeners(router))
            {
                if (!method.TryGetAttributeSafe(out RpcReceiveAttribute targetAttribute))
                {
                    continue;
                }

                if (!IgnoreSignatureHash && !targetAttribute.Raw)
                {
                    int paramHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(method);

                    if (SignatureHash != paramHash)
                    {
                        continue;
                    }
                }

                any = true;

                try
                {
                    object? targetObject = null;
                    if (!method.IsStatic)
                    {
                        targetObject = GetTargetObject(method);
                    }

                    if (targetAttribute.Raw)
                    {
                        ProxyGenerator.RpcInvokeHandlerRawBytes invokeRawMethod = ProxyGenerator.Instance.GetInvokeRawBytesMethod(method);
                        firstRtnValue ??= InvokeRawInvokeMethod(invokeRawMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, rawData, canTakeOwnership, token);
                    }
                    else
                    {
                        ProxyGenerator.RpcInvokeHandlerBytes invokeMethod = ProxyGenerator.Instance.GetInvokeBytesMethod(method);
                        firstRtnValue ??= InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, ptr, (uint)rawData.Length, token);
                    }

                }
                catch (Exception ex)
                {
                    (exceptions ??= [ ]).Add(ex);
                }
            }
        }

        if (!any)
            throw new RpcEndpointNotFoundException(this);

        if (exceptions != null)
        {
            if (exceptions.Count == 1)
                throw exceptions[0];

            throw new AggregateException(exceptions);
        }

        return firstRtnValue == null ? default : ConvertReturnedValueToValueTask(firstRtnValue);
    }
    public virtual ValueTask Invoke(RpcOverhead overhead, IRpcRouter router, IRpcSerializer serializer, Stream stream, CancellationToken token = default)
    {
        if (!IsBroadcast)
        {
            MethodInfo? toInvoke = Method;
            if (toInvoke == null || !toInvoke.TryGetAttributeSafe(out RpcReceiveAttribute targetAttribute))
                throw new RpcEndpointNotFoundException(this);

            if (!IgnoreSignatureHash && !targetAttribute.Raw)
            {
                int paramHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(toInvoke);

                if (SignatureHash != paramHash)
                {
                    throw new RpcEndpointNotFoundException(this, Properties.Exceptions.RpcEndpointNotFoundExceptionMismatchHash);
                }
            }

            object? targetObject = null;
            if (!IsStatic)
            {
                targetObject = GetTargetObject(toInvoke);
            }

            object? returnedValue;
            if (targetAttribute.Raw)
            {
                ProxyGenerator.RpcInvokeHandlerStream invokeRawMethod = ProxyGenerator.Instance.GetInvokeRawStreamMethod(toInvoke);
                returnedValue = InvokeRawInvokeMethod(invokeRawMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, stream, token);
            }
            else
            {
                ProxyGenerator.RpcInvokeHandlerStream invokeMethod = ProxyGenerator.Instance.GetInvokeStreamMethod(toInvoke);
                returnedValue = InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, stream, token);
            }

            return ConvertReturnedValueToValueTask(returnedValue);
        }


        bool any = false;
        List<Exception>? exceptions = null;
        object? firstRtnValue = null;
        foreach (MethodInfo method in FindBroadcastListeners(router))
        {
            if (!method.TryGetAttributeSafe(out RpcReceiveAttribute targetAttribute))
            {
                continue;
            }

            if (!IgnoreSignatureHash && !targetAttribute.Raw)
            {
                int paramHash = ProxyGenerator.Instance.SerializerGenerator.GetBindingMethodSignatureHash(method);

                if (SignatureHash != paramHash)
                {
                    continue;
                }
            }

            any = true;

            try
            {
                object? targetObject = null;
                if (!method.IsStatic)
                {
                    targetObject = GetTargetObject(method);
                }

                if (targetAttribute.Raw)
                {
                    ProxyGenerator.RpcInvokeHandlerStream invokeRawMethod = ProxyGenerator.Instance.GetInvokeRawStreamMethod(method);
                    firstRtnValue ??= InvokeRawInvokeMethod(invokeRawMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, stream, token);
                }
                else
                {
                    ProxyGenerator.RpcInvokeHandlerStream invokeMethod = ProxyGenerator.Instance.GetInvokeStreamMethod(method);
                    firstRtnValue ??= InvokeInvokeMethod(invokeMethod, targetObject, overhead, overhead.ReceivingConnection.Router, serializer, stream, token);
                }

            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (!any)
            throw new RpcEndpointNotFoundException(this);

        if (exceptions != null)
            throw new AggregateException(exceptions);

        return firstRtnValue == null ? default : ConvertReturnedValueToValueTask(firstRtnValue);
    }
    protected virtual ValueTask ConvertReturnedValueToValueTask(object? returnedValue)
    {
        return ProxyGenerator.Instance.ConvertReturnedValueToValueTask(returnedValue);
    }
    protected virtual object? GetTargetObject(MethodInfo? knownMethod)
    {
        Type? declType = knownMethod?.DeclaringType ?? DeclaringType;
        bool isStatic = knownMethod == null ? IsStatic : knownMethod.IsStatic;

        if (Identifier == null || isStatic)
            return null;

        if (declType == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierDeclaringTypeNotFound) { ErrorCode = 4 };

        WeakReference? weakRef = ProxyGenerator.Instance.GetObjectByIdentifier(declType, Identifier);
        object? target;

        try
        {
            target = weakRef?.Target;
        }
        catch (InvalidOperationException)
        {
            target = null;
        }

        if (target == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionIdentifierNotExists) { ErrorCode = 5 };

        return target;
    }

    private void CalculateSize()
    {
        uint size = sizeof(uint) + sizeof(int) + sizeof(ushort) + sizeof(ushort) + 1;
        size += Math.Min((uint)Encoding.UTF8.GetByteCount(DeclaringTypeName), ushort.MaxValue)
                + Math.Min((uint)Encoding.UTF8.GetByteCount(MethodName), ushort.MaxValue);
        if (ParameterTypeNames != null)
        {
            size += sizeof(ushort);
            int c = Math.Min(ParameterTypeNames.Length, ushort.MaxValue);
            for (int i = 0; i < c; ++i)
                size += Math.Min((uint)Encoding.UTF8.GetByteCount(ParameterTypeNames[i]), ushort.MaxValue);
        }

        _sizeWithoutIdentifier = size;
        Size = size;
    }

    internal unsafe int WriteToBytes(IRpcSerializer serializer, IRpcRouter router, byte* bytes, uint maxCt)
    {
        if (maxCt < 13)
            throw new RpcOverflowException(Properties.Exceptions.RpcOverflowException) { ErrorCode = 1 };

        EndpointFlags flags = ParametersAreBindedParametersOnly ? EndpointFlags.ArgsAreBindOnly : 0;
        flags |= (EndpointFlags)((ParameterTypeNames != null ? 1 : 0) * (int)EndpointFlags.DefinesParameters);
        flags |= (EndpointFlags)((IsBroadcast ? 1 : 0) * (int)EndpointFlags.Broadcast);

        *bytes = (byte)flags;
        ++bytes;
        
        uint enpId = EndpointId.GetValueOrDefault();
        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, enpId);
        }
        else
        {
            bytes[3] = unchecked( (byte) enpId );
            bytes[2] = unchecked( (byte)(enpId >>> 8)  );
            bytes[1] = unchecked( (byte)(enpId >>> 16) );
            *bytes   = unchecked( (byte)(enpId >>> 24) );
        }

        bytes += 4;

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, SignatureHash);
        }
        else
        {
            bytes[3] = unchecked( (byte) SignatureHash );
            bytes[2] = unchecked( (byte)(SignatureHash >>> 8)  );
            bytes[1] = unchecked( (byte)(SignatureHash >>> 16) );
            *bytes   = unchecked( (byte)(SignatureHash >>> 24) );
        }

        bytes += 4;

        int typeNameLenCt = Encoding.UTF8.GetByteCount(DeclaringTypeName);
        int methodNameLenCt = Encoding.UTF8.GetByteCount(MethodName);

        if (typeNameLenCt > ushort.MaxValue)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionTypeNameTooLong, MethodName)) { ErrorCode = 2 };

        if (methodNameLenCt > ushort.MaxValue)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionMethodNameTooLong, DeclaringTypeName)) { ErrorCode = 3 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, (ushort)typeNameLenCt);
        }
        else
        {
            bytes[1] = unchecked( (byte) (ushort)typeNameLenCt );
            *bytes   = unchecked( (byte)((ushort)typeNameLenCt >>> 8)  );
        }

        bytes += 2;

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, (ushort)methodNameLenCt);
        }
        else
        {
            bytes[1] = unchecked( (byte) (ushort)methodNameLenCt );
            *bytes   = unchecked( (byte)((ushort)methodNameLenCt >>> 8)  );
        }

        bytes += 2;
        int size = 13 + typeNameLenCt + methodNameLenCt;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        fixed (char* ptr = DeclaringTypeName)
            Encoding.UTF8.GetBytes(ptr, DeclaringTypeName.Length, bytes, typeNameLenCt);
        bytes += typeNameLenCt;

        fixed (char* ptr = MethodName)
            Encoding.UTF8.GetBytes(ptr, MethodName.Length, bytes, methodNameLenCt);
        bytes += methodNameLenCt;

        if ((flags & EndpointFlags.DefinesParameters) == 0)
            return size;

        size += sizeof(ushort);

        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        int argCt = ParameterTypeNames!.Length;
        if (argCt > ushort.MaxValue)
            throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionTooManyArguments, DeclaringTypeName, MethodName)) { ErrorCode = 4 };

        size += argCt * 2;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, (ushort)argCt);
        }
        else
        {
            bytes[1] = unchecked( (byte) (ushort)argCt );
            *bytes   = unchecked( (byte)((ushort)argCt >>> 8) );
        }

        bytes += 2;

        Span<ushort> argLens = stackalloc ushort[argCt];

        int ttlLen = 0;
        for (int i = 0; i < argCt; ++i)
        {
            int utf8Len = Encoding.UTF8.GetByteCount(ParameterTypeNames[i]);
            if (argCt > ushort.MaxValue)
                throw new RpcOverflowException(string.Format(Properties.Exceptions.RpcOverflowExceptionParameterTypeNameTooLong, DeclaringTypeName, MethodName)) { ErrorCode = 5 };

            argLens[i] = (ushort)utf8Len;
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(bytes, (ushort)utf8Len);
            }
            else
            {
                bytes[1] = unchecked( (byte) (ushort)utf8Len );
                *bytes   = unchecked( (byte)((ushort)utf8Len >>> 8) );
            }

            bytes += 2;

            ttlLen += utf8Len;
        }

        size += ttlLen;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        for (int i = 0; i < argCt; ++i)
        {
            string typeName = ParameterTypeNames[i];
            fixed (char* ptr = typeName)
                Encoding.UTF8.GetBytes(ptr, typeName.Length, bytes, argLens[i]);
            bytes += argLens[i];
        }

        return size;
    }

    [Pure]
    internal static unsafe IRpcInvocationPoint ReadFromBytes(IRpcSerializer serializer, IRpcRouter router, byte* bytes, uint maxCt, out int bytesRead)
    {
        if (maxCt < 13)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        byte* originalPtr = bytes;
        EndpointFlags flags1 = (EndpointFlags)(*bytes);
        ++bytes;

        uint knownRpcShortcutId = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<uint>(bytes)
            : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        bytes += sizeof(uint);

        int signatureHash = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<int>(bytes)
            : *bytes << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];

        bytes += sizeof(int);

        int rpcTypeLength = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(bytes)
            : (ushort)(*bytes << 8 | bytes[1]);

        bytes += sizeof(ushort);

        int rpcMethodLength = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(bytes)
            : (ushort)(*bytes << 8 | bytes[1]);

        bytes += sizeof(ushort);
        int size = 13 + rpcTypeLength + rpcMethodLength;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        string typeName = Encoding.UTF8.GetString(bytes, rpcTypeLength);
        bytes += rpcTypeLength;
        string methodName = Encoding.UTF8.GetString(bytes, rpcMethodLength);
        bytes += rpcMethodLength;

        string[] args;
        if ((flags1 & EndpointFlags.DefinesParameters) != 0)
        {
            size += sizeof(ushort);

            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            int argCt = BitConverter.IsLittleEndian
                ? Unsafe.ReadUnaligned<ushort>(bytes)
                : (ushort)(*bytes << 8 | bytes[1]);
            bytes += 2;
            size += argCt * 2;

            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            args = new string[argCt];
            Span<ushort> argLens = stackalloc ushort[argCt];
            int ttlLen = 0;
            for (int i = 0; i < argCt; ++i)
            {
                ushort len = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(bytes)
                    : (ushort)(*bytes << 8 | bytes[1]);
                argLens[i] = len;
                ttlLen += len;
                bytes += sizeof(ushort);
            }

            size += ttlLen;
            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

            for (int i = 0; i < argCt; ++i)
            {
                args[i] = Encoding.UTF8.GetString(bytes, argLens[i]);
                bytes += argLens[i];
            }
        }
        else args = null!;

        object? identifier = ReadIdentifierFromBytes(serializer, bytes, maxCt - (uint)size, out int bytesReadIdentifier);
        // size += bytesReadIdentifier;
        bytes += bytesReadIdentifier;

        bytesRead = checked( (int)(bytes - originalPtr) );
        return router.ResolveEndpoint(serializer, knownRpcShortcutId, typeName, methodName, args, (flags1 & EndpointFlags.ArgsAreBindOnly) != 0, (flags1 & EndpointFlags.Broadcast) != 0, signatureHash, (flags1 & EndpointFlags.IgnoreSignatureHash) != 0, bytesRead, identifier);
    }

    [Pure]
    internal static unsafe IRpcInvocationPoint ReadFromStream(IRpcSerializer serializer, IRpcRouter router, Stream stream, out int bytesRead)
    {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byte[] bytes = new byte[64];

        int byteCt = stream.Read(bytes, 0, 13);
#else
        Span<byte> bytes = stackalloc byte[64];

        int byteCt = stream.Read(bytes[..13]);
#endif
        if (byteCt < 13)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

        EndpointFlags flags1 = (EndpointFlags)bytes[0];

        uint knownRpcShortcutId = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<uint>(ref bytes[1])
            : (uint)bytes[1] << 24 | (uint)bytes[2] << 16 | (uint)bytes[3] << 8 | bytes[4];

        int index = sizeof(uint) + 1;

        int signatureHash = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<int>(ref bytes[index])
            : bytes[index] << 24 | bytes[index + 1] << 16 | bytes[index + 2] << 8 | bytes[index + 3];

        index += sizeof(int);

        int rpcTypeLength = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(ref bytes[index])
            : (ushort)(bytes[index] << 8 | bytes[index + 1]);

        index += sizeof(ushort);

        int rpcMethodLength = BitConverter.IsLittleEndian
            ? Unsafe.ReadUnaligned<ushort>(ref bytes[index])
            : (ushort)(bytes[index] << 8 | bytes[index + 1]);

        index += sizeof(ushort);

        int maxSize = Math.Max(rpcTypeLength, rpcMethodLength);
        if (maxSize > 64)
        {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            bytes = new byte[maxSize];
#else
            bytes = stackalloc byte[maxSize];
#endif
        }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byteCt = stream.Read(bytes, 0, rpcTypeLength);
#else
        byteCt = stream.Read(bytes[..rpcTypeLength]);
#endif

        if (byteCt < rpcTypeLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        string typeName = Encoding.UTF8.GetString(bytes, 0, rpcTypeLength);
#else
        string typeName = Encoding.UTF8.GetString(bytes[..rpcTypeLength]);
#endif
        index += rpcTypeLength;

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        byteCt = stream.Read(bytes, 0, rpcMethodLength);
#else
        byteCt = stream.Read(bytes[..rpcMethodLength]);
#endif

        if (byteCt < rpcMethodLength)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
        string methodName = Encoding.UTF8.GetString(bytes, 0, rpcMethodLength);
#else
        string methodName = Encoding.UTF8.GetString(bytes[..rpcMethodLength]);
#endif
        index += rpcMethodLength;

        string[] args;
        if ((flags1 & EndpointFlags.DefinesParameters) != 0)
        {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, 2);
#else
            byteCt = stream.Read(bytes[..2]);
#endif

            if (byteCt < 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            int argCt = BitConverter.IsLittleEndian
                ? Unsafe.ReadUnaligned<ushort>(ref bytes[0])
                : (ushort)(bytes[0] << 8 | bytes[1]);
            args = new string[argCt];
            Span<int> argLens = stackalloc int[argCt];
            int ttlLen = 0;
            if (argCt * 2 > bytes.Length)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[argCt * 2];
#else
                bytes = argCt > 256 ? new byte[argCt * 2] : stackalloc byte[argCt * 2];
#endif
            }
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, argCt * 2);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            for (int i = 0; i < argCt; ++i)
            {
                int len = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref bytes[i * argCt])
                    : (ushort)(bytes[i * argCt] << 8 | bytes[i * argCt + 1]);
                index += sizeof(ushort);
                argLens[i] = len;
                ttlLen += len;
                index += len;
            }

            if (ttlLen > bytes.Length)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                bytes = new byte[ttlLen];
#else
                bytes = ttlLen > 512 ? new byte[ttlLen] : stackalloc byte[ttlLen];
#endif
            }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byteCt = stream.Read(bytes, 0, ttlLen);
#else
            byteCt = stream.Read(bytes[..(argCt * 2)]);
#endif

            if (byteCt < argCt * 2)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            int pos = 0;
            for (int i = 0; i < argCt; ++i)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                args[i] = Encoding.UTF8.GetString(bytes, pos, argLens[i]);
#else
                args[i] = Encoding.UTF8.GetString(bytes.Slice(pos, argLens[i]));
#endif
                pos += argLens[i];
            }
        }
        else args = null!;

        object? identifier = ReadIdentifierFromStream(serializer, stream, out int bytesReadIdentifier);
        index += bytesReadIdentifier;

        bytesRead = index;
        return router.ResolveEndpoint(serializer, knownRpcShortcutId, typeName, methodName, args, (flags1 & EndpointFlags.ArgsAreBindOnly) != 0, (flags1 & EndpointFlags.Broadcast) != 0, signatureHash, (flags1 & EndpointFlags.IgnoreSignatureHash) != 0, bytesRead, identifier);
    }

    [Pure]
    public virtual IRpcInvocationPoint CloneWithIdentifier(IRpcSerializer serializer, object? identifier)
    {
        return new RpcEndpoint(serializer, this, identifier);
    }

    [Pure]
    internal static uint CalculateIdentifierSize(IRpcSerializer serializer, object identifier)
    {
        Type idType = identifier.GetType();
        TypeCode tc = TypeUtility.GetTypeCode(idType);
        if (tc == TypeCode.Object)
        {
            uint size = 2 + (uint)serializer.GetSize(identifier);
            if (serializer.TryGetKnownTypeId(idType, out _))
                return 4 + size;

            return size + (uint)serializer.GetSize(TypeUtility.GetAssemblyQualifiedNameNoVersion(idType));
        }

        if (tc == TypeCode.DBNull)
            return 1;

        if (tc != TypeCode.String && serializer.CanFastReadPrimitives)
            return 1 + (uint)TypeUtility.GetTypeCodeSize(tc);

        return 1 + (uint)serializer.GetSize(identifier);
    }

    [Pure]
    internal static unsafe object? ReadIdentifierFromBytes(IRpcSerializer serializer, byte* bytes, uint maxCt, out int bytesRead)
    {
        int size = 1;
        if (maxCt < size)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

        TypeCode typeCode = (TypeCode)(*bytes);
        if (typeCode == 0)
        {
            bytesRead = 1;
            return null;
        }

        ++bytes;
        if (typeCode != TypeCode.Object)
        {
            if (typeCode > TypeUtility.MaxUsedTypeCode)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };

            if (typeCode == TypeCode.String)
            {
                string? str = serializer.ReadObject<string>(bytes, maxCt - (uint)size, out int strBytesRead);
                bytesRead = size + strBytesRead;
                return str;
            }

            if (typeCode == TypeCode.DBNull)
            {
                bytesRead = 1;
                return DBNull.Value;
            }

            object identifier;
            if (serializer.CanFastReadPrimitives)
            {
                size += TypeUtility.GetTypeCodeSize(typeCode);
                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                uint index = 0;
                identifier = TypeUtility.ReadTypeCode(typeCode, serializer, bytes, (int)maxCt - size, ref index, out _)
                             ?? throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };
            }
            else
            {
                Type type = TypeUtility.GetType(typeCode);
                object? obj = serializer.ReadObject(type, bytes, maxCt - (uint)size, out int identBytesRead);
                bytesRead = size + identBytesRead;
                return obj;
            }

            bytesRead = size;
            return identifier;
        }
        else
        {
            ++size;
            if (maxCt < size)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };
            IdentifierFlags flags = (IdentifierFlags)(*bytes);
            ++bytes;
            uint? knownTypeId = null;
            string? typeName = null;
            if ((flags & IdentifierFlags.IsTypeNameOnly) == 0)
            {
                size += sizeof(uint);

                if (maxCt < size)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionBufferRunOut) { ErrorCode = 1 };

                knownTypeId = BitConverter.IsLittleEndian
                        ? Unsafe.ReadUnaligned<uint>(bytes)
                        : (uint)*bytes << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

                bytes += sizeof(uint);
            }
            if ((flags & IdentifierFlags.IsKnownTypeOnly) == 0)
            {
                typeName = serializer.ReadObject<string>(bytes, maxCt - (uint)size, out int strBytesRead);
                bytes += strBytesRead;
                size += strBytesRead;
            }

            Type type = DetermineIdentifierType(serializer, typeName, knownTypeId);
            object? identifier = serializer.ReadObject(type, bytes, maxCt - (uint)size, out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }

    [Pure]
    internal static unsafe object? ReadIdentifierFromStream(IRpcSerializer serializer, Stream stream, out int bytesRead)
    {
        int size = 1;

        int b = stream.ReadByte();
        if (b == -1)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
        if (b == 0)
        {
            bytesRead = 0;
            return null;
        }

        TypeCode typeCode = (TypeCode)b;
        if (typeCode != TypeCode.Object)
        {
            if (typeCode > TypeUtility.MaxUsedTypeCode)
                throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };

            object identifier;

            if (typeCode == TypeCode.String)
            {
                string? value = serializer.ReadObject<string>(stream, out int strBytesRead);
                bytesRead = size + strBytesRead;
                return value;
            }

            if (typeCode == TypeCode.DBNull)
            {
                bytesRead = 1;
                return DBNull.Value;
            }

            if (serializer.CanFastReadPrimitives)
            {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                byte[]? bytes = null;
                try
                {
#else
                scoped Span<byte> bytes;
#endif
                int tcsz = TypeUtility.GetTypeCodeSize(typeCode);
                size += tcsz;
                if (size != 1)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    bytes = DefaultSerializer.ArrayPool.Rent(tcsz);
                    int byteCt = stream.Read(bytes, 0, tcsz);
#else
                    bytes = stackalloc byte[tcsz];
                    int byteCt = stream.Read(bytes);
#endif
                    if (byteCt < tcsz)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
                }
                else
                {
                    bytes = null!;
                    b = stream.ReadByte();
                    if (b == -1)
                        throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
                }

                fixed (byte* ptr = bytes)
                {
                    uint index = 0u;
                    identifier = TypeUtility.ReadTypeCode(typeCode, serializer, ptr, tcsz, ref index, out _)
                             ?? throw new RpcOverheadParseException(string.Format(Properties.Exceptions.RpcOverheadParseExceptionInvalidTypeCode, typeCode.ToString())) { ErrorCode = 7 };
                }
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                }
                finally
                {
                    if (bytes != null)
                        DefaultSerializer.ArrayPool.Return(bytes);
                }
#endif
            }
            else
            {
                Type type = TypeUtility.GetType(typeCode);
                object? obj = serializer.ReadObject(type, stream, out int identBytesRead);
                bytesRead = size + identBytesRead;
                return obj;
            }

            bytesRead = size;
            return identifier;
        }
        else
        {
            uint? knownTypeId = null;
            string? typeName = null;
            b = stream.ReadByte();

            if (b == -1)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            IdentifierFlags flags = (IdentifierFlags)b;
            int sz = (flags & IdentifierFlags.IsTypeNameOnly) == 0 ? sizeof(uint) : 0;
            sz += (flags & IdentifierFlags.IsKnownTypeOnly) == 0 ? sizeof(ushort) : 0;
            int arrSize = (flags & IdentifierFlags.IsTypeNameOnly) == 0 ? sz + 32 : sz;
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
            byte[] bytes = new byte[arrSize];
            int byteCt = stream.Read(bytes, 0, sz);
#else
            scoped Span<byte> bytes = stackalloc byte[arrSize];
            int byteCt = stream.Read(bytes[..sz]);
#endif
            if (byteCt < sz)
                throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };

            if ((flags & IdentifierFlags.IsTypeNameOnly) == 0)
            {
                size += sizeof(uint);

                knownTypeId = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<uint>(ref bytes[0])
                    : (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
            }
            if ((flags & IdentifierFlags.IsKnownTypeOnly) == 0)
            {
                size += sizeof(ushort);

                int strLen = BitConverter.IsLittleEndian
                    ? Unsafe.ReadUnaligned<ushort>(ref bytes[0])
                    : (ushort)(bytes[0] << 8 | bytes[1]);

                size += strLen;

                if (strLen > bytes.Length)
                {
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                    bytes = new byte[strLen];
#else
                    bytes = stackalloc byte[strLen];
#endif
                }

#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                byteCt = stream.Read(bytes, 0, strLen);
#else
                byteCt = stream.Read(bytes[..strLen]);
#endif
                if (byteCt < strLen)
                    throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionStreamRunOut) { ErrorCode = 2 };
#if NETFRAMEWORK || NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
                typeName = Encoding.UTF8.GetString(bytes, 0, strLen);
#else
                typeName = Encoding.UTF8.GetString(bytes[..strLen]);
#endif
            }

            Type type = DetermineIdentifierType(serializer, typeName, knownTypeId);
            object? identifier = serializer.ReadObject(type, stream, out bytesRead);

            bytesRead += size;
            return identifier;
        }
    }
    private IEnumerable<MethodInfo> FindBroadcastListeners(IRpcRouter router)
    {
        if (!IsBroadcast)
            throw new InvalidOperationException();

        if (!router.BroadcastTargets.TryGetValue(DeclaringTypeName, out IReadOnlyList<RpcEndpointTarget> targets))
            return Array.Empty<MethodInfo>();

        MethodInfo? match = null;
        List<MethodInfo>? matches = null;

        if (targets is RpcEndpointTarget[] targetArr)
        {
            for (int i = 0; i < targetArr.Length; ++i)
            {
                ref RpcEndpointTarget target = ref targetArr[i];
                TestTarget(in target, this, ref match, ref matches);
            }
        }
        else
        {
            foreach (RpcEndpointTarget target in targets)
            {
                TestTarget(in target, this, ref match, ref matches);
            }
        }

        if (matches is not null)
            return matches;

        return match is not null
            ? Enumerable.Repeat(match, 1)
            : Array.Empty<MethodInfo>();

        static void TestTarget(in RpcEndpointTarget target, RpcEndpoint endpoint, ref MethodInfo? match, ref List<MethodInfo>? matches)
        {
            MethodInfo mtd = target.OwnerMethodInfo!;

            if (!target.MethodName.Equals(endpoint.MethodName, StringComparison.Ordinal))
                return;

            if (target.ParameterTypes != null && (endpoint.ParameterTypeNames != null || endpoint.ParameterTypes != null))
            {
                if (!TypeUtility.ParametersMatchParameters(endpoint.ParameterTypes, endpoint.ParameterTypeNames,
                        endpoint.ParametersAreBindedParametersOnly, target.ParameterTypes,
                        target.ParameterTypesAreBindOnly))
                {
                    return;
                }
            }

            if (match == null)
                match = mtd;
            else if (matches == null)
                matches = new List<MethodInfo>(2) { match, mtd };
            else
                matches.Add(mtd);
        }
    }
    private static Type DetermineIdentifierType(IRpcSerializer serializer, string? typeName, uint? knownTypeId)
    {
        Type? type = null;
        if (knownTypeId.HasValue)
            serializer.TryGetKnownType(knownTypeId.Value, out type);

        if (type != null)
            return type;

        if (typeName != null)
            type = Type.GetType(typeName, false, false);

        if (type == null)
            throw new RpcOverheadParseException(Properties.Exceptions.RpcOverheadParseExceptionUnknownIdentifierType) { ErrorCode = 8 };

        if (knownTypeId.HasValue)
        {
            serializer.SaveKnownType(knownTypeId.Value, type);
        }

        return type;
    }
    
    [Flags]
    protected internal enum IdentifierFlags : byte
    {
        IsKnownTypeOnly = 1,
        IsTypeNameOnly = 1 << 1
    }

    [Flags]
    protected internal enum EndpointFlags : byte
    {
        DefinesParameters = 1,
        ArgsAreBindOnly = 1 << 1,
        Broadcast = 1 << 2,
        IgnoreSignatureHash = 1 << 3
    }
}
