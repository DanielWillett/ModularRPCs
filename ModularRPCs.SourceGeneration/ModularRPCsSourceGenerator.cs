using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Protocol;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModularRPCs.Generators;
using ModularRPCs.Util;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModularRPCs;

[Generator(LanguageNames.CSharp)]
public class ModularRPCsSourceGenerator
#if ROSLYN_4_0_OR_GREATER
    : IIncrementalGenerator
#else
    : ISourceGenerator
#endif
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Initialize(
#if ROSLYN_4_0_OR_GREATER
        IncrementalGeneratorInitializationContext context
#else
        GeneratorInitializationContext context
#endif
    )
    {
#if ROSLYN_4_0_OR_GREATER
        IncrementalValuesProvider<RpcClassDeclaration> symbolsAndClassDefs =
#if ROSLYN_4_3_OR_GREATER
            context.SyntaxProvider.ForAttributeWithMetadataName(
                "DanielWillett.ModularRpcs.Annotations.GenerateRpcSourceAttribute",
                static (n, _) => n is ClassDeclarationSyntax or StructDeclarationSyntax,
#else // ROSLYN_4_3_OR_GREATER
            context.SyntaxProvider.CreateSyntaxProvider(
                static (n, _) => n is ClassDeclarationSyntax or StructDeclarationSyntax,
#endif
                ProcessNodes
            ).Where(x => x != null)!
#if ROSLYN_4_2_OR_GREATER
             .WithTrackingName<RpcClassDeclaration>("ModularRPCs_AllGeneratedRpcClasses")
#endif
            ;

        context.RegisterSourceOutput(symbolsAndClassDefs.Combine(context.CompilationProvider), static (n, c) =>
        {
            new ClassSnippetGenerator(n, (CSharpCompilation)c.Right, c.Left).GenerateClassSnippet();
        });
#else // ROSLYN_4_0_OR_GREATER
        context.RegisterForSyntaxNotifications(static () => new Roslyn3SyntaxContextReceiver());
#endif
    }

#if !ROSLYN_4_0_OR_GREATER

    // Roslyn 3 Compatability for older Unity versions
    // inspired by System.Text.Json's implementation
    // https://github.com/dotnet/dotnet/blob/main/src/runtime/src/libraries/System.Text.Json/gen/JsonSourceGenerator.Roslyn3.11.cs

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not Roslyn3SyntaxContextReceiver contextReceiver || contextReceiver.Classes == null)
            return;

        List<Exception>? exceptions = null;
        for (int i = 0; i < contextReceiver.Classes.Count; i++)
        {
            (INamedTypeSymbol namedType, SemanticModel semanticModel) = contextReceiver.Classes[i];
            try
            {
                RpcClassDeclaration? declaration = ProcessNodes(namedType, semanticModel, CancellationToken.None);
                if (declaration == null || declaration.Error != ClassError.None)
                    continue;

                ClassSnippetGenerator generator =
                    new ClassSnippetGenerator(context, (CSharpCompilation)context.Compilation, declaration);
                generator.GenerateClassSnippet();
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        if (exceptions != null)
        {
            throw new AggregateException("Failed to generate source for some files.", exceptions);
        }
    }

    private sealed class Roslyn3SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public List<(INamedTypeSymbol, SemanticModel)>? Classes;

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not TypeDeclarationSyntax typeDef || typeDef is not StructDeclarationSyntax and not ClassDeclarationSyntax)
                return;

            if (!typeDef.DefinesAttribute(
                    context.SemanticModel,
                    "global::DanielWillett.ModularRpcs.Annotations.GenerateRpcSourceAttribute",
                    CancellationToken.None
                ))
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(typeDef) is not { } namedType)
            {
                return;
            }

            Classes ??= new List<(INamedTypeSymbol, SemanticModel)>();
            Classes.Add((namedType, context.SemanticModel));
        }
    }
#endif

#if ROSLYN_4_3_OR_GREATER
    private static RpcClassDeclaration? ProcessNodes(GeneratorAttributeSyntaxContext n, CancellationToken token)
