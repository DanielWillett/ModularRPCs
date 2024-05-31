using System;
using DanielWillett.ModularRpcs.Configuration;
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ModularRpcs.Serialization;

/// <summary>
/// Allows a less strict way to implement <see cref="IBinaryTypeParser"/> for multiple allowed types, ex. for generic types.
/// </summary>
/// <remarks>Individually registered <see cref="IBinaryTypeParser"/>'s always take priority over factories, and factories are prioritized in the order they were configured, first index being higher priority.</remarks>
public interface IBinaryParserFactory
{
    /// <summary>
    /// Attempt to get a parser for this type. Returning <see langword="false"/> will skip to the next factory.
    /// </summary>
    /// <param name="isInputParameter">Will this value be written or the size gotten from it instead of read, in other words will it be a parameter instead of a return value. This can be used to decide contravariance or covariance.</param>
    /// <param name="canCacheParser">If the returned <paramref name="typeParser"/> can be cached to use later for this exact type.</param>
    bool TryGetParser(Type type, SerializationConfiguration config, bool isInputParameter,
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        [MaybeNullWhen(false)]
#endif
        out IBinaryTypeParser typeParser, out bool canCacheParser);
}