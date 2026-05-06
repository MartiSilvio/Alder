using System.Linq.Expressions;

namespace Alder.Compiled.DynamicLinq;

public static class DynamicQueryParser
{
    public static DynamicQueryPlan ParsePredicate<T>(
        this AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParsePredicate(typeof(T), expression, values, itName);

    public static DynamicQueryPlan ParsePredicate(
        this AlderEngine engine,
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(itType);
        ArgumentNullException.ThrowIfNull(expression);
        return DynamicLinqFrontend.ParsePredicate(engine, itType, expression, values, itName);
    }

    public static DynamicQueryPlan ParseSelector<T, TResult>(
        this AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParseSelector(typeof(T), typeof(TResult), expression, values, itName);

    public static DynamicQueryPlan ParseSelector(
        this AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(itType);
        ArgumentNullException.ThrowIfNull(expression);
        return DynamicLinqFrontend.ParseSelector(engine, itType, resultType, expression, values, itName);
    }

    public static DynamicQueryPlan ParseLambda(
        this AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(itType);
        var parameter = Expression.Parameter(itType, string.IsNullOrWhiteSpace(itName) ? "it" : itName);
        return engine.ParseLambda([parameter], resultType, expression, values);
    }

    public static DynamicQueryPlan ParseLambda(
        this AlderEngine engine,
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<string>? parameterNames,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
    {
        ArgumentNullException.ThrowIfNull(parameterTypes);

        if (parameterNames != null && parameterNames.Count != parameterTypes.Count)
            throw new ArgumentException("Parameter name count must match parameter type count.", nameof(parameterNames));

        var parameters = new ParameterExpression[parameterTypes.Count];
        for (var i = 0; i < parameterTypes.Count; i++)
        {
            var name = parameterNames?[i] ?? $"arg{i}";
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Parameter names must be non-empty.", nameof(parameterNames));

            parameters[i] = Expression.Parameter(parameterTypes[i], name);
        }

        return engine.ParseLambda(parameters, resultType, expression, values);
    }

    public static DynamicQueryPlan ParseLambda(
        this AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(expression);
        return DynamicLinqFrontend.ParseLambda(engine, parameters, resultType, expression, values);
    }
}
