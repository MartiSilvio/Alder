using System.Linq.Expressions;

namespace Alder.Compiled.DynamicLinq;

public interface IDynamicQueryPlanFactory
{
    DynamicQueryPlan ParsePredicate(
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    DynamicQueryPlan ParseSelector(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    DynamicQueryPlan Parse(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null);

    DynamicQueryPlan Parse(
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<string>? parameterNames,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null);

    DynamicQueryPlan Parse(
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null);
}

public sealed class AlderDynamicQueryPlanFactory(AlderEngine engine) : IDynamicQueryPlanFactory
{
    public DynamicQueryPlan ParsePredicate(
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParsePredicate(itType, expression, values, itName);

    public DynamicQueryPlan ParseSelector(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParseSelector(itType, resultType, expression, values, itName);

    public DynamicQueryPlan Parse(
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null,
        string? itName = null)
        => engine.ParseLambda(itType, resultType, expression, values, itName);

    public DynamicQueryPlan Parse(
        IReadOnlyList<Type> parameterTypes,
        IReadOnlyList<string>? parameterNames,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
        => engine.ParseLambda(parameterTypes, parameterNames, resultType, expression, values);

    public DynamicQueryPlan Parse(
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values = null)
        => engine.ParseLambda(parameters, resultType, expression, values);
}