#elif ROSLYN_4_0_OR_GREATER
    private static RpcClassDeclaration? ProcessNodes(GeneratorSyntaxContext n, CancellationToken token)
#else
    private static RpcClassDeclaration? ProcessNodes(INamedTypeSymbol symbol, SemanticModel semanticModel, CancellationToken token)
#endif
    {
#if ROSLYN_4_3_OR_GREATER
        SemanticModel semanticModel = n.SemanticModel;
        INamedTypeSymbol? symbol = n.TargetSymbol as INamedTypeSymbol;
#elif ROSLYN_4_0_OR_GREATER
        SemanticModel semanticModel = n.SemanticModel;

        TypeDeclarationSyntax node = (TypeDeclarationSyntax)n.Node;
        if (!node.DefinesAttribute(n.SemanticModel, "global::DanielWillett.ModularRpcs.Annotations.GenerateRpcSourceAttribute", token))
        {
            return null;
        }

        INamedTypeSymbol? symbol = n.SemanticModel.GetDeclaredSymbol(node, token);
#endif
        if (symbol == null || symbol.HasAttribute("global::DanielWillett.ModularRpcs.Annotations.IgnoreGenerateRpcSourceAttribute"))
        {
            return null;
        }

        ClassError error = ClassError.None;
        if (symbol.IsStatic)
            error = ClassError.Static;
        else if (!symbol.CanBeReferencedByName)
            error = ClassError.Invalid;
        else if (symbol.IsValueType)
            error = ClassError.Struct;

        if (error != ClassError.None)
        {
            return null;
        }

        INamedTypeSymbol? rpcObjectInterface = symbol.GetImplementation(
            x => x.IsGenericType && x.ConstructedFrom.IsEqualTo("global::DanielWillett.ModularRpcs.Protocol.IRpcObject<T>")
        );
        INamedTypeSymbol? singleConnectionInterface = symbol.GetImplementation(
            x => !x.IsGenericType && x.ConstructedFrom.IsEqualTo("global::DanielWillett.ModularRpcs.Protocol.IRpcSingleConnectionObject")
        );
        INamedTypeSymbol? multiConnectionInterface = symbol.GetImplementation(
            x => !x.IsGenericType && x.ConstructedFrom.IsEqualTo("global::DanielWillett.ModularRpcs.Protocol.IRpcMultipleConnectionsObject")
        );

        ITypeSymbol? idType = rpcObjectInterface?.TypeArguments[0];

        bool explicitId = rpcObjectInterface.IsExplicitlyImplemented<IPropertySymbol>(symbol, nameof(IRpcObject<>.Identifier));
        bool explicitSingle = singleConnectionInterface.IsExplicitlyImplemented<IPropertySymbol>(symbol, nameof(IRpcSingleConnectionObject.Connection));
        bool explicitMulti = multiConnectionInterface.IsExplicitlyImplemented<IPropertySymbol>(symbol, nameof(IRpcMultipleConnectionsObject.Connections));

        IPropertySymbol? idProperty = rpcObjectInterface?.GetMembers(nameof(IRpcObject<>.Identifier)).OfType<IPropertySymbol>().FirstOrDefault();
        bool idIsInBaseType = idProperty != null && !SymbolEqualityComparer.Default.Equals(symbol, symbol.FindImplementationForInterfaceMember(idProperty)?.ContainingType);

        bool isUnityType = false;
        bool hasReleaseMethod = false;
        int hasExplicitFinalizer = 0;

        TypeSymbolInfo? inherited = null;

        for (ITypeSymbol? baseType = symbol; baseType != null; baseType = baseType.BaseType)
        {
            if (!ReferenceEquals(baseType, symbol)
                && baseType.IsEqualTo("global::UnityEngine.MonoBehaviour"))
            {
                isUnityType = true;
            }

            if (!ReferenceEquals(baseType, symbol)
                && inherited == null
                && baseType.HasAttribute("global::DanielWillett.ModularRpcs.Annotations.GenerateRpcSourceAttribute"))
            {
                inherited = new TypeSymbolInfo(semanticModel.Compilation, baseType);
            }

            if (hasExplicitFinalizer == 0 && idType != null)
            {
                INamedTypeSymbol? explicitFinalizerInterface = baseType.AllInterfaces.FirstOrDefault(x =>
                    !x.IsGenericType && x.IsEqualTo("global::DanielWillett.ModularRpcs.Protocol.IExplicitFinalizerRpcObject")
                );

                if (explicitFinalizerInterface != null)
                {
                    hasExplicitFinalizer = 1 + (explicitFinalizerInterface.IsExplicitlyImplemented<IMethodSymbol>(symbol, nameof(IExplicitFinalizerRpcObject.OnFinalizing)) ? 1 : 0);
                }
            }

            if (hasReleaseMethod || idType == null)
                continue;

            IMethodSymbol? method = baseType
                .GetMembers("Release")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(x => x.Parameters.Length == 0 && !x.IsStatic && !x.HasAttribute("global::DanielWillett.ReflectionTools.IgnoreAttribute"));
            
            if (method != null)
                hasReleaseMethod = true;
        }

        RpcClassDeclaration cls = new RpcClassDeclaration
        {
            Error = error,
            Definition = symbol.ToDisplayString(CustomFormats.TypeDeclarationFormat),
            IsValueType = symbol.IsValueType,
            Type = new TypeSymbolInfo(semanticModel.Compilation, symbol),
            IdType = idType == null ? null : new TypeSymbolInfo(semanticModel.Compilation, idType, createInfo: true),
            IdTypeCode = TypeHelper.GetTypeCode(idType),
            IdIsExplicit = explicitId,
            IsUnityType = isUnityType,
            IsSingleConnectionObject = singleConnectionInterface != null, 
            IsSingleConnectionExplicit = explicitSingle,
            IsMultipleConnectionObject = multiConnectionInterface != null,
            IsMultipleConnectionExplicit = explicitMulti, 
            NestedParents = symbol.ContainingType == null ? null : GetNestedParents(symbol),
            HasReleaseMethod = hasReleaseMethod,
            HasExplicitFinalizer = hasExplicitFinalizer > 0,
            IsExplicitFinalizerExplicit = hasExplicitFinalizer == 2,
            IsSealed = symbol.IsSealed,
            InheritedGeneratedType = inherited,
            IdIsInBaseType = idIsInBaseType && inherited != null
        };

        cls.Methods = new EquatableList<RpcMethodDeclaration>(EnumerateMethods(semanticModel.Compilation, symbol, cls, token));
        return cls;
    }

    private static EquatableList<string> GetNestedParents(INamedTypeSymbol symbol)
    {
        EquatableList<string> list = new EquatableList<string>();
        for (INamedTypeSymbol? containingType = symbol.ContainingType; containingType != null; containingType = containingType.ContainingType)
        {
            list.Add(containingType.ToDisplayString(CustomFormats.TypeDeclarationFormat));
        }

        list.Reverse();
        return list;
    }

    private static IEnumerable<RpcMethodDeclaration> EnumerateMethods(Compilation compilation, INamedTypeSymbol symbol, RpcClassDeclaration decl, CancellationToken token)
    {
        List<IMethodSymbol> methods = symbol.GetMembers().OfType<IMethodSymbol>().ToList();
        foreach (IMethodSymbol method in methods)
        {
            RpcTargetAttribute? attribute = method.GetAttributes()
                                                  .Select(a => GetRpcTypeAttribute(compilation, a, symbol))
                                                  .FirstOrDefault(x => x != null);
            if (attribute == null
                || attribute is RpcReceiveAttribute && method.HasAttribute("global::DanielWillett.ReflectionTools.IgnoreAttribute"))
            {
                continue;
            }

            token.ThrowIfCancellationRequested();

            bool isFireAndForget = false;
            if (method.ReturnType.IsEqualTo("global::DanielWillett.ModularRpcs.Async.RpcTask"))
            {
                isFireAndForget = method.GetAttributes().Any(
                    x => x.AttributeClass.IsEqualTo("global::DanielWillett.ModularRpcs.Annotations.RpcFireAndForgetAttribute")
                );
            }
            else if (method.ReturnType.IsEqualTo("global::System.Void"))
            {
                isFireAndForget = true;
            }

            EquatableList<RpcParameterDeclaration> parameters = new EquatableList<RpcParameterDeclaration>(method.Parameters.Select((arg, index) =>
            {
                TypeSymbolInfo symbolInfo = new TypeSymbolInfo(compilation, arg.Type, createInfo: true, isByRef: arg.RefKind != RefKind.None);
                return new RpcParameterDeclaration
                {
                    Index = index,
                    Name = arg.Name,
                    Type = symbolInfo,
                    InjectType = ParameterHelper.GetAutoInjectType(arg.Type),
                    IsManualInjected = arg.GetAttributes().Any(
                        x => x.AttributeClass.IsEqualTo("global::DanielWillett.ModularRpcs.Annotations.RpcInjectAttribute")
                    ),
                    Definition = arg.ToDisplayString(CustomFormats.MethodDeclarationFormat),
                    RefKind = arg.RefKind,
                    RawInjectType = ParameterHelper.GetRawByteInjectType(arg)
                };
            }).Where(x => x != null));

            bool isBroadcast = string.IsNullOrEmpty(attribute.MethodName);
            
            // check if targets self
            if (!isBroadcast)
            {
                if ((attribute.TypeName == null || TypeHelper.TypesEqual(attribute.TypeName.AsSpan(), decl.Type.AssemblyQualifiedName.AsSpan()))
                    && string.Equals(attribute.MethodName, method.Name, StringComparison.Ordinal)
                    && (attribute.ParameterTypeNames == null || ParametersMatchMethod(attribute.ParameterTypeNames, parameters, attribute.ParametersAreBindedParametersOnly)))
                {
                    isBroadcast = true;
                }
            }

            bool injectsCancellationToken = parameters.Exists(x => x.Type.Equals("global::System.Threading.CancellationToken"));

            yield return new RpcMethodDeclaration
            {
                Target = attribute,
                Visibility = method.DeclaredAccessibility,
                IsStatic = method.IsStatic,
                Name = method.Name,
                Definition = CustomFormats.InsertPartial(method.ToDisplayString(CustomFormats.MethodDeclarationFormat)),
                XmlDocs = CustomFormats.GetXmlDocReference(method),
                DisplayString = method.ToDisplayString(CustomFormats.MethodDisplayFormat),
                Parameters = parameters,
                ReturnType = new TypeSymbolInfo(compilation, method.ReturnType, createInfo: true),
                IsFireAndForget = isFireAndForget,
                IsBroadcast = isBroadcast,
                SignatureHash = ParameterHelper.GetMethodSignatureHash(compilation, method, parameters),
                ForceSignatureCheck = method.HasAttribute("global::DanielWillett.ModularRpcs.Annotations.RpcForceSignatureCheckAttribute"),
                Timeout = GetTimeout(method),
                ReturnTypeAwaitableInfo = attribute is RpcReceiveAttribute ? ParameterHelper.GetAwaitableInfo(compilation, method.ReturnType) : null,
                NeedsUnsafe = method.Parameters.Any(x => x.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer),
                DelegateType = new DelegateType(method),
                InjectsCancellationToken = injectsCancellationToken,
                NeedsSignatureCheck = methods.Exists(x => !ReferenceEquals(method, x) && x.Name.Equals(method.Name, StringComparison.Ordinal))
            };
        }
    }

    private static bool ParametersMatchMethod(string[] parameterTypeNames, EquatableList<RpcParameterDeclaration> parameters, bool parametersAreBindedParametersOnly)
    {
        IList<RpcParameterDeclaration> toBind;
        if (parametersAreBindedParametersOnly)
        {
            ParameterHelper.BindParameters(parameters, out _, out RpcParameterDeclaration[] toBindArr);
            toBind = toBindArr;
        }
        else
            toBind = parameters;

        for (int i = 0; i < toBind.Count; ++i)
        {
            RpcParameterDeclaration p = toBind[i];
            string typeName = parameterTypeNames[i];
            
            if (TypeHelper.TypesEqual(typeName.AsSpan(), p.Type.AssemblyQualifiedName.AsSpan()))
                continue;

            return false;
        }

        return true;
    }

    private static TimeSpan GetTimeout(IMethodSymbol method)
    {
        AttributeData? attribute = method.GetAttribute("global::DanielWillett.ModularRpcs.Annotations.RpcTimeoutAttribute");
        attribute ??= method.ContainingType.GetAttribute("global::DanielWillett.ModularRpcs.Annotations.RpcTimeoutAttribute");
        if (attribute == null || attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not int i)
            return TimeSpan.Zero;

        return i < 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(i);
    }

    private static RpcTargetAttribute? GetRpcTypeAttribute(Compilation compilation, AttributeData attributeData, INamedTypeSymbol owner)
    {
        bool isSend = attributeData.AttributeClass.IsEqualTo("global::DanielWillett.ModularRpcs.Annotations.RpcSendAttribute");
        bool isReceive = attributeData.AttributeClass.IsEqualTo("global::DanielWillett.ModularRpcs.Annotations.RpcReceiveAttribute");
        if (!isSend && !isReceive)
            return null;

        AttributeData? rpcClassAttribute = owner.GetAttribute("global::DanielWillett.ModularRpcs.Annotations.RpcDefaultTargetTypeAttribute");
        string? defaultType;
        if (rpcClassAttribute is { ConstructorArguments.Length: 1 })
        {
            TypedConstant defaultTypeProp = rpcClassAttribute.ConstructorArguments[0];
            defaultType = defaultTypeProp.Kind switch
            {
                TypedConstantKind.Type when defaultTypeProp.Value is ITypeSymbol defaultTypePropValue
                    => TypeHelper.GetAssemblyQualifiedNameNoVersion(compilation, defaultTypePropValue),

                TypedConstantKind.Primitive when defaultTypeProp.Value is string defaultTypeNameValue
                    => defaultTypeNameValue,

                _ => null
            };
        }
        else
        {
            defaultType = null;
        }

        RpcTargetAttribute attribute;
        if (attributeData.AttributeConstructor == null || attributeData.AttributeConstructor.Parameters.Length == 0)
        {
            attribute = isSend ? new RpcSendAttribute() : new RpcReceiveAttribute();
        }
        else if (attributeData.AttributeConstructor.Parameters.Length == 1)
        {
            TypedConstant methodName = attributeData.ConstructorArguments[0];
            if (methodName.Kind != TypedConstantKind.Primitive || methodName.Type is not { SpecialType: SpecialType.System_String })
            {
                return null;
            }

            string? methodNameStr = (string?)methodName.Value;

            if (methodNameStr == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(defaultType))
                attribute = isSend ? new RpcSendAttribute(defaultType!, methodNameStr) : new RpcReceiveAttribute(defaultType!, methodNameStr);
            else
                attribute = isSend ? new RpcSendAttribute(methodNameStr) : new RpcReceiveAttribute(methodNameStr);
        }
        else if (attributeData.AttributeConstructor.Parameters.Length == 2)
        {
            TypedConstant declaringType = attributeData.ConstructorArguments[0];
            TypedConstant methodName = attributeData.ConstructorArguments[1];
            if (methodName.Kind != TypedConstantKind.Primitive || methodName.Type is not { SpecialType: SpecialType.System_String })
            {
                return null;
            }
            if (declaringType.Kind != TypedConstantKind.Primitive || declaringType.Type is not { SpecialType: SpecialType.System_String })
            {
                if (declaringType.Kind != TypedConstantKind.Type)
                    return null;
            }

            string? typeName = declaringType.Kind == TypedConstantKind.Type
                ? TypeHelper.GetAssemblyQualifiedNameNoVersion(compilation, (ITypeSymbol)declaringType.Value!)
                : (string)declaringType.Value!;

            string? methodNameStr = (string?)methodName.Value;

            typeName ??= defaultType;
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(methodNameStr))
            {
                return null;
            }

            attribute = isSend ? new RpcSendAttribute(typeName!, methodNameStr!) : new RpcReceiveAttribute(typeName!, methodNameStr!);
        }
        else
        {
            return null;
        }

        KeyValuePair<string, TypedConstant> isRaw = attributeData.NamedArguments.FirstOrDefault(
            x => x.Key.Equals(nameof(RpcTargetAttribute.Raw), StringComparison.Ordinal)
        );
        if (isRaw.Key is not null && isRaw.Value is { Kind: TypedConstantKind.Primitive, Value: bool isRawVal })
        {
            attribute.Raw = isRawVal;
        }

        KeyValuePair<string, TypedConstant> paramsAreBindedOnly = attributeData.NamedArguments.FirstOrDefault(
            x => x.Key.Equals(nameof(RpcTargetAttribute.ParametersAreBindedParametersOnly), StringComparison.Ordinal)
        );
        if (paramsAreBindedOnly.Key is not null && paramsAreBindedOnly.Value is { Kind: TypedConstantKind.Primitive, Value: bool paramsAreBindedOnlyVal })
        {
            attribute.ParametersAreBindedParametersOnly = paramsAreBindedOnlyVal;
        }

        KeyValuePair<string, TypedConstant> parameterTypes = attributeData.NamedArguments.FirstOrDefault(
            x => x.Key.Equals(nameof(RpcTargetAttribute.ParameterTypes), StringComparison.Ordinal)
        );
        if (parameterTypes.Key is not null && parameterTypes.Value is { Kind: TypedConstantKind.Array, IsNull: false })
        {
            ImmutableArray<TypedConstant> arr = parameterTypes.Value.Values;
            attribute.ParameterTypeNames = arr
                .Where(x => x is { Kind: TypedConstantKind.Type, Value: ITypeSymbol })
                .Select(x => TypeHelper.GetAssemblyQualifiedNameNoVersion(compilation, (ITypeSymbol)x.Value!))
                .ToArray();
        }

        KeyValuePair<string, TypedConstant> parameterNames = attributeData.NamedArguments.FirstOrDefault(
            x => x.Key.Equals(nameof(RpcTargetAttribute.ParameterTypeNames), StringComparison.Ordinal)
        );
        if (parameterNames.Key is not null && parameterNames.Value is { Kind: TypedConstantKind.Array, IsNull: false })
        {
            ImmutableArray<TypedConstant> arr = parameterNames.Value.Values;
            attribute.ParameterTypeNames = arr
                .Where(x => x is { Kind: TypedConstantKind.Primitive, Value: string })
                .Select(x => (string)x.Value!)
                .ToArray();
        }

        return attribute;
    }
}

