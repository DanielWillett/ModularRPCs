namespace DanielWillett.ModularRpcs.Abstractions;
public interface IRpcEndPointResolver
{
    IRpcInvocationPoint ResolveEndpoint(string domain, string callPoint);
}
