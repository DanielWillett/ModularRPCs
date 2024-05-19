using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace DanielWillett.ModularRpcs.Annotations;

[MeansImplicitUse]
public abstract class RpcTargetAttribute : Attribute
{
    /// <summary>
    /// Declaring type of the target method.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Case-sensitive assembly qualified name of the declaring type of the target method.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// Case-sensitive name of the method to target.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Types of parameters to match.
    /// By default, these should only include binded parameters. See <see cref="ParametersAreBindedParametersOnly"/> for more info.
    /// </summary>
    /// <remarks>Not recommended to use this property unless it's required to differentiate target methods, as it can use a decent bit of bandwidth.</remarks>
    public Type[]? ParameterTypes { get; set; }

    /// <summary>
    /// Case-sensitive assembly qualified names of parameters to match.
    /// By default, these should only include binded parameters. See <see cref="ParametersAreBindedParametersOnly"/> for more info.
    /// </summary>
    /// <remarks>Not recommended to use this property unless it's required to differentiate target methods, as it can use a decent bit of bandwidth.</remarks>
    public string[]? ParameterTypeNames { get; set; }

    /// <summary>
    /// Do <see cref="ParameterTypes"/> or <see cref="ParameterTypeNames"/> only include binded parameters. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Binded parameters means non-injected parameters, meaning only parameters that are replicated.
    /// Other injected parameters such as <see cref="CancellationToken"/>, <see cref="IRpcRouter"/>, etc, along with any other parameters decorated with the <see cref="RpcInjectAttribute"/>, will not be checked as part of the parameter type arrays.
    /// </remarks>
    public bool ParametersAreBindedParametersOnly { get; set; } = true;

    protected RpcTargetAttribute() { }
    protected RpcTargetAttribute(string methodName)
    {
        MethodName = methodName;
    }
    protected RpcTargetAttribute(string declaringType, string methodName)
    {
        Type = Type.GetType(declaringType, throwOnError: false);
        TypeName = declaringType;
        MethodName = methodName;
    }
    protected RpcTargetAttribute(Type declaringType, string methodName)
    {
        Type = declaringType;
        TypeName = declaringType.AssemblyQualifiedName;
        MethodName = methodName;
    }

    /// <summary>
    /// Resolve a method from this target.
    /// </summary>
    public bool TryResolveMethod(
        MethodInfo decoratingMethod,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out MethodInfo method, out ResolveMethodResult result)
    {
        return TypeUtility.TryResolveMethod(decoratingMethod, MethodName, Type, TypeName, ParameterTypes, ParameterTypeNames, ParametersAreBindedParametersOnly, out method, out result);
    }
}
