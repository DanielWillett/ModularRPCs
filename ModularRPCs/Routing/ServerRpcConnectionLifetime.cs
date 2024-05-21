using System.Threading;
using System.Threading.Tasks;
using DanielWillett.ModularRpcs.Abstractions;

namespace DanielWillett.ModularRpcs.Routing;
public class ServerRpcConnectionLifetime : IRpcConnectionLifetime
{
    public bool IsSingleConnection => false;
    public int ForEachRemoteConnection(ForEachRemoteConnectionWhile callback, bool workOnCopy = false) => throw new System.NotImplementedException();
    public ValueTask<bool> TryAddNewConnection(IModularRpcRemoteConnection connection, CancellationToken token = default) => throw new System.NotImplementedException();

}
