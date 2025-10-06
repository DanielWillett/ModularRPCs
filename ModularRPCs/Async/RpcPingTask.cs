using System;

namespace DanielWillett.ModularRpcs.Async;

/// <summary>
/// A task specifically used for 
/// </summary>
public sealed class RpcPingTask : RpcTask<TimeSpan>
{
    /// <summary>
    /// The time at which the ping was started.
    /// </summary>
    public DateTime UtcPingTime { get; }

    /// <summary>
    /// The time at which the ping responded, or <see cref="DateTime.MaxValue"/> if it hasn't responded yet.
    /// </summary>
    public DateTime UtcRespondTime { get; private set; }

    internal RpcPingTask(DateTime utcPingTime)
    {
        UtcPingTime = utcPingTime;
        UtcRespondTime = DateTime.MaxValue;
    }

    protected internal override bool TrySetResult(object? value)
    {
        if (value is not TimeSpan ts)
            return false;

        ResultIntl = ts;
        UtcRespondTime = UtcPingTime + ts;
        return true;
    }

    internal bool ReceiveResponse()
    {
        DateTime now = DateTime.UtcNow;
        UtcRespondTime = now;
        ResultIntl = now - UtcPingTime;
        TriggerComplete(null);
        return true;
    }
}
