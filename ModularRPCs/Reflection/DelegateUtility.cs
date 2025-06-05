using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ModularRpcs.Reflection;
internal static class DelegateUtility
{
    public static Type CreateDelegateType(string name, TypeBuilder? nestParent, string[]? generics, Type rtnType, Type[] paramTypes, string[]? argNames, ParameterAttributes[]? attr, ProxyGenerator proxyGen)
    {
        TypeBuilder typeBuilder = nestParent != null
                                  ? nestParent.DefineNestedType(name, TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.AutoClass, typeof(MulticastDelegate))
                                  : proxyGen.ModuleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass, typeof(MulticastDelegate));
        
        if (generics is { Length: > 0 })
        {
            GenericTypeParameterBuilder[] gens = typeBuilder.DefineGenericParameters(generics);
            for (int i = 0; i < paramTypes.Length; i++)
            {
                Type paramType = paramTypes[i];
                try
                {
                    _ = paramType.TypeHandle;
                    continue;
                }
                catch (NotSupportedException)
                {

                }

                string paramName;
                bool wasByRef;
                try
                {
                    wasByRef = paramType.IsByRef;
                    paramName = wasByRef ? paramType.GetElementType()!.Name : paramType.Name;
                }
                catch (NotSupportedException)
                {
                    paramName = paramType.Name;
                    wasByRef = false;
                }

                GenericTypeParameterBuilder? param = gens.FirstOrDefault(x => x.Name.Equals(paramName, StringComparison.Ordinal));
                if (param != null)
                    paramTypes[i] = wasByRef ? param.MakeByRefType() : param;
            }
        }

        typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            [ typeof(object), typeof(IntPtr) ]
        ).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        MethodBuilder invokeMethod = typeBuilder.DefineMethod(
            "Invoke",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            rtnType,
            paramTypes
        );

        invokeMethod.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

        int argInd = -1;
        for (int i = 0; i < paramTypes.Length; ++i)
        {
            ParameterAttributes pAttr = attr?[i] ?? ParameterAttributes.None;
            if (argNames != null && argNames.Length > i)
            {
                invokeMethod.DefineParameter(i + 1, pAttr, argNames[i]);
            }
            else
            {
                invokeMethod.DefineParameter(i + 1, pAttr, "arg" + (++argInd).ToString(CultureInfo.InvariantCulture));
            }
        }

#if NETSTANDARD2_0
        return typeBuilder.CreateTypeInfo()!;
#else
        return typeBuilder.CreateType()!;
#endif
    }
}
