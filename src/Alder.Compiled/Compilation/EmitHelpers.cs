using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Compiled.Compilation;

internal static class EmitHelpers
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

    internal static LinqExpression UnboxOrCoerce(LinqExpression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;
        if (expression.Type != typeof(object))
            expression = LinqExpression.Convert(expression, typeof(object));
        return targetType.IsValueType
            ? LinqExpression.Unbox(expression, targetType)
            : LinqExpression.Convert(expression, targetType);
    }

    // Converts any expression to string using string.Concat(object), which handles null (returns "")
    // and matches C# string concatenation coercion semantics.
    internal static LinqExpression ToStringExpression(LinqExpression expression)
    {
        if (expression.Type == typeof(string))
            return expression;
        return LinqExpression.Call(BoundRuntimeMethodCache.StringConcatObjectMethod, AsObject(expression));
    }

    internal static bool CanEmitDirectMethodCall(BoundResolvedCallExpr call, int sourceArgumentCount)
    {
        var method = call.SelectedMethod;
        if (method.ContainsGenericParameters)
            return false;

        var resolved = call.Resolution;
        var parameters = MethodDispatchCache.GetParameters(method);
        var sources = resolved.ArgMap.Sources;

        if (parameters.Length != sources.Length)
            return false;

        if (resolved.Conversions.Length != sourceArgumentCount)
            return false;

        foreach (var parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef)
                return false;
        }

        for (var paramIdx = 0; paramIdx < sources.Length; paramIdx++)
        {
            var source = sources[paramIdx];
            switch (source.Kind)
            {
                case ParameterSourceKind.Argument:
                    if ((uint)source.ArgumentIndex >= (uint)sourceArgumentCount)
                        return false;
                    break;

                case ParameterSourceKind.Default:
                    if (!parameters[paramIdx].HasDefaultValue)
                        return false;
                    break;

                case ParameterSourceKind.ParamsRange:
                    if (!parameters[paramIdx].IsDefined(typeof(ParamArrayAttribute), false))
                        return false;
                    if (source.ParamsStartIndex < 0 || source.ParamsCount < 0)
                        return false;
                    if (source.ParamsStartIndex + source.ParamsCount > sourceArgumentCount)
                        return false;
                    break;

                default:
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
        return expr switch
        {
            BoundLiteralExpr { Value: null } => TypeNameFormatter.Null,
            BoundLiteralExpr { Value: { } value } => value.GetType().Name,
            _ => expr.StaticType.ClrType == typeof(object) ? "unknown" : expr.StaticType.ClrType.Name
        };
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
