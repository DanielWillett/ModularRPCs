namespace DanielWillett.ModularRpcs.Reflection;
internal static class Compatibility
{
    public static bool IncompatibleWithIgnoresAccessChecksToAttribute => MonoImpl.MonoVersion != null;
}
