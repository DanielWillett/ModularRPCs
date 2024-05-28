using DanielWillett.ReflectionTools;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace DanielWillett.ModularRpcs.Reflection;

[Ignore]
internal static class MonoImpl
{
    private static bool _alreadyTriedMonoVersion;
    private static Version? _monoVersion;
    private static bool _alreadyTriedUnityVersion;
    private static string? _unityVersion;

    // parse mono version from a string looking something like this: 3.12.0 (tarball Sat Feb  7 19:13:43 UTC 2015)
    // https://stackoverflow.com/a/28924359
    internal static Version? ParseMonoVersion(string? monoVersion)
    {
        if (string.IsNullOrEmpty(monoVersion))
            return null;

        monoVersion = monoVersion!.Trim();

        int firstParenthesis = monoVersion.IndexOf('(');
        if (firstParenthesis >= 0)
        {
            while (firstParenthesis > 0 && monoVersion.Length > 2 && monoVersion[firstParenthesis - 1] == ' ')
                --firstParenthesis;
        }
        if (firstParenthesis < 0)
        {
            int endParenthesis = monoVersion.IndexOf(')');
            if (endParenthesis >= 0)
                monoVersion = monoVersion.Substring(0, endParenthesis).Trim();
        }

        string versionString = firstParenthesis < 0 ? monoVersion : monoVersion.Substring(0, firstParenthesis).Trim();

        Version.TryParse(versionString, out Version? version);
        return version;
    }

    [DllImport("__Internal", EntryPoint = "mono_get_runtime_build_info")]
    private static extern string? GetMonoVersion();
    public static string? UnityVersion
    {
        get
        {
            if (_alreadyTriedUnityVersion)
                return _unityVersion;

            Type? applicationType = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule");
            if (applicationType == null)
            {
                _unityVersion = null;
                Interlocked.MemoryBarrier();
                _alreadyTriedUnityVersion = true;
                return null;
            }

            MethodInfo? versionGetter = applicationType
                .GetProperty("unityVersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?
                .GetGetMethod(true);

            string? version = versionGetter?.Invoke(null, Array.Empty<object>()) as string;
            _unityVersion = version;
            Interlocked.MemoryBarrier();
            _alreadyTriedUnityVersion = true;
            return version;
        }
    }
    public static Version? MonoVersion
    {
        get
        {
            if (_alreadyTriedMonoVersion)
                return _monoVersion;

            try
            {
                string? v = GetMonoVersion();
                _monoVersion = ParseMonoVersion(v);
                Interlocked.MemoryBarrier();
                _alreadyTriedMonoVersion = true;
                return _monoVersion;
            }
            catch (DllNotFoundException)
            {
                _alreadyTriedMonoVersion = true;
                Interlocked.MemoryBarrier();
                _monoVersion = null;
                return null;
            }
        }
    }
}
