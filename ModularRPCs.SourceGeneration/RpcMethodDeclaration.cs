using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.SourceGeneration.Util;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace DanielWillett.ModularRpcs.SourceGeneration;

public record RpcMethodDeclaration
{
    public required string Name { get; init; }
    public required string Definition { get; init; }
    public required string XmlDocs { get; init; }
    public required string DisplayString { get; init; }
    public required bool IsFireAndForget { get; init; }
    public required bool IsBroadcast { get; init; }
    public bool IsReceive => Target is RpcReceiveAttribute;
    public bool IsSend => Target is RpcSendAttribute;
    public required bool IsStatic { get; init; }
    public required Accessibility Visibility { get; init; }
    public required RpcTargetAttribute Target { get; init; }
    public required RpcClassDeclaration Type { get; init; }
    public required EquatableList<RpcParameterDeclaration> Parameters { get; init; }
    public required TypeSymbolInfo ReturnType { get; init; }
    public required int SignatureHash { get; init; }
    public required bool ForceSignatureCheck { get; init; }
    public required TimeSpan Timeout { get; init; }
    public required AwaitableInfo? ReturnTypeAwaitableInfo { get; init; }
    public required bool NeedsUnsafe { get; init; }
    public required DelegateType DelegateType { get; init; }
}

public sealed class DelegateType : IEquatable<DelegateType>
{
    internal int Methods;

    public PredefinedDelegateType Predefined { get; }
    public string? ReturnType { get; }
    public RefKind ReturnRefKind { get; }
    public EquatableList<DelegateParameter>? Parameters { get; }
    public string Name { get; }
    public DelegateType(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.ReturnsByRef && !methodSymbol.ReturnsByRefReadonly &&
            (methodSymbol.Parameters.Length == 0
             || !methodSymbol.Parameters.Any(x => x.RefKind != RefKind.None
                                                  || x.IsParams
                                                  || x.ScopedKind != ScopedKind.None
                                                  || x.HasAttribute("global::System.Diagnostics.CodeAnalysis.UnscopedRefAttribute")
                                                  || !x.Type.CanBeGenericArgument()
                                                )
             )
            )
        {
            if (methodSymbol.Parameters.Length is >= 0 and <= 16)
            {
                Predefined = methodSymbol.ReturnType.SpecialType == SpecialType.System_Void
                    ? PredefinedDelegateType.Action0 + methodSymbol.Parameters.Length
                    : PredefinedDelegateType.Func0 + methodSymbol.Parameters.Length;
                Name = methodSymbol.ReturnType.SpecialType == SpecialType.System_Void
                    ? "Action"
                    : "Func";
                return;
            }
        }

        ReturnType = methodSymbol.ReturnType.SpecialType == SpecialType.System_Void
            ? "void"
            : methodSymbol.ReturnType.ToDisplayString(CustomFormats.FullTypeNameWithGlobalFormat);
        ReturnRefKind = methodSymbol.ReturnsByRef
            ? RefKind.Ref : methodSymbol.ReturnsByRefReadonly
                ? RefKind.RefReadOnly : RefKind.None;

        Name = $"__ModularRpcsGeneratedDelegate_{methodSymbol.Name}_{{0}}";

