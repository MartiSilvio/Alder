using System.Linq.Expressions;
using System.Reflection;
using Alder.Compiled.Compilation;

namespace Alder.Compiled.DynamicLinq;

internal static partial class DynamicQueryDispatcher
{
    private static object ApplySingleLambdaOperator(
        DynamicQueryProviderKind providerKind,
        DynamicQueryOperatorKind op,
        object source,
        AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        Type sourceType,
        DynamicQueryLambdaKind lambdaKind)
    {
        var prepared = PrepareSingleParameterLambda(engine, sourceType, expression, variables, lambdaKind);
        var methodResultType = op == DynamicQueryOperatorKind.SelectMany
            ? GetSequenceElementType(prepared.ResultType)
            : prepared.ResultType;
        var lambdaResultType = op == DynamicQueryOperatorKind.SelectMany
            ? typeof(IEnumerable<>).MakeGenericType(methodResultType)
            : methodResultType;
        var methodDefinition = DynamicQueryMethodCache.GetMethod(providerKind, op, methodResultType);
        var method = methodDefinition.MakeGenericMethod(GetGenericArguments(methodDefinition, sourceType, methodResultType));
        var lambdaArg = CreateLambdaArgument(providerKind, prepared.ExportedLambda, lambdaResultType);
        return method.Invoke(null, new object?[] { source, lambdaArg })!;
    }

    private static DynamicQueryPlan PrepareSingleParameterLambda<T>(
        AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        DynamicQueryLambdaKind kind)
        => PrepareSingleParameterLambda(engine, typeof(T), expression, variables, kind);

    private static DynamicQueryPlan PrepareSingleParameterLambda(
        AlderEngine engine,
        Type sourceType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        DynamicQueryLambdaKind kind)
        => QueryExpressionPreparer.PrepareDynamicQueryLambda(
            engine,
            [Expression.Parameter(sourceType, "it")],
            expression,
            variables,
            enableImplicitReceiver: true,
            expectedKind: kind);

    private static DynamicQueryPlan PrepareBinaryLambda(
        AlderEngine engine,
        Type leftType,
        Type rightType,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? variables)
        => QueryExpressionPreparer.PrepareDynamicQueryLambda(
            engine,
            [
                Expression.Parameter(leftType, "outer"),
                Expression.Parameter(
                    rightType,
                    rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? "group" : "inner")
            ],
            expression,
            variables,
            enableImplicitReceiver: false,
            expectedKind: DynamicQueryLambdaKind.BinarySelector);

    private static Type[] GetGenericArguments(MethodInfo methodDefinition, Type sourceType, Type resultType)
        => methodDefinition.GetGenericArguments().Length switch
        {
            1 => [sourceType],
            2 => [sourceType, resultType],
            _ => throw new InvalidOperationException($"Unsupported generic arity for {methodDefinition.Name}.")
        };

    private static object CreateLambdaArgument(
        DynamicQueryProviderKind providerKind,
        LambdaExpression lambda,
        Type resultType)
    {
        var delegateType = Expression.GetDelegateType(
            lambda.Parameters
                .Select(static p => p.Type)
                .Append(resultType)
                .ToArray());
        var typedLambda = Expression.Lambda(delegateType, lambda.Body, lambda.Parameters);
        return providerKind == DynamicQueryProviderKind.Enumerable
            ? typedLambda.Compile()
            : typedLambda;
    }

    private static Type GetSequenceElementType(Type sequenceType)
    {
        if (sequenceType == typeof(string))
            throw new InvalidOperationException("String is not a valid SelectMany collection result.");

        if (sequenceType.IsArray)
            return sequenceType.GetElementType()!;

        if (sequenceType.IsGenericType)
        {
            var genericDefinition = sequenceType.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>) || genericDefinition == typeof(IQueryable<>))
                return sequenceType.GetGenericArguments()[0];
        }

        var enumerableInterface = sequenceType
            .GetInterfaces()
            .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0]
            ?? throw new InvalidOperationException($"Unable to infer collection element type from {sequenceType}.");
    }
}
