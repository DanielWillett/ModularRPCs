using System.Text;
using System.Text.RegularExpressions;

namespace DanielWillett.ModularRpcs.SourceGeneration.Util;
internal static class NamespaceHelper
{
    private static readonly Regex NamespaceSanitizeRegex = new Regex(@"@{0,1}([^\\.]+)", RegexOptions.Compiled);
    public static string SanitizeNamespace(string ns)
    {
        if (string.IsNullOrWhiteSpace(ns))
            return ns;

        if (ns.IndexOf('@') == -1)
        {
            return "@" + ns.Replace(".", ".@");
        }

        StringBuilder sb = new StringBuilder(ns.Length + 6);
        bool isFirst = true;
        foreach (Match match in NamespaceSanitizeRegex.Matches(ns))
        {
            if (!isFirst)
                sb.Append('.');
            else
                isFirst = false;

            sb.Append('@');
            sb.Append(match.Groups[1].Value);
        }

        return sb.ToString();
    }
}
