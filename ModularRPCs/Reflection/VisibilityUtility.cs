using DanielWillett.ReflectionTools;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class VisibilityUtility
{
    public static bool AssemblyGivesInternalAccess(Assembly assembly)
    {
        return assembly.GetAttributesSafe<InternalsVisibleToAttribute>()
            .Any(static x => x.AssemblyName.Equals(ProxyGenerator.Instance.ProxyAssemblyName.Name));
    }
    public static bool IsMethodOverridable(MethodBase method)
        => method is { DeclaringType: not null, IsStatic: false, IsFinal: false }
           && IsMethodOverridable(method, AssemblyGivesInternalAccess(method.DeclaringType.Assembly));
    public static bool IsMethodOverridable(MethodBase method, bool assemblyGivesInternalAccess)
    {
        if (method.IsStatic || method.DeclaringType == null || method.IsFinal || method is { IsConstructor: false, IsVirtual: false, IsAbstract: false })
            return false;

        if (assemblyGivesInternalAccess)
        {
            if (method.IsPrivate)
                return false;
        }
        else
        {
            if (method is not { IsAssembly: false, IsFamilyAndAssembly: false, IsPrivate: false })
                return false;
        }

        return true;
    }
    public static bool IsTypeVisible(Type type) => IsTypeVisible(type, AssemblyGivesInternalAccess(type.Assembly));
    public static bool IsTypeVisible(Type type, bool assemblyGivesInternalAccess)
    {
        Type? nestingType = type;
        for (; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            if (nestingType.IsNested)
            {
                if (nestingType.IsNestedPrivate)
                    return false;

                if (!assemblyGivesInternalAccess)
                {
                    if (!nestingType.IsNestedPublic)
                        return false;
                }
                else if (nestingType.IsNestedFamily || nestingType.IsNestedFamANDAssem)
                    return false;
            }
            else if (!assemblyGivesInternalAccess && nestingType.IsNotPublic)
                return false;
        }

        return true;
    }
}
