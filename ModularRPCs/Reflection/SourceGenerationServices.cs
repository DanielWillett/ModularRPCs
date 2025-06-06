using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace DanielWillett.ModularRpcs.Reflection;

/// <summary>
/// Utilities used by source generators.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SourceGenerationServices
{
    public delegate ref RpcCallMethodInfo GetCallInfo();
    public delegate RpcCallMethodInfo GetCallInfoByVal();

    public static string ResxRpcInjectionExceptionInstanceNull => Properties.Exceptions.RpcInjectionExceptionInstanceNull;
    public static string ResxRpcParseExceptionBufferRunOutFastRead => Properties.Exceptions.RpcParseExceptionBufferRunOutFastRead;

    public static MethodInfo GetMethodByExpression<TDelegate>(Expression<TDelegate> expression)
    {
        if (expression.Body is MethodCallExpression mtd)
        {
            return mtd.Method;
        }

        throw new MemberAccessException(expression.ToString());
    }
}