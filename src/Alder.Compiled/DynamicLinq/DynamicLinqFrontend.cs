using System.Linq.Expressions;
using Alder.Compiled.Compilation;
using Alder.Diagnostics;

namespace Alder.Compiled.DynamicLinq;

internal static class DynamicLinqFrontend
{
    internal static LambdaExpression ParsePredicate(
        AlderEngine engine,
        Type itType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return ParseLambdaCore(engine, [parameter], typeof(bool), expression, values, enableImplicitReceiver: true);
    }

    internal static LambdaExpression ParseProjection(
        AlderEngine engine,
        Type itType,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string? itName)
    {
        var parameter = CreateItParameter(itType, itName);
        return ParseLambdaCore(engine, [parameter], resultType, expression, values, enableImplicitReceiver: true);
    }

    internal static LambdaExpression ParseLambda(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        var enableImplicitReceiver = parameters.Count == 1;
        return ParseLambdaCore(engine, parameters, resultType, expression, values, enableImplicitReceiver);
    }

    private static LambdaExpression ParseLambdaCore(
        AlderEngine engine,
        IReadOnlyList<ParameterExpression> parameters,
        Type? resultType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        bool enableImplicitReceiver)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(expression);

        try
        {
            var prepared = QueryExpressionPreparer.PrepareDynamicLambda(
                engine,
                parameters,
                expression,
                values,
                enableImplicitReceiver);
            var body = new QueryTreeExporter(prepared.Parameters, prepared.CapturedVariables)
                .Export(prepared.BoundBody);
            body = CoerceResult(body, resultType);

            return Expression.Lambda(body, prepared.Parameters);
        }
        catch (InsufficientExecutionStackException ex)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded, ex);
        }
    }

    private static Expression CoerceResult(Expression body, Type? resultType)
    {
        if (resultType == null || body.Type == resultType)
            return body;

        if (!body.Type.IsValueType && resultType.IsAssignableFrom(body.Type))
            return body;

        try
        {
            return Expression.Convert(body, resultType);
        }
        catch (InvalidOperationException ex)
        {
            throw new AlderException(
                DiagnosticDescriptors.CantConvAnonMethReturnType,
                ex,
                resultType.Name);
        }
    }

    private static ParameterExpression CreateItParameter(Type itType, string? itName)
        => Expression.Parameter(itType, string.IsNullOrWhiteSpace(itName) ? "it" : itName);
}
