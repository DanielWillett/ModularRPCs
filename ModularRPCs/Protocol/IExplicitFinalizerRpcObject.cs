using JetBrains.Annotations;

namespace DanielWillett.ModularRpcs.Protocol;

/// <summary>
/// Allows source-generated types to still define a finalizer or OnDestroy method.
/// </summary>
/// <remarks>This has no effect on proxy-generated types.</remarks>
[UsedImplicitly]
public interface IExplicitFinalizerRpcObject
{
    /// <summary>
    /// Invoked by the finalizer (or OnDestroy method for Unity components).
    /// </summary>
    [UsedImplicitly]
    void OnFinalizing(ExplicitFinalizerSource source);
}

/// <summary>
/// Defines the source of a call to <see cref="IExplicitFinalizerRpcObject.OnFinalizing"/>.
/// </summary>
[UsedImplicitly]
public enum ExplicitFinalizerSource
{
    /// <summary>
    /// This finalizer invocation came from a basic C# finalizer.
    /// </summary>
    [UsedImplicitly]
    Finalizer,

    /// <summary>
    /// This finalizer invocation came the OnDestroy Unity message.
    /// </summary>
    [UsedImplicitly]
    OnDestroy
}