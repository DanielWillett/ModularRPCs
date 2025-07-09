using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ModularRPCs.Util;

namespace ModularRPCs.Generators;

internal readonly struct SendMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;
    public readonly RpcClassDeclaration Class;
    public readonly CSharpCompilation Compilation;
    public readonly SendMethodInfo Info;

    internal SendMethodSnippetGenerator(SourceProductionContext context, CSharpCompilation compilation, SendMethodInfo method, RpcClassDeclaration @class)
    {
        Context = context;
        Method = method.Method;
        Send = (RpcSendAttribute)method.Method.Target;
        Info = method;
        Compilation = compilation;
        Class = @class;
    }


    public void GenerateMethodBodySnippet(SourceStringBuilder bldr)
    {
        bool raw = Method.Target.Raw;

        Context.CancellationToken.ThrowIfCancellationRequested();

        EquatableList<RpcParameterDeclaration> parameters = Method.Parameters;

        bool hasId = Class.IdType != null;

        ParameterHelper.BindParameters(parameters, out RpcParameterDeclaration[] toInject, out RpcParameterDeclaration[] toBind);

        bool isReturningTask = !Method.ReturnType.Equals("global::System.Void");

        string returnStr = isReturningTask ? $"return ({Method.ReturnType.GloballyQualifiedName})" : string.Empty;

        bool canUnsignedRightShift = Compilation.LanguageVersion >= LanguageVersion.CSharp11;

        int injectedConnectionArg = -1;
        int cancellationTokenArg = -1;
        for (int i = 0; i < toInject.Length; i++)
        {
            RpcParameterDeclaration param = toInject[i];
            if (param.RefKind == RefKind.Out)
                continue;

            if (param.Type.Equals("global::System.Threading.CancellationToken"))
            {
                cancellationTokenArg = i;
                continue;
            }

            if (injectedConnectionArg == -1 && (param.Type.Info.IsMultipleConnections || param.Type.Info.IsSingleConnection))
            {
                injectedConnectionArg = i;
            }
        }

        bldr.String("global::DanielWillett.ModularRpcs.Reflection.GeneratedSendMethodState __modularRpcsGeneratedState = default;");

        bldr.String("__modularRpcsGeneratedState.Router = this.__modularRpcsGeneratedProxyContext.Router;")
            .String("__modularRpcsGeneratedState.Serializer = this.__modularRpcsGeneratedProxyContext.DefaultSerializer;");

        bldr.String("__modularRpcsGeneratedState.PreCalc = __modularRpcsGeneratedState.Serializer.CanFastReadPrimitives;");

        RpcParameterDeclaration? rawBytesPosition = null,
                                 rawCountPosition = null,
                                 rawCanTakeOwnershipPosition = null;

        bldr.String("__modularRpcsGeneratedState.OverheadSize = __modularRpcsGeneratedState.Router.GetOverheadSize(")
            .In()
                .Build($"@{Class.Type.Name}.{Info.MethodInfoFieldName}.MethodHandle,")
                .Build($"ref @{Class.Type.Name}.{Info.MethodInfoFieldName}").Out()
            .String(");")
            .Empty();

        // PreOverheadSize includes overhead but not ID

        bldr.String("__modularRpcsGeneratedState.PreOverheadSize = __modularRpcsGeneratedState.OverheadSize;");

        GenerateGetIdSize(
            bldr,
            Class,
            out string? escapedIdTypeName,
            "__modularRpcsGeneratedState.IdTypeSize",
            "__modularRpcsGeneratedState.IdSize",
            "__modularRpcsGeneratedState.Serializer",
            "__modularRpcsGeneratedState.HasKnownTypeId",
            "__modularRpcsGeneratedState.KnownTypeId",
            "__modularRpcsGeneratedState.PreCalc",
            "__modularRpcsGeneratedState.OverheadSize",
            out bool passIdByRef,
            "this"
        );

        bldr.String("__modularRpcsGeneratedState.OverheadSize += __modularRpcsGeneratedState.IdTypeSize;");

        if (!raw && toBind.Length > 0)
        {
            bldr.String("#region Get Size")
                .Empty();

            uint ttl = checked ( (uint)toBind.Sum(parameter => TypeHelper.GetPrimitiveLikeSize(parameter.Type.PrimitiveLikeType)) );

            if (ttl != 0)
            {
                bldr.Build($"__modularRpcsGeneratedState.Size = (__modularRpcsGeneratedState.PreCalc ? 1u : 0u) * {ttl}u;");
            }

            bldr.Empty();

            foreach (RpcParameterDeclaration parameter in toBind)
            {
                if (parameter.Type.PrimitiveLikeType != TypeHelper.PrimitiveLikeType.None)
                {
                    bldr.String("if (!__modularRpcsGeneratedState.PreCalc)")
                        .String("{").In();
                }

                TypeSerializationInfo info = parameter.Type.Info;
                switch (info.Type)
                {
                    case TypeSerializationInfoType.Value:
                    case TypeSerializationInfoType.PrimitiveLike:
                        bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSize<{
                            parameter.Type.GloballyQualifiedName}>(@{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.NullableValue:
                        bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSize<{
                            info.UnderlyingType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.SerializableValue:
                        bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializableSize<{
                            info.SerializableType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.NullableSerializableValue:
                        bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializableSize<{
                            info.SerializableType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.SerializableCollection:
                    case TypeSerializationInfoType.NullableSerializableCollection:
                        if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        {
                            bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializablesSize<{
                                info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}) );");
                        }
                        else
                        {
                            bldr.Build($"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializablesSize<{
                                info.SerializableType.GloballyQualifiedName}>(@{parameter.Name}) );");
                        }
                        break;

                    case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                        if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        {
                            bldr.Build($"if (@{parameter.Name}.HasValue)")
                                .In().Build(
                                    $"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}.Value) );").Out()
                                .Build($"else")
                                .In().Build(
                                    $"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(null) );").Out();
                        }
                        else
                        {
                            bldr.Build($"if (@{parameter.Name}.HasValue)")
                                .In().Build(
                                    $"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}.Value) );").Out()
                                .Build($"else")
                                .In().Build(
                                    $"__modularRpcsGeneratedState.Size += checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(null) );").Out();
                        }
                        break;
                }

                if (parameter.Type.PrimitiveLikeType != TypeHelper.PrimitiveLikeType.None)
                {
                    bldr.Out()
                        .String("}")
                        .Empty();
                }
            }

            bldr.Empty()
                .String("#endregion")
                .Empty();
        }
        else if (raw)
        {
            bool needsCountParam = false;
            foreach (RpcParameterDeclaration param in toBind)
            {
                switch (param.Type.PrimitiveType)
                {
                    case TypeHelper.PrimitiveLikeType.Boolean:

                        if (rawCanTakeOwnershipPosition != null)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleCanTakeOwnership);");
                            continue;
                        }

                        rawCanTakeOwnershipPosition = param;
                        continue;

                    case TypeHelper.PrimitiveLikeType.SByte:
                    case TypeHelper.PrimitiveLikeType.Byte:
                    case TypeHelper.PrimitiveLikeType.Int16:
                    case TypeHelper.PrimitiveLikeType.UInt16:
                    case TypeHelper.PrimitiveLikeType.Int32:
                    case TypeHelper.PrimitiveLikeType.UInt32:
                    case TypeHelper.PrimitiveLikeType.Int64:
                    case TypeHelper.PrimitiveLikeType.UInt64:
                    case TypeHelper.PrimitiveLikeType.IntPtr:
                    case TypeHelper.PrimitiveLikeType.UIntPtr:
                        if (rawCountPosition != null)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleByteCount);");
                            continue;
                        }

                        if (param.Type.PrimitiveType is TypeHelper.PrimitiveLikeType.SByte or TypeHelper.PrimitiveLikeType.Int16 or TypeHelper.PrimitiveLikeType.Int32 or TypeHelper.PrimitiveLikeType.Int64 or TypeHelper.PrimitiveLikeType.UInt64 or TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr)
                        {
                            bldr.Build($"if (@{param.Name} < 0 || @{param.Name} > {int.MaxValue})")
                                .In().Build($"throw new global::System.ArgumentOutOfRangeException(nameof(@{param.Name}), @{param.Name}, null);").Out();
                        }

                        rawCountPosition = param;
                        break;

                    default:

                        if (param.RawInjectType is ParameterHelper.RawByteInjectType.None)
                        {
                            bldr.Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInvalidRawParameter, \"{TypeHelper.Escape(param.Name)}\", \"{TypeHelper.Escape(param.Type.FullyQualifiedName)}\"));");
                            continue;
                        }

                        if (rawBytesPosition != null)
                        {
                            bldr.String("throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionMultipleByteData);");
                            continue;
                        }

                        rawBytesPosition = param;
                        switch (param.RawInjectType)
                        {
                            case ParameterHelper.RawByteInjectType.Pointer:
                            case ParameterHelper.RawByteInjectType.ByRefByte:
                                needsCountParam = true;
                                break;
                        }
                        break;
                }
            }

            if (needsCountParam && rawCountPosition == null)
            {
                bldr.Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionNoByteCount, \"{TypeHelper.Escape(rawBytesPosition!.Type.FullyQualifiedName)}\"));");
                return;
            }

            if (rawBytesPosition == null)
            {
                bldr.Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionNoByteData);");
                return;
            }

            string? methodName = rawBytesPosition.RawInjectType switch
            {
                ParameterHelper.RawByteInjectType.Pointer => "InvokeRpcInvokerByPointer",
                ParameterHelper.RawByteInjectType.Array => "InvokeRpcInvokerByArray",
                ParameterHelper.RawByteInjectType.Stream => "InvokeRpcInvokerByStream",
                ParameterHelper.RawByteInjectType.Memory => "InvokeRpcInvokerByMemory",
                ParameterHelper.RawByteInjectType.ReadOnlyMemory => "InvokeRpcInvokerByReadOnlyMemory",
                ParameterHelper.RawByteInjectType.Span => "InvokeRpcInvokerBySpan",
                ParameterHelper.RawByteInjectType.ReadOnlySpan => "InvokeRpcInvokerByReadOnlySpan",
                ParameterHelper.RawByteInjectType.ArraySegment => "InvokeRpcInvokerByArraySegment",
                ParameterHelper.RawByteInjectType.IList
                    or ParameterHelper.RawByteInjectType.List
                    or ParameterHelper.RawByteInjectType.ICollection => "InvokeRpcInvokerByCollection",
                ParameterHelper.RawByteInjectType.IReadOnlyList
                    or ParameterHelper.RawByteInjectType.ReadOnlyCollection
                    or ParameterHelper.RawByteInjectType.IReadOnlyCollection => "InvokeRpcInvokerByReadOnlyCollection",
                ParameterHelper.RawByteInjectType.ArrayList => "InvokeRpcInvokerByArrayList",
                ParameterHelper.RawByteInjectType.ByRefByte => "InvokeRpcInvokerByReference",
                ParameterHelper.RawByteInjectType.ByteWriter => "InvokeRpcInvokerByByteWriter",
                ParameterHelper.RawByteInjectType.ByteReader => "InvokeRpcInvokerByByteReader",
                _ => null
            };

            if (methodName == null)
            {
                bldr.Build($"throw new global::DanielWillett.ModularRpcs.Exceptions.RpcInjectionException(string.Format(global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.ResxRpcInjectionExceptionInvalidRawParameter, \"{TypeHelper.Escape(rawBytesPosition.Name)}\", \"{TypeHelper.Escape(rawBytesPosition.Type.FullyQualifiedName)}\"));");
                return;
            }
            
            bldr.String("byte[] __modularRpcsGeneratedHeader = new byte[__modularRpcsGeneratedState.OverheadSize];");

            if (Class.IdType == null)
            {
                bldr.String("// TypeCode: Empty")
                    .String("__modularRpcsGeneratedHeader[__modularRpcsGeneratedState.PreOverheadSize] = 0;");
            }
            else
            {
                if (!Method.NeedsUnsafe)
                {
                    bldr.String("unsafe")
                        .String("{")
                        .In();
                }

                bldr.String("fixed (byte* __modularRpcsGeneratedHeaderPointer = __modularRpcsGeneratedHeader)")
                    .String("{")
                    .In()
                    .String("__modularRpcsGeneratedState.Buffer = __modularRpcsGeneratedHeaderPointer;");

                WriteIdentifier(bldr, canUnsignedRightShift, escapedIdTypeName);

                bldr.Out()
                    .String("}");

                if (!Method.NeedsUnsafe)
                {
                    bldr.Out()
                        .String("}");
                }
            }

            bldr.Build($"{returnStr}global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.{methodName}(")
                .In()
                .String("__modularRpcsGeneratedState.Router,");

            LoadConnection(bldr, injectedConnectionArg, toInject);

            bldr.String("__modularRpcsGeneratedState.Serializer,")
                .Build($"@{Class.Type.Name}.{Info.MethodInfoFieldName}.MethodHandle,");

            LoadCancellationToken(bldr, cancellationTokenArg, toInject);

            switch (rawBytesPosition.RawInjectType)
            {
                case ParameterHelper.RawByteInjectType.ByRefByte:
                    bldr.Build($"ref @{rawBytesPosition.Name},");
                    break;

                default:
                    bldr.Build($"@{rawBytesPosition.Name},");
                    break;
            }

            if (rawCountPosition != null)
            {
                switch (rawCountPosition.Type.PrimitiveType)
                {
                    case TypeHelper.PrimitiveLikeType.SByte:
                    case TypeHelper.PrimitiveLikeType.Int16:
                    case TypeHelper.PrimitiveLikeType.Int32:
                    case TypeHelper.PrimitiveLikeType.Int64:
                    case TypeHelper.PrimitiveLikeType.UInt64:
                    case TypeHelper.PrimitiveLikeType.IntPtr:
                    case TypeHelper.PrimitiveLikeType.UIntPtr:
                        bldr.Build($"checked ( (uint)@{rawCountPosition.Name} ),");
                        break;

                    default:
                        bldr.Build($"@{rawCountPosition.Name},");
                        break;
                }
            }
            else
            {
                switch (rawBytesPosition.RawInjectType)
                {
                    case ParameterHelper.RawByteInjectType.Stream:
                        bldr.Build($"checked ( (uint)(@{rawBytesPosition.Name}.Length - @{rawBytesPosition.Name}.Position) ),");
                        break;

                    case ParameterHelper.RawByteInjectType.Array:
                    case ParameterHelper.RawByteInjectType.Memory:
                    case ParameterHelper.RawByteInjectType.ReadOnlyMemory:
                    case ParameterHelper.RawByteInjectType.Span:
                    case ParameterHelper.RawByteInjectType.ReadOnlySpan:
                        bldr.Build($"(uint)@{rawBytesPosition.Name}.Length,");
                        break;

                    case ParameterHelper.RawByteInjectType.IList:
                    case ParameterHelper.RawByteInjectType.ArraySegment:
                    case ParameterHelper.RawByteInjectType.IReadOnlyList:
                    case ParameterHelper.RawByteInjectType.ICollection:
                    case ParameterHelper.RawByteInjectType.IReadOnlyCollection:
                    case ParameterHelper.RawByteInjectType.List:
                    case ParameterHelper.RawByteInjectType.ArrayList:
                    case ParameterHelper.RawByteInjectType.ReadOnlyCollection:
                    case ParameterHelper.RawByteInjectType.ByteWriter:
                        bldr.Build($"(uint)@{rawBytesPosition.Name}.Count,");
                        break;

                    case ParameterHelper.RawByteInjectType.IEnumerable:
                    case ParameterHelper.RawByteInjectType.ByteReader:
                        bldr.String("uint.MaxValue,");
                        break;
                }
            }

            bldr.String("__modularRpcsGeneratedHeader,");

            if (rawBytesPosition.RawInjectType == ParameterHelper.RawByteInjectType.Stream)
            {
                if (rawCanTakeOwnershipPosition != null)
                {
                    bldr.Build($"!@{rawCanTakeOwnershipPosition.Name},");
                }
                else
                {
                    bldr.String("true,");
                }
            }

            bldr.Build($"ref @{Class.Type.Name}.{Info.MethodInfoFieldName}")
                .Out()
                .String(");");

            return;
        }

        bldr.String("__modularRpcsGeneratedState.Size += __modularRpcsGeneratedState.OverheadSize;");


        bldr.String("unsafe")
            .String("{")
            .In();

        // based on an array
        bldr.String("if (__modularRpcsGeneratedState.Size > this.__modularRpcsGeneratedProxyContext.Generator.MaxSizeForStackalloc)")
            .String("{")
            .In();

        bldr.String("byte[] __modularRpcsGeneratedBufferArray = new byte[__modularRpcsGeneratedState.Size];")
            .String("fixed (byte* __modularRpcsGeneratedBuffer = __modularRpcsGeneratedBufferArray)")
            .String("{")
            .In();

        bldr.String("__modularRpcsGeneratedState.Buffer = __modularRpcsGeneratedBuffer;");

        string parameterList = parameters.Count == 0 ? string.Empty : (", " + string.Join(", ", parameters.Select(x => $"@{x.Name}")));

        bldr.Build($"{returnStr}__ModularRpcsGeneratedInvoke(ref __modularRpcsGeneratedState{(hasId ? (passIdByRef ? ", ref __modularRpcsGeneratedId" : ", __modularRpcsGeneratedId") : string.Empty)}{parameterList});");

        bldr.Out()
            .String("}");

        bldr.Out()
            .String("}")
            .String("else")
            .String("{").In();

        // based on a stackalloc
        bldr.String("byte* __modularRpcsGeneratedBuffer = stackalloc byte[checked ( (int)__modularRpcsGeneratedState.Size )];")
            .String("__modularRpcsGeneratedState.Buffer = __modularRpcsGeneratedBuffer;");


        bldr.Build($"{returnStr}__ModularRpcsGeneratedInvoke(ref __modularRpcsGeneratedState{(hasId ? (passIdByRef ? ", ref __modularRpcsGeneratedId" : ", __modularRpcsGeneratedId") : string.Empty)}{parameterList});");

        bldr.Out()
            .String("}")
            .Out()
            .String("}");

        //foreach (RpcParameterDeclaration parameter in parameters)
        //{
        //    bldr.Build($"global::System.Console.WriteLine(\"{parameter.Name}: {parameter.Type.GloballyQualifiedName}\");");
        //}

        string parameterDefList = parameters.Count == 0 ? string.Empty : (", " + string.Join(", ", parameters.Select(x => x.Definition)));

        bldr.Build($"static unsafe {Method.ReturnType.GloballyQualifiedName} __ModularRpcsGeneratedInvoke(ref global::DanielWillett.ModularRpcs.Reflection.GeneratedSendMethodState __modularRpcsGeneratedState{(hasId ? (passIdByRef ? $", ref {Class.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId" : $", {Class.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId") : string.Empty)}{parameterDefList})")
            .String("{")
            .In();

        WriteIdentifier(bldr, canUnsignedRightShift, escapedIdTypeName);

        bldr.Empty();

        if (!raw)
        {
            if (toBind.Length > 0)
            {
                if (toBind.Length == 1)
                    bldr.Build($"#region Write Parameter");
                else
                    bldr.Build($"#region Write Parameters ({toBind.Length} parameters)");

                bldr.String(Class.IdType == null
                    ? "uint __modularRpcsGeneratedIndex = __modularRpcsGeneratedState.PreOverheadSize + 1u;"
                    : "uint __modularRpcsGeneratedIndex = __modularRpcsGeneratedIdPos;");

                foreach (RpcParameterDeclaration param in toBind)
                {
                    bldr.Empty()
                        .Build($"/* Write {param.Type.FullyQualifiedName} (#{param.Index}) */");

                    WriteValue(bldr, param.Type, canUnsignedRightShift, "__modularRpcsGeneratedIndex", $"@{param.Name}", "__modularRpcsGeneratedState.Size - __modularRpcsGeneratedIndex");
                }

                bldr.String("#endregion");
            }
            else
            {
                bldr.String("// There are no parameters to write.");
            }
        }

        bldr.Build($"{returnStr}__modularRpcsGeneratedState.Router.InvokeRpc(")
            .In();

        LoadConnection(bldr, injectedConnectionArg, toInject);

        bldr    .String("__modularRpcsGeneratedState.Serializer,")
                .Build($"@{Class.Type.Name}.{Info.MethodInfoFieldName}.MethodHandle,");

        LoadCancellationToken(bldr, cancellationTokenArg, toInject);

        bldr    .String("__modularRpcsGeneratedState.Buffer,")
                .String("checked ( (int)__modularRpcsGeneratedState.Size ),")
                .String("__modularRpcsGeneratedState.Size - __modularRpcsGeneratedState.OverheadSize,")
                .Build($"ref @{Class.Type.Name}.{Info.MethodInfoFieldName},")
                .String("global::DanielWillett.ModularRpcs.Routing.RpcInvokeOptions.Default | global::DanielWillett.ModularRpcs.Routing.RpcInvokeOptions.Generated").Out()
            .String(");");

        bldr.Out()
            .String("}");
    }

    private void WriteIdentifier(SourceStringBuilder bldr, bool canUnsignedRightShift, string? escapedIdTypeName)
    {
        if (Class.IdType == null)
        {
            bldr.String("// TypeCode: Empty")
                .String("__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize] = 0;");
            return;
        }

        bldr.Build($"#region Write ID ({Class.IdType.Name})")
            .Build($"// TypeCode: {Class.IdTypeCode.ToTypeCodeString()}")
            .Build($"__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize] = {(byte)Class.IdTypeCode};")
            .Empty();

        if (Class.IdTypeCode == TypeCode.Object)
        {
            bldr.String("uint __modularRpcsGeneratedIdPos;")
                .String("if (__modularRpcsGeneratedState.HasKnownTypeId)")
                .String("{")
                .In();

            bldr.Build($"__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize + 1] = {(byte)RpcEndpoint.IdentifierFlags.IsKnownTypeOnly};")
                .Empty()
                .String("byte* __modularRpcsGeneratedKnownTypeIdBufferLocation = __modularRpcsGeneratedState.Buffer + __modularRpcsGeneratedState.PreOverheadSize + 2;")
                .Preprocessor("#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER")
                .String("uint __modularRpcsGeneratedKnownTypeIdEndiannessApplied = global::System.BitConverter.IsLittleEndian ? __modularRpcsGeneratedState.KnownTypeId : global::System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(__modularRpcsGeneratedState.KnownTypeId);")
                .Preprocessor("#else")
                .String("uint __modularRpcsGeneratedKnownTypeIdEndiannessApplied;")
                .String("if (global::System.BitConverter.IsLittleEndian)")
                .In().String("__modularRpcsGeneratedKnownTypeIdEndiannessApplied = __modularRpcsGeneratedState.KnownTypeId;").Out()
                .String("else")
                .In().String("__modularRpcsGeneratedKnownTypeIdEndiannessApplied = (__modularRpcsGeneratedState.KnownTypeId & 0x000000ffu) << 24 | (__modularRpcsGeneratedState.KnownTypeId & 0x0000ff00u) << 8 | (__modularRpcsGeneratedState.KnownTypeId & 0x00ff0000u) >> 8 | (__modularRpcsGeneratedState.KnownTypeId & 0xff000000u) >> 24;").Out()
                .Preprocessor("#endif")
                .Empty()
                .String("if ((nint)__modularRpcsGeneratedKnownTypeIdBufferLocation % sizeof(uint) == 0)")
                .In().String("*(uint*)__modularRpcsGeneratedKnownTypeIdBufferLocation = __modularRpcsGeneratedKnownTypeIdEndiannessApplied;").Out()
                .String("else")
                .String("{").In()
                    .String("*__modularRpcsGeneratedKnownTypeIdBufferLocation   = unchecked( (byte)(__modularRpcsGeneratedKnownTypeIdEndiannessApplied) );")
                    .String("__modularRpcsGeneratedKnownTypeIdBufferLocation[1] = unchecked( (byte)(__modularRpcsGeneratedKnownTypeIdEndiannessApplied >>  8) );")
                    .String("__modularRpcsGeneratedKnownTypeIdBufferLocation[2] = unchecked( (byte)(__modularRpcsGeneratedKnownTypeIdEndiannessApplied >> 16) );")
                    .String("__modularRpcsGeneratedKnownTypeIdBufferLocation[3] = unchecked( (byte)(__modularRpcsGeneratedKnownTypeIdEndiannessApplied >> 24) );")
                .Out()
                .String("}")
                .Empty()
                .String("__modularRpcsGeneratedIdPos = __modularRpcsGeneratedState.PreOverheadSize + 6u;");

            bldr.Out()
                .String("}")
                .String("else")
                .String("{").In();

            bldr.Build($"__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize + 1] = {(byte)RpcEndpoint.IdentifierFlags.IsTypeNameOnly};")
                .Empty()
                .String($"__modularRpcsGeneratedIdPos = checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteObject<string>(\"{escapedIdTypeName}\", __modularRpcsGeneratedState.Buffer + __modularRpcsGeneratedState.PreOverheadSize + 2, __modularRpcsGeneratedState.IdTypeSize - 2u) ) + __modularRpcsGeneratedState.PreOverheadSize + 2u;");

            bldr.Out()
                .String("}");
        }
        else
        {
            bldr.String("uint __modularRpcsGeneratedIdPos = __modularRpcsGeneratedState.PreOverheadSize + 1u;");
        }

        WriteValue(bldr, Class.IdType, canUnsignedRightShift, "__modularRpcsGeneratedIdPos", "__modularRpcsGeneratedId", "__modularRpcsGeneratedState.IdSize");

        bldr.String("#endregion");
    }

    private void LoadConnection(SourceStringBuilder bldr, int injectedConnectionArg, IList<RpcParameterDeclaration> toInject)
    {
        string? thisConnection = null;
        if (injectedConnectionArg == -1)
        {
            if (Class.IsSingleConnectionObject)
            {
                thisConnection = Class.IsSingleConnectionExplicit ? "((global::DanielWillett.ModularRpcs.Protocol.IRpcSingleConnectionObject)this).Connection" : "this.Connection";
            }
            else if (Class.IsMultipleConnectionObject)
            {
                thisConnection = Class.IsMultipleConnectionExplicit ? "((global::DanielWillett.ModularRpcs.Protocol.IRpcMultipleConnectionsObject)this).Connections" : "this.Connections";
            }
        }

        if (thisConnection != null)
        {
            bldr.Build($"{thisConnection},");
        }
        else if (injectedConnectionArg != -1)
        {
            RpcParameterDeclaration decl = toInject[injectedConnectionArg];
            if (decl.Type.Info.IsMultipleConnections)
                bldr.Build($"@{decl.Name} as global::System.Collections.Generic.IEnumerable<global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection>,");
            else
                bldr.Build($"@{decl.Name} as global::DanielWillett.ModularRpcs.Abstractions.IModularRpcConnection,");
        }
        else
        {
            bldr.String("null,");
        }
    }

    private static void LoadCancellationToken(SourceStringBuilder bldr, int cancellationTokenArg, RpcParameterDeclaration[] toInject)
    {
        if (cancellationTokenArg != -1)
        {
            bldr.Build($"@{toInject[cancellationTokenArg].Name},");
        }
        else
        {
            bldr.String("global::System.Threading.CancellationToken.None,");
        }
    }

    internal static void GenerateGetIdSize(SourceStringBuilder bldr, RpcClassDeclaration @class, out string? escapedIdTypeName, string idTypeSize, string idSize, string serializer, string? hasKnownTypeId, string knownTypeId, string preCalc, string overheadSize, out bool passIdByRef, string @this)
    {
        bool hasId = @class.IdType != null;

        // calculate ID size
        if (!hasId || @class.IdTypeCode != TypeCode.Object)
        {
            escapedIdTypeName = null;
            bldr.Build($"{idTypeSize} = 1u;");
        }
        else
        {
            escapedIdTypeName = TypeHelper.Escape(@class.IdType!.Info.SerializableType.AssemblyQualifiedName);

            if (hasKnownTypeId != null)
                bldr.Build($"if ({hasKnownTypeId} = {serializer}.TryGetKnownTypeId(typeof({@class.IdType!.GloballyQualifiedName}), out {knownTypeId}))");
            else
                bldr.Build($"if ({serializer}.TryGetKnownTypeId(typeof({@class.IdType!.GloballyQualifiedName}), out {knownTypeId}))");
            bldr.String("{").In()
                    .Build($"{idTypeSize} = 6u;")
                    .Out()
                .String("}")
                .String("else")
                .String("{").In()
                    .Build($"{idTypeSize} = 2u + checked ( (uint){serializer}.GetSize<string>(\"{escapedIdTypeName}\") );")
                    .Out()
                .String("}");
        }

        passIdByRef = false;

        if (hasId && @class.IdType!.Info.Type != TypeSerializationInfoType.Void)
        {
            TypeSerializationInfo idInfo = @class.IdType.Info;

            string idVar = @class.IdIsExplicit
                ? $"((global::DanielWillett.ModularRpcs.Protocol.IRpcObject<{@class.IdType!.GloballyQualifiedName}>){@this}).Identifier"
                : $"{@this}.Identifier";

            bldr.Build($"{@class.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId = {idVar};");

            switch (idInfo.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:

                    if (idInfo.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                    {
                        switch (idInfo.PrimitiveSerializationMode)
                        {
                            case TypeHelper.QuickSerializeMode.If64Bit:
                                bldr.Build($"if ({preCalc} && global::System.IntPtr.Size == 8)");
                                break;
                            case TypeHelper.QuickSerializeMode.If64BitLittleEndian:
                                bldr.Build($"if ({preCalc} && global::System.IntPtr.Size == 8 && global::System.BitConverter.IsLittleEndian)");
                                break;
                            case TypeHelper.QuickSerializeMode.IfLittleEndian:
                                bldr.Build($"if ({preCalc} && global::System.BitConverter.IsLittleEndian)");
                                break;
                            default: // Always
                                bldr.Build($"if ({preCalc})");
                                break;
                        }

                        int size = TypeHelper.GetPrimitiveLikeSize(@class.IdType.PrimitiveType);
                        bldr.String("{")
                            .In()
                            .Build($"{idSize} = {size}u;")
                            .Out()
                            .String("}");

                        bldr.String("else")
                            .String("{")
                            .In();
                    }
                    else
                    {
                        passIdByRef = @class.IdType.IsValueType;
                    }

                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetSize<{@class.IdType.GloballyQualifiedName}>(__modularRpcsGeneratedId) );");

                    if (idInfo.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                    {
                        bldr.Out()
                            .String("}");
                    }

                    break;

                case TypeSerializationInfoType.NullableValue:
                    passIdByRef = true;
                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetSize<{idInfo.UnderlyingType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    passIdByRef = @class.IdType!.IsValueType;
                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetSerializableSize<{idInfo.SerializableType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    passIdByRef = true;
                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetNullableSerializableSize<{idInfo.SerializableType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetSerializablesSize<{idInfo.SerializableType.GloballyQualifiedName}>(__modularRpcsGeneratedId as global::System.Collections.Generic.IEnumerable<{idInfo.SerializableType.GloballyQualifiedName}>) );");
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
                    bldr.Build($"{idSize} = checked ( (uint){serializer}.GetNullableSerializablesSize<{idInfo.SerializableType.GloballyQualifiedName}>(__modularRpcsGeneratedId as global::System.Collections.Generic.IEnumerable<{idInfo.SerializableType.GloballyQualifiedName}?>) );");
                    break;
            }

            bldr.String($"{overheadSize} += {idSize};");
        }
    }

    private static void WriteValue(SourceStringBuilder bldr, TypeSymbolInfo symbolInfo, bool canUnsignedRightShift, string offsetVar, string valueVar, string sizeVar)
    {
        TypeSerializationInfo info = symbolInfo.Info;
        switch (info.Type)
        {
            case TypeSerializationInfoType.PrimitiveLike:
            case TypeSerializationInfoType.Value:

                if (info.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                {
                    switch (info.PrimitiveSerializationMode)
                    {
                        case TypeHelper.QuickSerializeMode.If64Bit:
                            bldr.String("if (__modularRpcsGeneratedState.PreCalc && global::System.IntPtr.Size == 8)");
                            break;
                        case TypeHelper.QuickSerializeMode.If64BitLittleEndian:
                            bldr.String("if (__modularRpcsGeneratedState.PreCalc && global::System.IntPtr.Size == 8 && global::System.BitConverter.IsLittleEndian)");
                            break;
                        case TypeHelper.QuickSerializeMode.IfLittleEndian:
                            bldr.String("if (__modularRpcsGeneratedState.PreCalc && global::System.BitConverter.IsLittleEndian)");
                            break;
                        default: // Always
                            bldr.String("if (__modularRpcsGeneratedState.PreCalc)");
                            break;
                    }

                    bldr.String("{")
                        .In();

                    TypeHelper.PrimitiveLikeType idPrimType = symbolInfo.PrimitiveType & TypeHelper.PrimitiveLikeType.UnderlyingTypeMask;
                    switch (idPrimType)
                    {
                        case TypeHelper.PrimitiveLikeType.Boolean:
                            if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = (bool){valueVar} ? (byte)1 : (byte)0;");
                            else
                                bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = {valueVar} ? (byte)1 : (byte)0;");

                            bldr.Build($"++{offsetVar};");
                            break;

                        case TypeHelper.PrimitiveLikeType.Byte:
                            if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) != 0)
                                bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = (byte){valueVar};");
                            else
                                bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = {valueVar};");

                            bldr.Build($"++{offsetVar};");
                            break;

                        case TypeHelper.PrimitiveLikeType.SByte:
                            bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = unchecked ( (byte){valueVar} );")
                                .Build($"++{offsetVar};");
                            break;

                        default:
                            string bufferName = offsetVar + "BufferLocation";

                            bldr.Build($"byte* {bufferName} = __modularRpcsGeneratedState.Buffer + {offsetVar};");
                            if (idPrimType is TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr)
                                bldr.Build($"if ((nint){bufferName} % 8 == 0)");
                            else
                                bldr.Build($"if ((nint){bufferName} % sizeof({symbolInfo.GloballyQualifiedName}) == 0)");
                            bldr.String("{").In()
                                .Build($"*({symbolInfo.GloballyQualifiedName}*){bufferName} = {valueVar};");

                            if (idPrimType is TypeHelper.PrimitiveLikeType.IntPtr or TypeHelper.PrimitiveLikeType.UIntPtr)
                                bldr.Build($"{offsetVar} += 8;");
                            else
                                bldr.Build($"{offsetVar} += sizeof({symbolInfo.GloballyQualifiedName});");

                            bldr.Out()
                                .String("}")
                                .String("else")
                                .String("{").In();

                            switch (idPrimType)
                            {
                                case TypeHelper.PrimitiveLikeType.Char:
                                case TypeHelper.PrimitiveLikeType.Int16:
                                case TypeHelper.PrimitiveLikeType.UInt16:
                                    string shift;
                                    if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) == 0)
                                    {
                                        if (canUnsignedRightShift)
                                            shift = $"{valueVar} >>>";
                                        else if (idPrimType == TypeHelper.PrimitiveLikeType.UInt16)
                                            shift = $"(uint){valueVar} >>";
                                        else
                                            shift = $"unchecked ( (uint){valueVar} ) >>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.Char && canUnsignedRightShift)
                                    {
                                        shift = $"(char){valueVar} >>>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.Int16 && canUnsignedRightShift)
                                    {
                                        shift = $"(short){valueVar} >>>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.UInt16)
                                    {
                                        shift = $"(uint){valueVar} >>";
                                    }
                                    else
                                    {
                                        shift = $"unchecked ( (uint)(ushort){valueVar} ) >>";
                                    }

                                    bldr.Build($"*{bufferName}   = unchecked( (byte)({valueVar}) );")
                                        .Build($"{bufferName}[1] = unchecked( (byte)({shift}  8) );")
                                        .Build($"{offsetVar} += 2u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int32:
                                case TypeHelper.PrimitiveLikeType.UInt32:
                                    if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) == 0)
                                    {
                                        if (canUnsignedRightShift)
                                            shift = $"{valueVar} >>>";
                                        else if (idPrimType == TypeHelper.PrimitiveLikeType.UInt32)
                                            shift = $"{valueVar} >>";
                                        else
                                            shift = $"unchecked ( (uint){valueVar} ) >>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.Int32 && canUnsignedRightShift)
                                    {
                                        shift = $"(int){valueVar} >>>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.UInt32)
                                    {
                                        shift = $"(uint){valueVar} >>";
                                    }
                                    else
                                    {
                                        shift = $"unchecked ( (uint){valueVar} ) >>";
                                    }

                                    bldr.Build($"*{bufferName}   = unchecked( (byte)({valueVar}) );")
                                        .Build($"{bufferName}[1] = unchecked( (byte)({shift}  8) );")
                                        .Build($"{bufferName}[2] = unchecked( (byte)({shift} 16) );")
                                        .Build($"{bufferName}[3] = unchecked( (byte)({shift} 24) );")
                                        .Build($"{offsetVar} += 4u;");
                                    break;

                                case TypeHelper.PrimitiveLikeType.Single:

                                    if (canUnsignedRightShift)
                                    {
                                        bldr.Build($"int {valueVar}Num = *(int*)&{valueVar};")
                                            .Build($"*{bufferName}   = unchecked( (byte)({valueVar}Num) );")
                                            .Build($"{bufferName}[1] = unchecked( (byte)({valueVar}Num >>>  8) );")
                                            .Build($"{bufferName}[2] = unchecked( (byte)({valueVar}Num >>> 16) );")
                                            .Build($"{bufferName}[3] = unchecked( (byte)({valueVar}Num >>> 24) );")
                                            .Build($"{offsetVar} += 4u;");
                                    }
                                    else
                                    {
                                        bldr.Build($"uint {valueVar}Num = *(uint*)&{valueVar};")
                                            .Build($"*{bufferName}   = unchecked( (byte)({valueVar}Num) );")
                                            .Build($"{bufferName}[1] = unchecked( (byte)({valueVar}Num >>  8) );")
                                            .Build($"{bufferName}[2] = unchecked( (byte)({valueVar}Num >> 16) );")
                                            .Build($"{bufferName}[3] = unchecked( (byte)({valueVar}Num >> 24) );")
                                            .Build($"{offsetVar} += 4u;");
                                    }
                                    break;

                                case TypeHelper.PrimitiveLikeType.Double:

                                    if (canUnsignedRightShift)
                                    {
                                        bldr.Build($"long {valueVar}Num = *(long*)&{valueVar};")
                                            .Build($"*{bufferName}   = unchecked( (byte)({valueVar}Num) );")
                                            .Build($"{bufferName}[1] = unchecked( (byte)({valueVar}Num >>>  8) );")
                                            .Build($"{bufferName}[2] = unchecked( (byte)({valueVar}Num >>> 16) );")
                                            .Build($"{bufferName}[3] = unchecked( (byte)({valueVar}Num >>> 24) );")
                                            .Build($"{bufferName}[4] = unchecked( (byte)({valueVar}Num >>> 32) );")
                                            .Build($"{bufferName}[5] = unchecked( (byte)({valueVar}Num >>> 40) );")
                                            .Build($"{bufferName}[6] = unchecked( (byte)({valueVar}Num >>> 48) );")
                                            .Build($"{bufferName}[7] = unchecked( (byte)({valueVar}Num >>> 56) );")
                                            .Build($"{offsetVar} += 8u;");
                                    }
                                    else
                                    {
                                        bldr.Build($"ulong {valueVar}Num = *(ulong*)&{valueVar};")
                                            .Build($"*{bufferName}   = unchecked( (byte)({valueVar}Num) );")
                                            .Build($"{bufferName}[1] = unchecked( (byte)({valueVar}Num >>  8) );")
                                            .Build($"{bufferName}[2] = unchecked( (byte)({valueVar}Num >> 16) );")
                                            .Build($"{bufferName}[3] = unchecked( (byte)({valueVar}Num >> 24) );")
                                            .Build($"{bufferName}[4] = unchecked( (byte)({valueVar}Num >> 32) );")
                                            .Build($"{bufferName}[5] = unchecked( (byte)({valueVar}Num >> 40) );")
                                            .Build($"{bufferName}[6] = unchecked( (byte)({valueVar}Num >> 48) );")
                                            .Build($"{bufferName}[7] = unchecked( (byte)({valueVar}Num >> 56) );")
                                            .Build($"{offsetVar} += 8u;");
                                    }
                                    break;

                                case TypeHelper.PrimitiveLikeType.Int64:
                                case TypeHelper.PrimitiveLikeType.UInt64:
                                case TypeHelper.PrimitiveLikeType.IntPtr:
                                case TypeHelper.PrimitiveLikeType.UIntPtr:
                                    if ((symbolInfo.PrimitiveLikeType & TypeHelper.PrimitiveLikeType.Enum) == 0)
                                    {
                                        if (canUnsignedRightShift)
                                            shift = $"{valueVar} >>>";
                                        else if (idPrimType is TypeHelper.PrimitiveLikeType.UInt64)
                                            shift = $"{valueVar} >>";
                                        else
                                            shift = $"unchecked ( (ulong){valueVar} ) >>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.Int64 && canUnsignedRightShift)
                                    {
                                        shift = $"(long){valueVar} >>>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.IntPtr && canUnsignedRightShift)
                                    {
                                        shift = $"(nint){valueVar} >>>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.UInt64)
                                    {
                                        shift = $"(ulong){valueVar} >>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.UIntPtr)
                                    {
                                        shift = canUnsignedRightShift ? $"(nuint){valueVar} >>" : $"(ulong){valueVar} >>";
                                    }
                                    else if (idPrimType == TypeHelper.PrimitiveLikeType.IntPtr)
                                    {
                                        shift = $"unchecked ( (ulong){valueVar} ) >>";
                                    }
                                    else
                                    {
                                        shift = $"unchecked ( (ulong){valueVar} ) >>";
                                    }

                                    bldr.Build($"*{bufferName}   = unchecked( (byte)({valueVar}) );")
                                        .Build($"{bufferName}[1] = unchecked( (byte)({shift}  8) );")
                                        .Build($"{bufferName}[2] = unchecked( (byte)({shift} 16) );")
                                        .Build($"{bufferName}[3] = unchecked( (byte)({shift} 24) );")
                                        .Build($"{bufferName}[4] = unchecked( (byte)({shift} 32) );")
                                        .Build($"{bufferName}[5] = unchecked( (byte)({shift} 40) );")
                                        .Build($"{bufferName}[6] = unchecked( (byte)({shift} 48) );")
                                        .Build($"{bufferName}[7] = unchecked( (byte)({shift} 56) );")
                                        .Build($"{offsetVar} += 8u;");

                                    break;
                            }

                            bldr.Out()
                                .String("}");
                            break;
                    }

                    bldr.Out()
                        .String("}");

                    bldr.String("else")
                        .String("{")
                        .In();
                }

                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteObject<{symbolInfo.GloballyQualifiedName}>({valueVar}, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");

                if (info.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                {
                    bldr.Out()
                        .String("}");
                }

                break;

            case TypeSerializationInfoType.NullableValue:
                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteObject<{info.UnderlyingType.GloballyQualifiedName}>(in {valueVar}, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");
                break;

            case TypeSerializationInfoType.SerializableValue:
                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteSerializableObject<{info.SerializableType.GloballyQualifiedName}>(in {valueVar}, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");
                break;

            case TypeSerializationInfoType.NullableSerializableValue:
                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteNullableSerializableObject<{info.SerializableType.GloballyQualifiedName}>(in {valueVar}, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");
                break;

            case TypeSerializationInfoType.SerializableCollection:
            case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({valueVar} as global::System.Collections.Generic.IEnumerable<{info.SerializableType.GloballyQualifiedName}>, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");
                break;

            case TypeSerializationInfoType.NullableSerializableCollection:
            case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
                bldr.Build($"{offsetVar} += checked ( (uint)__modularRpcsGeneratedState.Serializer.WriteNullableSerializableObjects<{info.SerializableType.GloballyQualifiedName}>({valueVar} as global::System.Collections.Generic.IEnumerable<{info.SerializableType.GloballyQualifiedName}?>, __modularRpcsGeneratedState.Buffer + {offsetVar}, {sizeVar}) );");
                break;
        }
    }
}

internal class SendMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;
    public string MethodInfoFieldName;
    public string? DelegateName;
    public DelegateType? DelegateType;
    public bool IsDuplicateDelegateType;

    public SendMethodInfo(RpcMethodDeclaration method, int overload, string methodInfoFieldName)
    {
        Method = method;
        Overload = overload;
        MethodInfoFieldName = methodInfoFieldName;
    }
}