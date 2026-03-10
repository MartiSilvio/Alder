using System.Collections.Immutable;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

internal static class BoundEmitterSupport
{
    internal static LinqExpression AsObject(LinqExpression expression)
    {
        return expression.Type == typeof(object)
            ? expression
            : LinqExpression.Convert(expression, typeof(object));
    }

    internal static LinqExpression EnsureTypedExpression(LinqExpression expression, Type targetType)
    {
        return expression.Type == targetType
            ? expression
            : LinqExpression.Convert(expression, targetType);
    }

    internal static bool CanEmitDirectMethodCall(MethodInfo method, int argumentCount)
    {
        if (method.ContainsGenericParameters)
            return false;

        var parameters = MethodDispatchCache.GetParameters(method);
        if (parameters.Length != argumentCount)
            return false;

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef ||
                parameter.IsDefined(typeof(ParamArrayAttribute), false))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool TryGetIntIndexer(Type targetType, out PropertyInfo indexer)
    {
        foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!string.Equals(property.Name, "Item", StringComparison.Ordinal))
                continue;

            var parameters = property.GetIndexParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
            {
                indexer = property;
                return true;
            }
        }

        indexer = null!;
        return false;
    }

    internal static bool TryGetCountProperty(Type targetType, out PropertyInfo countProperty)
    {
        foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!string.Equals(property.Name, "Count", StringComparison.Ordinal))
                continue;
            if (property.GetIndexParameters().Length != 0 || property.PropertyType != typeof(int))
                continue;

            countProperty = property;
            return true;
        }

        countProperty = null!;
        return false;
    }

    internal static LinqExpression WrapGuardedValue(
        LinqExpression value,
        Type valueType,
        string context)
    {
        if (!TypeHelpers.RequiresReflectionLeakGuard(valueType))
            return value;

        return LinqExpression.Call(
            CompilerReflectionCache.GetGuardReflectionLeakTypedMethod(valueType),
            value,
            LinqExpression.Constant(context));
    }

    internal static string CreateMemberGuardContext(string memberName) => $"bound member access {memberName}";
    internal static string CreateMethodGuardContext(string methodName) => $"bound method call {methodName}";

    internal static string GetBoundTypeName(BoundExpr expr)
    {
        if (expr is BoundLiteralExpr { Value: null })
            return TypeNameFormatter.Null;

        if (expr is BoundLiteralExpr { Value: { } value })
            return value.GetType().Name;

        return expr.StaticType == typeof(object)
            ? "unknown"
            : expr.StaticType.Name;
    }

    internal static OutVariableBinding[] CollectOutBindings(ImmutableArray<BoundExpr> arguments)
    {
        List<OutVariableBinding>? bindings = null;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is BoundOutArgExpr { IsDiscard: false } outArg)
            {
                bindings ??= [];
                bindings.Add(new OutVariableBinding(i, outArg.VariableName, outArg.TypeName));
            }
        }

        return bindings?.ToArray() ?? [];
    }
}
