using Alder.Compiled.Compilation;

namespace Alder.Compiled.DynamicLinq;

internal static partial class DynamicQueryDispatcher
{
    private static object ApplySelectOperator(
        DynamicQueryProviderKind providerKind,
        object source,
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        Type sourceType)
    {
        ArgumentNullException.ThrowIfNull(source);

        var prepared = PrepareSingleParameterLambda(
            engine,
            sourceType,
            selector,
            variables,
            DynamicQueryLambdaKind.Selector);

        var method = DynamicQueryMethodCache.GetMethod(
            providerKind,
            DynamicQueryOperatorKind.Select)
            .MakeGenericMethod(sourceType, prepared.ResultType);

        object lambdaArg = providerKind == DynamicQueryProviderKind.Enumerable
            ? prepared.ExportedLambda.Compile()
            : prepared.ExportedLambda;
        return method.Invoke(null, new object?[] { source, lambdaArg })!;
    }

    private static object ApplySelectManyWithResultSelector(
        DynamicQueryProviderKind providerKind,
        object source,
        AlderEngine engine,
        string collectionSelector,
        string resultSelector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        Type sourceType,
        Type collectionElementType)
    {
        var collectionPrepared = PrepareSingleParameterLambda(engine, sourceType, collectionSelector, variables, DynamicQueryLambdaKind.CollectionSelector);
        var resultPrepared = PrepareBinaryLambda(engine, sourceType, collectionElementType, resultSelector, variables);

        var method = DynamicQueryMethodCache.GetMethod(providerKind, DynamicQueryOperatorKind.SelectManyWithResultSelector)
            .MakeGenericMethod(sourceType, collectionElementType, resultPrepared.ResultType);

        var collectionArg = CreateLambdaArgument(providerKind, collectionPrepared.ExportedLambda, typeof(IEnumerable<>).MakeGenericType(collectionElementType));
        var resultArg = CreateLambdaArgument(providerKind, resultPrepared.ExportedLambda, resultPrepared.ResultType);
        return method.Invoke(null, new object?[] { source, collectionArg, resultArg })!;
    }

    private static object ApplySelectManyWithInferredCollectionElement(
        DynamicQueryProviderKind providerKind,
        object source,
        AlderEngine engine,
        string collectionSelector,
        string resultSelector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        Type sourceType)
    {
        var collectionPrepared = PrepareSingleParameterLambda(
            engine,
            sourceType,
            collectionSelector,
            variables,
            DynamicQueryLambdaKind.CollectionSelector);
        var collectionElementType = GetSequenceElementType(collectionPrepared.ResultType);
        var resultPrepared = PrepareBinaryLambda(engine, sourceType, collectionElementType, resultSelector, variables);

        var method = DynamicQueryMethodCache.GetMethod(providerKind, DynamicQueryOperatorKind.SelectManyWithResultSelector)
            .MakeGenericMethod(sourceType, collectionElementType, resultPrepared.ResultType);

        var collectionArg = CreateLambdaArgument(providerKind, collectionPrepared.ExportedLambda, typeof(IEnumerable<>).MakeGenericType(collectionElementType));
        var resultArg = CreateLambdaArgument(providerKind, resultPrepared.ExportedLambda, resultPrepared.ResultType);
        return method.Invoke(null, new object?[] { source, collectionArg, resultArg })!;
    }
}
