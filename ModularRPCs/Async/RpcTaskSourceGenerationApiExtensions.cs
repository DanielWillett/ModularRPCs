using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using DanielWillett.ModularRpcs.Routing;
using JetBrains.Annotations;

#pragma warning disable IDE0130
namespace DanielWillett.ModularRpcs.Async.Internal;
#pragma warning restore IDE0130

/// <summary>
/// These methods are used to skip serialization/deserialization for loopback connections in source generators.
/// </summary>
/// <remarks>Members of this class should not be used in user code.</remarks>
[UsedImplicitly, EditorBrowsable(EditorBrowsableState.Never)]
public static class RpcTaskSourceGenerationApiExtensions
{
    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RpcTask Create(bool isFireAndForget)
    {
        return new RpcTask(isFireAndForget);
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RpcTask CreateBroadcast(bool isFireAndForget)
    {
        return new RpcBroadcastTask(isFireAndForget);
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T? GetResult<T>(RpcTask<T> task)
    {
        return ref task.ResultIntl;
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref int GetCompleteCount(RpcTask task)
    {
        return ref task.CompleteCount;
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref int GetConnectionCount(RpcBroadcastTask task)
    {
        return ref task.ConnectionCountIntl;
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TriggerComplete(RpcTask task, Exception? exception)
    {
        task.TriggerComplete(exception);
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref CombinedTokenSources GetCombinedTokensToDisposeOnComplete(RpcTask task)
    {
        return ref task.CombinedTokensToDisposeOnComplete;
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetToken(RpcTask task, CancellationToken token, IRpcRouter router)
    {
        task.SetToken(token, router);
    }

    [UsedImplicitly, DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(RpcTask task, object? value)
    {
        return task.TrySetResult(value);
    }
}