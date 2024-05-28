using System;
using System.Globalization;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Various switches for compatability with different runtimes, primarily Mono (including Unity).
/// </summary>
public static class Compatibility
{
    private static bool _hasTriedMemCpyCompat;
    private static bool _memCpyCompat;
    internal static bool CanMonoVersionUseMemoryCopyOnOverlappedBuffers(Version monoVersion)
    {
        return monoVersion >= new Version(6, 12);
    }
    internal static bool CanUnityVersionUseMemoryCopyOnOverlappedBuffers(string unityVersion)
    {
        // version >= 2021.2.0f1.
        unityVersion = unityVersion.Trim();
        int ind = unityVersion.IndexOf('.');
        int majorVersion;
        if (ind == -1)
        {
            if (!int.TryParse(unityVersion, NumberStyles.Number, CultureInfo.InvariantCulture, out majorVersion))
            {
                return false;
            }

            return majorVersion > 2021;
        }

        if (!int.TryParse(unityVersion.Substring(0, ind), NumberStyles.Number, CultureInfo.InvariantCulture, out majorVersion))
        {
            return false;
        }
        if (majorVersion > 2021)
            return true;
        if (majorVersion < 2021 || ind + 1 >= unityVersion.Length)
            return false;

        int ind2 = unityVersion.IndexOf('.', ind + 1);
        if (!int.TryParse(unityVersion.Substring(ind + 1, ind2 - ind - 1), NumberStyles.Number, CultureInfo.InvariantCulture, out int minorVersion))
        {
            return false;
        }

        return minorVersion > 1;
    }

    /// <summary>
    /// The current runtime is unable to use <see cref="T:System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute"/> to access private members.
    /// </summary>
    /// <remarks>All Mono implementations except the dotnet fork, which isn't easily distinguishable from the official Mono runtime.</remarks>
    public static bool IncompatibleWithIgnoresAccessChecksToAttribute => MonoImpl.MonoVersion != null;

    /// <summary>
    /// The current runtime is unable to use <see cref="Buffer.MemoryCopy"/> on overlapping buffers.
    /// </summary>
    /// <remarks>Less than Mono 6.12.0 or less than Unity 2021.2.0f1.</remarks>
    public static bool IncompatibleWithBufferMemoryCopyOverlap
    {
        get
        {
            if (_hasTriedMemCpyCompat)
                return !_memCpyCompat;

            string? unityVersion = MonoImpl.UnityVersion;
            if (unityVersion != null)
            {
                _memCpyCompat = CanUnityVersionUseMemoryCopyOnOverlappedBuffers(unityVersion);
                Interlocked.MemoryBarrier();
                _hasTriedMemCpyCompat = true;
                return !_memCpyCompat;
            }

            Version? monoVersion = MonoImpl.MonoVersion;
            if (monoVersion != null)
            {
                _memCpyCompat = CanMonoVersionUseMemoryCopyOnOverlappedBuffers(monoVersion);
                Interlocked.MemoryBarrier();
                _hasTriedMemCpyCompat = true;
                return !_memCpyCompat;
            }

            _memCpyCompat = true;
            Interlocked.MemoryBarrier();
            _hasTriedMemCpyCompat = true;
            return !_memCpyCompat;
        }
    }
}