        if (methodSymbol.Parameters.Length != 0)
        {
            Parameters = new EquatableList<DelegateParameter>(methodSymbol.Parameters.Length);
            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                Parameters.Add(new DelegateParameter(parameter));
            }
        }
    }
    
    private static string GetTypeName(ITypeSymbol symbol)
    {
        if (symbol.SpecialType == SpecialType.System_Void)
            return "void";

        return symbol.ToDisplayString(CustomFormats.FullTypeNameFormat)
            .Replace('.', '_')
            .Replace("*", "__MODRPCS_POINTER__")
            .Replace("[", "__MODRPCS_ARR_O__")
            .Replace("]", "__MODRPCS_ARR_C__")
            .Replace("<", "__MODRPCS_GEN_O__")
            .Replace(">", "__MODRPCS_GEN_C__")
            .Replace(",", "__MODRPCS_RANK__")
            .Replace("&", "__MODRPCS_ADDR__");
    }

    public string GetMethodByExpressionString(RpcMethodDeclaration method, string typeName, string? name)
    {
        switch (Predefined)
        {
            case PredefinedDelegateType.Action0:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Action>(@{typeName} => @{typeName}.@{method.Name})";

            case PredefinedDelegateType.Action1:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Action<{method.Parameters[0].Type.GloballyQualifiedName}>>(@{typeName} => @{typeName}.@{method.Name})";

            case PredefinedDelegateType.Action2:
            case PredefinedDelegateType.Action3:
            case PredefinedDelegateType.Action4:
            case PredefinedDelegateType.Action5:
            case PredefinedDelegateType.Action6:
            case PredefinedDelegateType.Action7:
            case PredefinedDelegateType.Action8:
            case PredefinedDelegateType.Action9:
            case PredefinedDelegateType.Action10:
            case PredefinedDelegateType.Action11:
            case PredefinedDelegateType.Action12:
            case PredefinedDelegateType.Action13:
            case PredefinedDelegateType.Action14:
            case PredefinedDelegateType.Action15:
            case PredefinedDelegateType.Action16:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Action<{string.Join(", ", method.Parameters.Select(x => x.Type.GloballyQualifiedName))}>>(@{typeName} => @{typeName}.@{method.Name})";

            case PredefinedDelegateType.Func0:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Func<{method.ReturnType.GloballyQualifiedName}>>(@{typeName} => @{typeName}.@{method.Name})";

            case PredefinedDelegateType.Func1:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Func<{method.Parameters[0].Type.GloballyQualifiedName}, {method.ReturnType.GloballyQualifiedName}>>(@{typeName} => @{typeName}.@{method.Name})";

            case PredefinedDelegateType.Func2:
            case PredefinedDelegateType.Func3:
            case PredefinedDelegateType.Func4:
            case PredefinedDelegateType.Func5:
            case PredefinedDelegateType.Func6:
            case PredefinedDelegateType.Func7:
            case PredefinedDelegateType.Func8:
            case PredefinedDelegateType.Func9:
            case PredefinedDelegateType.Func10:
            case PredefinedDelegateType.Func11:
            case PredefinedDelegateType.Func12:
            case PredefinedDelegateType.Func13:
            case PredefinedDelegateType.Func14:
            case PredefinedDelegateType.Func15:
            case PredefinedDelegateType.Func16:
                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, global::System.Func<{string.Join(", ", method.Parameters.Select(x => x.Type.GloballyQualifiedName))}, {method.ReturnType.GloballyQualifiedName}>>(@{typeName} => @{typeName}.@{method.Name})";

            default:
                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                return $"global::DanielWillett.ModularRpcs.Reflection.SourceGenerationServices.GetMethodByExpression<@{typeName}, {name}>(@{typeName} => @{typeName}.@{method.Name})";
        }
    }

    public void WriteDefinition(SourceStringBuilder bldr, Accessibility accessibility, string name)
    {
        if (Predefined != PredefinedDelegateType.None)
            throw new InvalidOperationException("Predefined.");

        bldr.Build($"{accessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.ProtectedAndFriend => "private protected",
                Accessibility.Protected => "protected",
                Accessibility.Friend => "internal",
                Accessibility.ProtectedOrFriend => "protected internal",
                _ => "public"
            }} unsafe delegate {ReturnRefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.RefReadOnly => "ref readonly ",
                _ => string.Empty
            }}{ReturnType} @{name}({(Parameters == null || Parameters.Count == 0 ? ")" : string.Empty)}");

        if (Parameters == null || Parameters.Count == 0)
            return;

        bldr.In();
        for (int i = 0; i < Parameters.Count; i++)
        {
            DelegateParameter parameter = Parameters[i];
            string def = parameter.Definition;
            if (parameter.UnscopedRef)
                def = "[global::System.Diagnostics.CodeAnalysis.UnscopedRefAttribute] " + def;
            if (i == Parameters.Count - 1)
                bldr.String(def);
            else
                bldr.Build($"{def},");
        }

        bldr.Out().String(");");
    }

    /// <inheritdoc />
    public bool Equals(DelegateType? other)
    {
        return other != null && (ReferenceEquals(this, other) || Predefined == other.Predefined && ReturnType == other.ReturnType && ReturnRefKind == other.ReturnRefKind && Equals(Parameters, other.Parameters));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DelegateType t && Equals(t);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = (int)Predefined;
            hashCode = (hashCode * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int)ReturnRefKind;
            hashCode = (hashCode * 397) ^ (Parameters != null ? Parameters.GetHashCode() : 0);
            return hashCode;
        }
    }
}

public sealed class DelegateParameter : IEquatable<DelegateParameter>
{
    public string Definition { get; }
    public bool UnscopedRef { get; }

    public DelegateParameter(IParameterSymbol parameter)
    {
        Definition = parameter.ToDisplayString(CustomFormats.MethodDeclarationFormat);
        UnscopedRef = parameter.HasAttribute("global::System.Diagnostics.CodeAnalysis.UnscopedRefAttribute");
    }

    /// <inheritdoc />
    public bool Equals(DelegateParameter? other)
    {
        return other != null && UnscopedRef == other.UnscopedRef && string.Equals(Definition, other.Definition, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DelegateParameter p && Equals(p);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked ( Definition.GetHashCode() + (UnscopedRef ? 1 : 0) );
}

public enum PredefinedDelegateType
{
    None,
    Action0,
    Action1,
    Action2,
    Action3,
    Action4,
    Action5,
    Action6,
    Action7,
    Action8,
    Action9,
    Action10,
    Action11,
    Action12,
    Action13,
    Action14,
    Action15,
    Action16,
    Func0,
    Func1,
    Func2,
    Func3,
    Func4,
    Func5,
    Func6,
    Func7,
    Func8,
    Func9,
    Func10,
    Func11,
    Func12,
    Func13,
    Func14,
    Func15,
    Func16,
}