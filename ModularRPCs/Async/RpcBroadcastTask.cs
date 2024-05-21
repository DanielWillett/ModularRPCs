namespace DanielWillett.ModularRpcs.Async;
public class RpcBroadcastTask : RpcTask
{
    internal int ConnectionCountIntl;

    /// <summary>
    /// Number of connections this message was sent to.
    /// </summary>
    public int ConnectionCount => ConnectionCountIntl;
    internal RpcBroadcastTask(bool isFireAndForget) : base(isFireAndForget)
    {
        Awaiter = new RpcTaskAwaiter(this, isFireAndForget);
    }
}