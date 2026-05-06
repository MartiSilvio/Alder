using System.Linq.Expressions;
using Alder.Compiled.Compilation;
using Alder.Diagnostics;

namespace Alder.Compiled.DynamicLinq;

internal static class DynamicLinqFrontend
{
    internal static DynamicQueryPlan ParsePredicate(
        AlderEngine engine,
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return PrepareLambdaCore(
            engine,
            [parameter],
            typeof(bool),
            expression,
            values,
            enableImplicitReceiver: true,
            expectedKind: DynamicQueryLambdaKind.Predicate);
    }

    internal static DynamicQueryPlan ParseSelector(
        AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return PrepareLambdaCore(
            engine,
            [parameter],
            resultType,
            expression,
            values,
            enableImplicitReceiver: true,
            expectedKind: IsCollectionResult(resultType)
                ? DynamicQueryLambdaKind.CollectionSelector
                : DynamicQueryLambdaKind.Selector);
    }

    internal static DynamicQueryPlan PrepareLambda(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        var enableImplicitReceiver = parameters.Count == 1;
        return PrepareLambdaCore(
            engine,
            parameters,
            resultType,
            expression,
            values,
            enableImplicitReceiver,
            expectedKind: parameters.Count == 2
                ? DynamicQueryLambdaKind.BinarySelector
                : IsCollectionResult(resultType)
                    ? DynamicQueryLambdaKind.CollectionSelector
                    : DynamicQueryLambdaKind.Selector);
    }

    internal static DynamicQueryPlan ParseLambda(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
        => PrepareLambda(engine, parameters, resultType, expression, values);

    private static DynamicQueryPlan PrepareLambdaCore(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        bool enableImplicitReceiver,
        DynamicQueryLambdaKind expectedKind)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(expression);

        try
        {
            var prepared = QueryExpressionPreparer.PrepareDynamicQueryLambda(
                engine,
                parameters,
                expression,
                values,
                enableImplicitReceiver,
                expectedKind);
            return prepared;
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    private static ParameterExpression CreateItParameter(Type itType, string? itName)
        => Expression.Parameter(itType, string.IsNullOrWhiteSpace(itName) ? "it" : itName);

    private static bool IsCollectionResult(Type? resultType) =>
        resultType != null &&
        resultType != typeof(string) &&
        typeof(IEnumerable).IsAssignableFrom(resultType);
}
