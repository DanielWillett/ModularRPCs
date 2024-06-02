using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IModularRpcAuthoritativeParentConnection : IModularRpcLocalConnection, IModularRpcServersideConnection
{
    Task InitializeConnectionAsync(CancellationToken token = default);
}