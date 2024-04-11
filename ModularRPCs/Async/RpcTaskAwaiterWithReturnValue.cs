namespace DanielWillett.ModularRpcs.Async;
public class RpcTaskAwaiter<T> : RpcTaskAwaiter
{
    public RpcTaskAwaiter(RpcTask<T> task, bool isCompleted) : base(task, isCompleted) { }
    public new T GetResult()
    {
        base.GetResult();
        return ((RpcTask<T>)Task).Result!;
    }
}
