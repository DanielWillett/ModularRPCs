using System.Threading;
using System.Threading.Tasks;

namespace DanielWillett.ModularRpcs.Abstractions;
public interface IModularRpcAuthoritativeParentConnection : IModularRpcConnection
{
    Task InitializeConnectionAsync(IModularRpcRemoteConnection connection, CancellationToken token = default);
}