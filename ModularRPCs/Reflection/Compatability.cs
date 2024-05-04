namespace DanielWillett.ModularRpcs.Reflection;
internal static class Compatability
{
    public static bool IncompatableWithIgnoresAccessChecksToAttribute => MonoImpl.MonoVersion != null;
}
