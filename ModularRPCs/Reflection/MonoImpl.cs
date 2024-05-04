using System;
using System.Runtime.InteropServices;
using DanielWillett.ReflectionTools;

namespace DanielWillett.ModularRpcs.Reflection;

[Ignore]
internal static class MonoImpl
{
    private static bool _alreadyTriedMonoVersion;
    private static Version? _monoVersion;

    [DllImport("__Internal", EntryPoint = "mono_get_runtime_build_info")]
    private static extern string GetMonoVersion();

    public static Version? MonoVersion
    {
        get
        {
            if (_alreadyTriedMonoVersion)
                return _monoVersion;

            try
            {
                // parse mono version from a string looking something like this: 3.12.0 (tarball Sat Feb  7 19:13:43 UTC 2015)
                // https://stackoverflow.com/a/28924359

                string v = GetMonoVersion();
                int firstParenthesis = v.IndexOf('(');
                if (firstParenthesis >= 0)
                {
                    while (firstParenthesis > 0 && v.Length > 2 && v[firstParenthesis - 1] == ' ')
                        --firstParenthesis;
                }

                string versionString = firstParenthesis < 0 ? v : v.Substring(0, firstParenthesis);
                Version.TryParse(versionString, out _monoVersion);
                _alreadyTriedMonoVersion = true;
                return _monoVersion;
            }
            catch (DllNotFoundException)
            {
                _alreadyTriedMonoVersion = true;
                _monoVersion = null;
                return null;
            }
        }
    }
}
