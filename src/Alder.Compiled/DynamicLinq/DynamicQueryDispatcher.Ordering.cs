using Alder.Compiled.Compilation;

namespace Alder.Compiled.DynamicLinq;

internal static partial class DynamicQueryDispatcher
{
    private static object ApplyOrderedOperator(
        DynamicQueryProviderKind providerKind,
        object source,
        AlderEngine engine,
        string keySelector,
        IReadOnlyList<KeyValuePair<string, object?>>? variables,
        DynamicQueryOperatorKind op,
        Type sourceType)
        => ApplySingleLambdaOperator(providerKind, op, source, engine, keySelector, variables, sourceType, DynamicQueryLambdaKind.KeySelector);
}
