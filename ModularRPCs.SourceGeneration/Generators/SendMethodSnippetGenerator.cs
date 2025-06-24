using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Threading;
using TypeSerializationInfo = DanielWillett.ModularRpcs.SourceGeneration.Util.TypeSerializationInfo;
using TypeSerializationInfoType = DanielWillett.ModularRpcs.SourceGeneration.Util.TypeSerializationInfoType;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct SendMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;
    public readonly CSharpCompilation Compilation;
    public readonly SendMethodInfo Info;

    internal SendMethodSnippetGenerator(SourceProductionContext context, CSharpCompilation compilation, SendMethodInfo method)
    {
        Context = context;
        Method = method.Method;
        Send = (RpcSendAttribute)method.Method.Target;
        Info = method;
        Compilation = compilation;
    }


    public void GenerateMethodBodySnippet(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        EquatableList<RpcParameterDeclaration> parameters = Method.Parameters;

        bool hasId = Method.Type.IdType != null;

        ParameterHelper.BindParameters(parameters, out RpcParameterDeclaration[] toInject, out RpcParameterDeclaration[] toBind);

        bool isReturningTask = !Method.ReturnType.Equals("global::System.Void");

        bool canUnsignedRightShift = Compilation.LanguageVersion >= LanguageVersion.CSharp11;

        bldr.String("global::DanielWillett.ModularRpcs.Reflection.GeneratedSendMethodState __modularRpcsGeneratedState = default;");

        bldr.String("__modularRpcsGeneratedState.Router = this.__modularRpcsGeneratedProxyContext.Router;")
            .String("__modularRpcsGeneratedState.Serializer = this.__modularRpcsGeneratedProxyContext.DefaultSerializer;");

        bldr.String("__modularRpcsGeneratedState.PreCalc = __modularRpcsGeneratedState.Serializer.CanFastReadPrimitives;");

        if (toBind.Length > 0)
        {
            bldr.String("#region Get Size")
                .Empty();

            uint ttl = checked ( (uint)toBind.Sum(parameter => TypeHelper.GetPrimitiveLikeSize(parameter.PrimitiveLikeType)) );

            if (ttl != 0)
            {
                bldr.Build($"__modularRpcsGeneratedState.Size = (__modularRpcsGeneratedState.PreCalc ? 1u : 0u) * {ttl}u;");
            }

            bldr.Empty();

            foreach (RpcParameterDeclaration parameter in toBind)
            {
                if (parameter.PrimitiveLikeType != TypeHelper.PrimitiveLikeType.None)
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

                if (parameter.PrimitiveLikeType != TypeHelper.PrimitiveLikeType.None)
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

        bldr.String("__modularRpcsGeneratedState.OverheadSize = __modularRpcsGeneratedState.Router.GetOverheadSize(")
            .In()
                .Build($"@{Method.Type.Type.Name}.{Info.MethodInfoFieldName}.MethodHandle,")
                .Build($"ref @{Method.Type.Type.Name}.{Info.MethodInfoFieldName}").Out()
            .String(");")
            .Empty();

        // preOverheadSize includes overhead but not ID

        bldr.String("__modularRpcsGeneratedState.PreOverheadSize = __modularRpcsGeneratedState.OverheadSize;");

        string? escapedIdTypeName;

        // calculate ID size
        if (!hasId || Method.Type.IdTypeCode != TypeCode.Object)
        {
            escapedIdTypeName = null;
            bldr.String("__modularRpcsGeneratedState.IdTypeSize = 1u;");
        }
        else
        {
            escapedIdTypeName = TypeHelper.Escape(Method.Type.IdType!.Info.SerializableType.AssemblyQualifiedName!);

            bldr.Build($"if (__modularRpcsGeneratedState.HasKnownTypeId = __modularRpcsGeneratedState.Serializer.TryGetKnownTypeId(typeof({Method.Type.IdType!.GloballyQualifiedName}), out __modularRpcsGeneratedState.KnownTypeId))")
                .String("{").In();
            bldr.String("__modularRpcsGeneratedState.IdTypeSize = 6u;")
                .Out()
                .String("}")
                .String("else")
                .String("{").In()
                .Build($"__modularRpcsGeneratedState.IdTypeSize = 2u + checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSize<string>(\"{escapedIdTypeName}\") );")
                .Out()
                .String("}");
        }

        bldr.String("__modularRpcsGeneratedState.OverheadSize += __modularRpcsGeneratedState.IdTypeSize;");

        bool passIdByRef = false;

        if (hasId && Method.Type.IdType!.Info.Type != TypeSerializationInfoType.Void)
        {
            TypeSerializationInfo idInfo = Method.Type.IdType.Info;

            string idVar = Method.Type.IdIsExplicit
                ? $"((global::DanielWillett.ModularRpcs.Protocol.IRpcObject<{Method.Type.IdType!.GloballyQualifiedName}>)this).Identifier"
                : "this.Identifier";

            bldr.Build($"{Method.Type.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId = {idVar};");

            switch (idInfo.Type)
            {
                case TypeSerializationInfoType.PrimitiveLike:
                case TypeSerializationInfoType.Value:

                    if (idInfo.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                    {
                        switch (idInfo.PrimitiveSerializationMode)
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

                        int size = TypeHelper.GetPrimitiveLikeSize(Method.Type.IdType.PrimitiveType);
                        bldr.String("{")
                            .In()
                            .Build($"__modularRpcsGeneratedState.IdSize = {size}u;")
                            .Out()
                            .String("}");

                        bldr.String("else")
                            .String("{")
                            .In();
                    }
                    else
                    {
                        passIdByRef = Method.Type.IdType.IsValueType;
                    }

                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSize<{Method.Type.IdType.GloballyQualifiedName}>(__modularRpcsGeneratedId) );");

                    if (idInfo.PrimitiveSerializationMode != TypeHelper.QuickSerializeMode.Never)
                    {
                        bldr.Out()
                            .String("}");
                    }

                    break;

                case TypeSerializationInfoType.NullableValue:
                    passIdByRef = true;
                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSize<{idInfo.UnderlyingType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.SerializableValue:
                    passIdByRef = Method.Type.IdType!.IsValueType;
                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializableSize<{idInfo.SerializableType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.NullableSerializableValue:
                    passIdByRef = true;
                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializableSize<{idInfo.SerializableType.GloballyQualifiedName}>(in __modularRpcsGeneratedId) );");
                    break;

                case TypeSerializationInfoType.SerializableCollection:
                case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetSerializablesSize<{idInfo.SerializableType.GloballyQualifiedName}>(__modularRpcsGeneratedId as global::System.Collections.Generic.IEnumerable<{idInfo.SerializableType.GloballyQualifiedName}>) );");
                    break;

                case TypeSerializationInfoType.NullableSerializableCollection:
                case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:
                    bldr.Build($"__modularRpcsGeneratedState.IdSize = checked ( (uint)__modularRpcsGeneratedState.Serializer.GetNullableSerializablesSize<{idInfo.SerializableType.GloballyQualifiedName}>(__modularRpcsGeneratedId as global::System.Collections.Generic.IEnumerable<{idInfo.SerializableType.GloballyQualifiedName}?>) );");
                    break;
            }

            bldr.String("__modularRpcsGeneratedState.OverheadSize += __modularRpcsGeneratedState.IdSize;");
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

        bldr.Build($"return __ModularRpcsGeneratedInvoke(ref __modularRpcsGeneratedState{(hasId ? (passIdByRef ? ", ref __modularRpcsGeneratedId" : ", __modularRpcsGeneratedId") : string.Empty)}{parameterList});");

        bldr.Out()
            .String("}");

        bldr.Out()
            .String("}")
            .String("else")
            .String("{").In();

        // based on a stackalloc
        bldr.String("byte* __modularRpcsGeneratedBuffer = stackalloc byte[checked ( (int)__modularRpcsGeneratedState.Size )];")
            .String("__modularRpcsGeneratedState.Buffer = __modularRpcsGeneratedBuffer;");


        bldr.Build($"return __ModularRpcsGeneratedInvoke(ref __modularRpcsGeneratedState{(hasId ? (passIdByRef ? ", ref __modularRpcsGeneratedId" : ", __modularRpcsGeneratedId") : string.Empty)}{parameterList});");

        bldr.Out()
            .String("}")
            .Out()
            .String("}");

        //foreach (RpcParameterDeclaration parameter in parameters)
        //{
        //    bldr.Build($"global::System.Console.WriteLine(\"{parameter.Name}: {parameter.Type.GloballyQualifiedName}\");");
        //}

        string parameterDefList = parameters.Count == 0 ? string.Empty : (", " + string.Join(", ", parameters.Select(x => x.Definition)));

        bldr.Build($"static unsafe {Method.ReturnType.GloballyQualifiedName} __ModularRpcsGeneratedInvoke(ref global::DanielWillett.ModularRpcs.Reflection.GeneratedSendMethodState __modularRpcsGeneratedState{(hasId ? (passIdByRef ? $", ref {Method.Type.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId" : $", {Method.Type.IdType!.GloballyQualifiedName} __modularRpcsGeneratedId") : string.Empty)}{parameterDefList})")
            .String("{")
            .In();

        if (Method.Type.IdType == null)
        {
            bldr.String("// TypeCode: Empty")
                .String("__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize] = 0;");
        }
        else
        {
            bldr.Build($"#region Write ID ({Method.Type.IdType.Name})")
                .Build($"// TypeCode: {Method.Type.IdTypeCode.ToTypeCodeString()}")
                .Build($"__modularRpcsGeneratedState.Buffer[__modularRpcsGeneratedState.PreOverheadSize] = {(byte)Method.Type.IdTypeCode};")
                .Empty();
                
            if (Method.Type.IdTypeCode == TypeCode.Object)
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

            WriteValue(bldr, Method.Type.IdType, canUnsignedRightShift, "__modularRpcsGeneratedIdPos", "__modularRpcsGeneratedId", "__modularRpcsGeneratedState.IdSize");

            bldr.String("#endregion");
        }

        bldr.Empty();

        if (toBind.Length > 0)
        {
            if (toBind.Length == 1)
                bldr.Build($"#region Write Parameter");
            else
                bldr.Build($"#region Write Parameters ({toBind.Length} parameters)");

            bldr.String("uint __modularRpcsGeneratedIndex = __modularRpcsGeneratedIdPos;");

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

        string? injectedConnectionArg = null;
        string? cancellationTokenArg = null;
        foreach (RpcParameterDeclaration param in toInject)
        {
            if (param.RefKind == RefKind.Out)
                continue;

            if (param.Type.Equals("global::System.Threading.CancellationToken"))
            {
                cancellationTokenArg = param.Name;
                continue;
            }

            //if (param.Type.Info.Type)
            //{
            //
            //}
        }

        bldr.String("");

        bldr.String("return default;");

        bldr.Out()
            .String("}");
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
                                bldr.Build($"__modularRpcsGeneratedState.Buffer[{offsetVar}] = (bool){valueVar} ? (byte)1 : (byte)0;;");
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
                            bldr.In().Build($"*({symbolInfo.GloballyQualifiedName}*){bufferName} = {valueVar};").Out()
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

internal struct SendMethodInfo
{
    public RpcMethodDeclaration Method;
    public int Overload;
    public string MethodInfoFieldName;

    public SendMethodInfo(RpcMethodDeclaration method, int overload, string methodInfoFieldName)
    {
        Method = method;
        Overload = overload;
        MethodInfoFieldName = methodInfoFieldName;
    }
}