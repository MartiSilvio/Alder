using System.Linq.Expressions;

namespace Alder.Compiled.DynamicLinq;

public static class DynamicLambdaParser
{
    public static LambdaExpression ParsePredicateExpression(
        this AlderEngine engine,
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(itType);
        return DynamicLinqFrontend.ParsePredicate(engine, itType, expression, values, itName);
    }

    public static LambdaExpression ParseSelectorExpression(
        this AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(itType);
        return DynamicLinqFrontend.ParseProjection(engine, itType, resultType, expression, values, itName);
    }

    /// <summary>
    /// Parses an expression into a <see cref="LambdaExpression"/> with a single input parameter type.
    /// Supports both lambda syntax (<c>x =&gt; ...</c>) and body-only syntax (<c>x + 1</c>).
    /// </summary>
    public static LambdaExpression ParseLambdaExpression(
        this AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
    {
        ArgumentNullException.ThrowIfNull(itType);
        var parameter = Expression.Parameter(itType, string.IsNullOrWhiteSpace(itName) ? "it" : itName);
        return DynamicLinqFrontend.ParseLambda(engine, [parameter], resultType, expression, values);
    }

    /// <summary>
    /// Parses an expression into a <see cref="LambdaExpression"/> with explicit parameter types and names.
    /// Supports both lambda syntax and body-only syntax.
    /// </summary>
    public static LambdaExpression ParseLambdaExpression(
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

        return DynamicLinqFrontend.ParseLambda(engine, parameters, resultType, expression, values);
    }

    /// <summary>
    /// Parses an expression into a <see cref="LambdaExpression"/> using explicit parameter expressions.
    /// Supports both lambda syntax and body-only syntax.
    /// </summary>
    public static LambdaExpression ParseLambdaExpression(
        this AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
    {
        return DynamicLinqFrontend.ParseLambda(engine, parameters, resultType, expression, values);
    }
}
