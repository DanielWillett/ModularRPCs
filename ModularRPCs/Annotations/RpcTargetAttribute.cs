using DanielWillett.ModularRpcs.Reflection;
using DanielWillett.ModularRpcs.Routing;
using DanielWillett.SpeedBytes;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Annotations;

[MeansImplicitUse]
public abstract class RpcTargetAttribute : Attribute, IEquatable<RpcTargetAttribute>
{
    /// <summary>
    /// Declaring type of the target method.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Set this to <see langword="true"/> when passing in or receiving raw data, such as a
    /// <see cref="Stream"/>, <see cref="IEnumerable{T}"/> of <see cref="byte"/>,
    /// <see cref="ReadOnlySpan{T}"/> or <see cref="Span{T}"/> of <see cref="byte"/>,
    /// <see cref="byte"/> pointer and <see cref="uint"/> or <see cref="int"/> maxSize, or a <see cref="ByteReader"/> or <see cref="ByteWriter"/>.
    ///
    /// <para>
    /// Send and receive methods can also attach a <see cref="bool"/> parameter (canTakeOwnership) that defines if it's safe to use whatever input after a context switch (like awaiting in an <see langword="async"/> method).
    /// </para>
    ///
    /// <para>
    /// Parameter mapping (send):<br/>
    /// * <see cref="byte"/>[] -> input data<br/>
    /// * <see cref="IEnumerable{T}"/> of <see cref="byte"/> -> input data<br/>
    /// * <see cref="byte"/>* -> input data<br/>
    /// * <see cref="Span{T}"/> of <see cref="byte"/> -> input data<br/>
    /// * <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> -> input data<br/>
    /// * <see cref="Memory{T}"/> of <see cref="byte"/> -> input data<br/>
    /// * <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> -> input data<br/>
    /// * <see cref="Stream"/> -> input data<br/>
    /// * <see cref="ByteWriter"/> -> input data (from beginning, not supported in stream mode)<br/>
    /// * <see cref="ByteReader"/> -> input data (from current position, using number of bytes if available)<br/>
    /// * Any integer type -> Number of bytes in input data<br/>
    /// * <see cref="bool"/> -> Can take ownership (see above)<br/>
    /// Note that, except for when using streams (or <see cref="ByteReader"/> in stream mode),
    /// enough space must be left at the beginning of the buffer for the overhead. Use <see cref="ProxyGenerator.CalculateOverheadSize(Delegate, out int)"/> to calculate how many bytes to leave.
    /// The provided byte count should include the overhead.
    /// </para><br/>
    /// <para>
    /// Parameter mapping (receive):<br/>
    /// * <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/> -> recommended when using binary provider (most common). raw data from packet (data usually wont copy)<br/>
    /// * <see cref="byte"/>[] -> raw data from packet (data usually wont copy)<br/>
    /// * <see cref="List{T}"/> of <see cref="byte"/> -> raw data from packet (data may copy)<br/>
    /// * <see cref="ArrayList"/> of <see cref="byte"/> -> raw data from packet (data will copy)<br/>
    /// * <see cref="IEnumerable{T}"/>, <see cref="ICollection{T}"/>, <see cref="IList{T}"/>, <see cref="IReadOnlyCollection{T}"/>, <see cref="IReadOnlyList{T}"/> of <see cref="byte"/> -> raw data from packet (data usually wont copy)<br/>
    /// * <see cref="byte"/>* -> raw data from packet (data usually wont copy)<br/>
    /// * <see cref="ArraySegment{T}"/> of <see cref="byte"/> -> raw data from packet (data usually wont copy)<br/>
    /// * <see cref="Span{T}"/> of <see cref="byte"/> -> raw data from packet (data may copy)<br/>
    /// * <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> -> raw data from packet (data usually wont copy)<br/>
    /// * <see cref="Memory{T}"/> of <see cref="byte"/> -> raw data from packet (data may copy)<br/>
    /// * <see cref="Stream"/> -> recommended when using stream provider (less common). raw stream or <see cref="MemoryStream"/> with raw data from packet (data usually will copy)<br/>
    /// * <see cref="ByteReader"/> -> raw data from packet (data usually will copy)<br/>
    /// * Any integer type -> Number of bytes in input data<br/>
    /// * <see cref="bool"/> -> Can take ownership (see above)<br/>
    /// </para><br/>
    /// See example in XML docs.
    /// </summary>
    /// <example>
    /// <code> 
    /// [RpcSend(Raw = true)]
    /// // send maxSize bytes directly from a byte pointer. If canTakeOwnership is not included, it's assumed to be false.
    /// virtual RpcTask CallRawMethod(byte* data, int maxSize, bool canTakeOwnership) => RpcTask.NotImplemented;
    ///
    /// // receive data directly as a byte array. If canTakeOwnership is not included, it should be assumed to be true.
    /// [RpcReceive(Raw = true)]
    /// async Task RawMethod(byte[] data, bool canTakeOwnership)
    /// {
    ///     if (!canTakeOwnership)
    ///     {
    ///         // canTakeOwnership tells the method if it needs to copy the data before switching contexts (such as in an async method)
    ///         byte[] newArray = new byte[data.Length];
    ///         Buffer.BlockCopy(data, 0, newArray, 0, data.Length);
    ///         data = newArray;
    ///     }
    /// 
    ///     await Task.Delay(5000);
    ///     Accessor.Logger!.LogInfo("source", data[0]);
    /// }
    /// 
    /// // elsewhere
    /// {
    ///     int ovhSize = ProxyGenerator.Instance.CalculateOverheadSize(obj.CallRawMethod, out int idStartIndex);
    ///     int size = 32 + ovhSize;
    ///     byte* buffer = stackalloc byte[size];
    ///
    ///     // extension method for IRpcObjects.
    ///     obj.WriteIdentifier(buffer + idStartIndex);
    ///     
    ///     for (int i = 0; i &lt; 32; i++)
    ///         buffer[i + ovhSize] = (byte)i;
    ///     
    ///     await CallRawMethod(buffer, size, false);
    /// }
    /// </code>
    /// </example>
    /// <remarks>Note: parameter names do not matter, they're just matched using their types.</remarks>
    public bool Raw { get; set; }

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

    /// <inheritdoc />
    public virtual bool Equals(RpcTargetAttribute other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Type != other.Type
            || Raw != other.Raw
            || !string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
            || !string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
            || ParametersAreBindedParametersOnly != other.ParametersAreBindedParametersOnly)
        {
            return false;
        }

        if (ParameterTypes == null)
        {
            if (other.ParameterTypes != null)
                return false;
        }
        else if (other.ParameterTypes == null || other.ParameterTypes.Length != ParameterTypes.Length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < ParameterTypes.Length; ++i)
            {
                if (ParameterTypes[i] != other.ParameterTypes[i])
                    return false;
            }
        }
        if (ParameterTypeNames == null)
        {
            if (other.ParameterTypeNames != null)
                return false;
        }
        else if (other.ParameterTypeNames == null || other.ParameterTypeNames.Length != ParameterTypeNames.Length)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < ParameterTypeNames.Length; ++i)
            {
                if (!string.Equals(ParameterTypeNames[i], other.ParameterTypeNames[i], StringComparison.Ordinal))
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RpcTargetAttribute t && Equals(t);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Type, Raw, TypeName, MethodName, ParametersAreBindedParametersOnly);
}