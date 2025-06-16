using System;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace DanielWillett.ModularRpcs.SourceGeneration.Generators;

internal readonly struct SendMethodSnippetGenerator
{
    public readonly SourceProductionContext Context;
    public readonly RpcMethodDeclaration Method;
    public readonly RpcSendAttribute Send;
    public readonly SendMethodInfo Info;

    internal SendMethodSnippetGenerator(SourceProductionContext context, SendMethodInfo method)
    {
        Context = context;
        Method = method.Method;
        Send = (RpcSendAttribute)method.Method.Target;
        Info = method;
    }


    public void GenerateMethodBodySnippet(SourceStringBuilder bldr)
    {
        Context.CancellationToken.ThrowIfCancellationRequested();

        EquatableList<RpcParameterDeclaration> parameters = Method.Parameters;

        ParameterHelper.BindParameters(parameters, out RpcParameterDeclaration[] toInject, out RpcParameterDeclaration[] toBind);

        bool isReturningTask = !Method.ReturnType.Equals("global::System.Void");

        bldr.String("global::DanielWillett.ModularRpcs.Routing.IRpcRouter __modularRpcsGeneratedRouter = this.__modularRpcsGeneratedProxyContext.Router;")
            .String("global::DanielWillett.ModularRpcs.Serialization.IRpcSerializer __modularRpcsGeneratedSerializer = this.__modularRpcsGeneratedProxyContext.DefaultSerializer;");

        bldr.String("bool __modularRpcsGeneratedPreCalc = __modularRpcsGeneratedSerializer.CanFastReadPrimitives;");

        if (toBind.Length > 0)
        {
            bldr.String("#region Get Size")
                .Empty();

            uint ttl = checked ( (uint)toBind.Sum(parameter => TypeHelper.GetPrimitiveLikeSize(parameter.PrimitiveLikeType)) );

            if (ttl == 0)
            {
                bldr.String("uint __modularRpcsGeneratedSize = 0;");
            }
            else
            {
                bldr.Build($"uint __modularRpcsGeneratedSize = (__modularRpcsGeneratedPreCalc ? 1u : 0u) * {ttl}u;");
            }

            bldr.Empty();

            foreach (RpcParameterDeclaration parameter in toBind)
            {
                if (parameter.PrimitiveLikeType != TypeHelper.PrimitiveLikeType.None)
                {
                    bldr.String("if (!__modularRpcsGeneratedPreCalc)")
                        .String("{").In();
                }

                TypeSerializationInfo info = parameter.Type.Info;
                switch (info.Type)
                {
                    case TypeSerializationInfoType.Value:
                    case TypeSerializationInfoType.PrimitiveLike:
                        bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSize<{
                            parameter.Type.GloballyQualifiedName}>(@{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.NullableValue:
                        bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSize<{
                            info.UnderlyingType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.SerializableValue:
                        bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSerializableSize<{
                            info.SerializableType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.NullableSerializableValue:
                        bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetNullableSerializableSize<{
                            info.SerializableType.GloballyQualifiedName}>(in @{parameter.Name}) );");
                        break;

                    case TypeSerializationInfoType.SerializableCollection:
                    case TypeSerializationInfoType.NullableSerializableCollection:
                        if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        {
                            bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetNullableSerializablesSize<{
                                info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}) );");
                        }
                        else
                        {
                            bldr.Build($"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSerializablesSize<{
                                info.SerializableType.GloballyQualifiedName}>(@{parameter.Name}) );");
                        }
                        break;

                    case TypeSerializationInfoType.NullableCollectionSerializableCollection:
                    case TypeSerializationInfoType.NullableCollectionNullableSerializableCollection:

                        if (info.Type == TypeSerializationInfoType.NullableSerializableCollection)
                        {
                            bldr.Build($"if (@{parameter.Name}.HasValue)")
                                .In().Build(
                                    $"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetNullableSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}.Value) );").Out()
                                .Build($"else")
                                .In().Build(
                                    $"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetNullableSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(null) );").Out();
                        }
                        else
                        {
                            bldr.Build($"if (@{parameter.Name}.HasValue)")
                                .In().Build(
                                    $"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSerializablesSize<{
                                        info.UnderlyingType.GloballyQualifiedName}>(@{parameter.Name}.Value) );").Out()
                                .Build($"else")
                                .In().Build(
                                    $"__modularRpcsGeneratedSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSerializablesSize<{
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
        else
        {
            bldr.String("uint __modularRpcsGeneratedSize = 0;");
        }

        bldr.String("uint __modularRpcsGeneratedOverheadSize = __modularRpcsGeneratedRouter.GetOverheadSize(")
            .In()
                .Build($"@{Method.Type.Type.Name}.{Info.MethodInfoFieldName}.MethodHandle,")
                .Build($"ref @{Method.Type.Type.Name}.{Info.MethodInfoFieldName}").Out()
            .String(");")
            .Empty();

        // preOverheadSize includes overhead but not ID

        bldr.String("uint __modularRpcsGeneratedPreOverheadSize = __modularRpcsGeneratedOverheadSize;");

        // calculate ID size
        if (Method.Type.IdTypeCode != TypeCode.Object)
        {
            bldr.String("const uint __modularRpcsGeneratedIdTypeSize = 1u;")
                .String("const uint __modularRpcsGeneratedKnownTypeId = 0u;")
                .String("const bool __modularRpcsGeneratedHasKnownTypeId = false;");
        }
        else
        {
            bldr.String("uint __modularRpcsGeneratedIdTypeSize;")
                .String("bool __modularRpcsGeneratedHasKnownTypeId;");

            bldr.Build($"if (__modularRpcsGeneratedHasKnownTypeId = __modularRpcsGeneratedSerializer.TryGetKnownTypeId(typeof({Method.Type.IdType!.GloballyQualifiedName}), out uint __modularRpcsGeneratedKnownTypeId))")
                .String("{").In();
            bldr.String("__modularRpcsGeneratedIdTypeSize = 6u;")
                .Out()
                .String("}")
                .String("else")
                .String("{").In()
                .Build($"__modularRpcsGeneratedIdTypeSize = 2u + checked ( (uint)__modularRpcsGeneratedSerializer.GetSize<string>(\"{TypeHelper.Escape(Method.Type.IdAssemblyQualifiedName!)}\") );")
                .Out()
                .String("}");
        }

        bldr.String("__modularRpcsGeneratedOverheadSize += __modularRpcsGeneratedIdTypeSize;");


        if (Method.Type.IdType != null)
        {
            TypeHelper.QuickSerializeMode quickSerialize = Method.Type.IdQuickSerialize;

            if (quickSerialize != TypeHelper.QuickSerializeMode.Never)
            {
                switch (quickSerialize)
                {
                    case TypeHelper.QuickSerializeMode.If64Bit:
                    case TypeHelper.QuickSerializeMode.If64BitLittleEndian:
                        bldr.String("if (__modularRpcsGeneratedPreCalc && global::System.IntPtr.Size == 8)");
                        break;
                    default: // Always, LittleEndian ( size will be the same on big-endian machines )
                        bldr.String("if (__modularRpcsGeneratedPreCalc)");
                        break;
                }

                int size = TypeHelper.GetPrimitiveLikeSize(Method.Type.IdPrimitiveLikeType);
                bldr.String("{")
                    .In()
                    .Build($"__modularRpcsGeneratedOverheadSize += {size}u;")
                    .Out()
                    .String("}");

                bldr.String("else")
                    .String("{")
                    .In();
            }

            bldr.Build($"__modularRpcsGeneratedOverheadSize += checked ( (uint)__modularRpcsGeneratedSerializer.GetSize<{
                Method.Type.IdType!.GloballyQualifiedName}>(");

            if (Method.Type.IdIsExplicit)
            {
                bldr.In().Build($"((global::DanielWillett.ModularRpcs.Protocol.IRpcObject<{Method.Type.IdType!.GloballyQualifiedName}>)this).Identifier) );").Out();
            }
            else
            {
                bldr.In().String("this.Identifier) );").Out();
            }

            if (quickSerialize != TypeHelper.QuickSerializeMode.Never)
            {
                bldr.Out()
                    .String("}");
            }
        }

        bldr.String("__modularRpcsGeneratedSize += __modularRpcsGeneratedOverheadSize;");


        bldr.String("unsafe")
            .String("{")
            .In();

        // based on an array
        bldr.String("if (__modularRpcsGeneratedSize > this.__modularRpcsGeneratedProxyContext.Generator.MaxSizeForStackalloc)")
            .String("{")
            .In();

        bldr.String("byte[] __modularRpcsGeneratedBufferArray = new byte[__modularRpcsGeneratedSize];")
            .String("fixed (byte* __modularRpcsGeneratedBuffer = __modularRpcsGeneratedBufferArray)")
            .String("{")
            .In();

        string parameterList = parameters.Count == 0 ? string.Empty : (", " + string.Join(", ", parameters.Select(x => $"@{x.Name}")));

        bldr.Build($"return __ModularRpcsGeneratedInvoke(__modularRpcsGeneratedBuffer, __modularRpcsGeneratedSize, __modularRpcsGeneratedOverheadSize, __modularRpcsGeneratedPreOverheadSize{parameterList});");

        bldr.Out()
            .String("}");

        bldr.Out()
            .String("}")
            .String("else")
            .String("{").In();

        // based on a stackalloc
        bldr.String("byte* __modularRpcsGeneratedBuffer = stackalloc byte[checked ( (int)__modularRpcsGeneratedSize )];");

        bldr.Build($"return __ModularRpcsGeneratedInvoke(__modularRpcsGeneratedBuffer, __modularRpcsGeneratedSize, __modularRpcsGeneratedOverheadSize, __modularRpcsGeneratedPreOverheadSize{parameterList});");

        bldr.Out()
            .String("}")
            .Out()
            .String("}");

        //foreach (RpcParameterDeclaration parameter in parameters)
        //{
        //    bldr.Build($"global::System.Console.WriteLine(\"{parameter.Name}: {parameter.Type.GloballyQualifiedName}\");");
        //}

        string parameterDefList = parameters.Count == 0 ? string.Empty : (", " + string.Join(", ", parameters.Select(x => x.Definition)));

        bldr.Build($"static unsafe {Method.ReturnType.GloballyQualifiedName} __ModularRpcsGeneratedInvoke(byte* __modularRpcsGeneratedBuffer, uint __modularRpcsGeneratedSize, uint __modularRpcsGeneratedOverheadSize, uint __modularRpcsGeneratedPreOverheadSize{parameterDefList})")
            .String("{")
            .In();

        bldr.String("int __modularRpcsGeneratedIdSize;");

        if (Method.Type.IdType == null)
        {
            bldr.String("__modularRpcsGeneratedBuffer[__modularRpcsGeneratedPreOverheadSize] = 0;")
                .String("__modularRpcsGeneratedIdSize = 1;");
        }

        bldr.Empty();

        bldr.String("return default;");

        bldr.Out()
            .String("}");
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