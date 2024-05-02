namespace DanielWillett.ModularRpcs.Protocol;
public interface IRpcObject<T>
{
    T Identifier { get; set; }
    bool HasIdentifier { get; set; }
}