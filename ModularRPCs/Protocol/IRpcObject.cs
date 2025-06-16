namespace DanielWillett.ModularRpcs.Protocol;
public interface IRpcObject<out T>
{
    T Identifier { get; }
}