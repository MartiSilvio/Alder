using System.Linq.Expressions;

namespace Alder.Compiled.DynamicLinq;

public interface IDynamicLambdaFactory
{
    LambdaExpression ParsePredicate(
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    LambdaExpression ParseSelector(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    LambdaExpression Parse(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    LambdaExpression Parse(
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<string>? parameterNames,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null);

    LambdaExpression Parse(
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null);
}

public sealed class AlderDynamicLambdaFactory(AlderEngine engine) : IDynamicLambdaFactory
{
    public LambdaExpression ParsePredicate(
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParsePredicateExpression(itType, expression, values, itName);

    public LambdaExpression ParseSelector(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParseSelectorExpression(itType, resultType, expression, values, itName);

    public LambdaExpression Parse(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParseLambdaExpression(itType, resultType, expression, values, itName);

    public LambdaExpression Parse(
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<string>? parameterNames,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
        => engine.ParseLambdaExpression(parameterTypes, parameterNames, resultType, expression, values);

    public LambdaExpression Parse(
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
        => engine.ParseLambdaExpression(parameters, resultType, expression, values);
}
