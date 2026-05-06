using Alder.Compiled.Compilation;

namespace Alder.Compiled.DynamicLinq;

internal static partial class DynamicQueryDispatcher
{
    private static object ApplyJoinLike(
        DynamicQueryProviderKind providerKind,
        DynamicQueryOperatorKind op,
        object outer,
        object inner,
        AlderEngine engine,
        string outerKeySelector,
        string innerKeySelector,
        string resultSelector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        Type outerType,
        Type innerType)
    {
        var outerKey = PrepareSingleParameterLambda(engine, outerType, outerKeySelector, variables, DynamicQueryLambdaKind.KeySelector);
        var innerKey = PrepareSingleParameterLambda(engine, innerType, innerKeySelector, variables, DynamicQueryLambdaKind.KeySelector);
        if (outerKey.ResultType != innerKey.ResultType)
            throw new InvalidOperationException("Join selectors inferred different key types.");

        var resultRightType = op == DynamicQueryOperatorKind.GroupJoin
            ? typeof(IEnumerable<>).MakeGenericType(innerType)
            : innerType;
        var resultPrepared = PrepareBinaryLambda(engine, outerType, resultRightType, resultSelector, variables);

        var method = DynamicQueryMethodCache.GetMethod(providerKind, op)
            .MakeGenericMethod(outerType, innerType, outerKey.ResultType, resultPrepared.ResultType);

        var outerKeyArg = CreateLambdaArgument(providerKind, outerKey.ExportedLambda, outerKey.ResultType);
        var innerKeyArg = CreateLambdaArgument(providerKind, innerKey.ExportedLambda, innerKey.ResultType);
        var resultArg = CreateLambdaArgument(providerKind, resultPrepared.ExportedLambda, resultPrepared.ResultType);

        return method.Invoke(null, new object?[] { outer, inner, outerKeyArg, innerKeyArg, resultArg })!;
    }
}