internal enum RpcAttributeType
{
    RpcSend,
    RpcReceive,
    RpcTimeout
}

public enum ClassError
{
    None,
    Static,
    Invalid,
    Struct
}

public record RpcClassDeclaration
{
    public required TypeSymbolInfo Type { get; init; }
    public required string Definition { get; init; }
    public required ClassError Error { get; init; }
    public required bool IsValueType { get; init; }
    public required bool IsSingleConnectionObject { get; init; }
    public required bool IsSingleConnectionExplicit { get; set; }
    public required bool IsMultipleConnectionObject { get; init; }
    public required bool IsMultipleConnectionExplicit { get; set; }
    public required EquatableList<string>? NestedParents { get; set; }
    public required bool IsSealed { get; init; }
    public TypeSymbolInfo? InheritedGeneratedType { get; init; }
    public EquatableList<RpcMethodDeclaration> Methods { get; set; }

    public bool IsUnityType { get; init; }
    public bool HasExplicitFinalizer { get; init; }
    public bool IsExplicitFinalizerExplicit { get; init; }


    /// <summary>
    /// <see langword="null"/> if not an <see cref="IRpcObject{T}"/>.
    /// </summary>
    public TypeSymbolInfo? IdType { get; init; }
    public bool IdIsExplicit { get; init; }
    public bool IdIsInBaseType { get; init; }
    public TypeCode IdTypeCode { get; init; }
    public bool HasReleaseMethod { get; init; }
}