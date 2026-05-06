using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Alder.Compiled.DynamicLinq;

internal enum DynamicQueryProviderKind
{
    Enumerable,
    Queryable
}

internal enum DynamicQueryOperatorKind
{
    Select,
    SelectMany,
    SelectManyWithResultSelector,
    OrderBy,
    OrderByDescending,
    ThenBy,
    ThenByDescending,
    GroupBy,
    Join,
    GroupJoin,
    Min,
    Max,
    Sum,
    Average,
    Contains,
    ElementAt,
    ElementAtOrDefault,
    DefaultIfEmpty,
    DefaultIfEmptyWithValue,
    Append,
    Prepend
}

internal static partial class DynamicQueryMethodCache
{
    private static readonly ConcurrentDictionary<(DynamicQueryProviderKind Provider, DynamicQueryOperatorKind Operator, Type? SelectorResultType), MethodInfo> Cache = new();

    internal static MethodInfo GetMethod(
        DynamicQueryProviderKind provider,
        DynamicQueryOperatorKind op,
        Type? selectorResultType = null) =>
        Cache.GetOrAdd(
            (provider, op, selectorResultType),
            static key => ResolveMethod(key.Provider, key.Operator, key.SelectorResultType));

    private static MethodInfo ResolveMethod(
        DynamicQueryProviderKind provider,
        DynamicQueryOperatorKind op,
        Type? selectorResultType)
    {
        var declaringType = provider switch
        {
            DynamicQueryProviderKind.Enumerable => typeof(Enumerable),
            DynamicQueryProviderKind.Queryable => typeof(Queryable),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        var name = GetOperatorMethodName(op);

        foreach (var method in declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!string.Equals(method.Name, name, StringComparison.Ordinal) || !method.IsGenericMethodDefinition)
                continue;

            if (MatchesOperator(provider, op, method, selectorResultType))
                return method;
        }

        throw new InvalidOperationException($"Unable to resolve LINQ method for {provider} {op}.");
    }

    private static bool MatchesUnarySelector(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        MatchesSelector(provider, parameters[1].ParameterType, 2);

    private static bool MatchesCollectionSelector(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        MatchesCollectionSelectorParameter(provider, parameters[1].ParameterType);

    private static bool MatchesSelectManyResultSelector(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 3 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        MatchesCollectionSelectorParameter(provider, parameters[1].ParameterType) &&
        MatchesBinarySelector(provider, parameters[2].ParameterType);

    private static bool MatchesOrderedUnarySelector(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesOrderedSource(provider, parameters[0].ParameterType) &&
        MatchesSelector(provider, parameters[1].ParameterType, 2);

    private static bool MatchesJoin(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 5 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        MatchesInnerSource(provider, parameters[1].ParameterType) &&
        MatchesSelector(provider, parameters[2].ParameterType, 2) &&
        MatchesSelector(provider, parameters[3].ParameterType, 2) &&
        MatchesBinarySelector(provider, parameters[4].ParameterType);

    private static bool MatchesGroupJoin(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 5 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        MatchesInnerSource(provider, parameters[1].ParameterType) &&
        MatchesSelector(provider, parameters[2].ParameterType, 2) &&
        MatchesSelector(provider, parameters[3].ParameterType, 2) &&
        MatchesBinarySelector(provider, parameters[4].ParameterType);

    private static bool MatchesContains(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesSource(provider, parameters[0].ParameterType);

    private static bool MatchesIndexOperator(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesSource(provider, parameters[0].ParameterType) &&
        parameters[1].ParameterType == typeof(int);

    private static bool MatchesDefaultIfEmpty(DynamicQueryProviderKind provider, ParameterInfo[] parameters, bool hasValue) =>
        parameters.Length == (hasValue ? 2 : 1) &&
        MatchesSource(provider, parameters[0].ParameterType);

    private static bool MatchesAppendPrepend(DynamicQueryProviderKind provider, ParameterInfo[] parameters) =>
        parameters.Length == 2 &&
        MatchesSource(provider, parameters[0].ParameterType);

    private static bool MatchesSource(DynamicQueryProviderKind provider, Type type) =>
        provider == DynamicQueryProviderKind.Enumerable ? IsEnumerableSource(type) : IsQueryableSource(type);

    private static bool MatchesOrderedSource(DynamicQueryProviderKind provider, Type type) =>
        provider == DynamicQueryProviderKind.Enumerable
            ? type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>)
            : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>);

    private static bool MatchesInnerSource(DynamicQueryProviderKind provider, Type type) =>
        provider == DynamicQueryProviderKind.Enumerable ? IsEnumerableSource(type) : IsInnerQueryableSource(type);

    private static bool IsInnerQueryableSource(Type type) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    private static bool MatchesSelector(DynamicQueryProviderKind provider, Type type, int delegateArity) =>
        provider == DynamicQueryProviderKind.Enumerable
            ? MatchesDelegate(type, delegateArity)
            : MatchesExpression(type, delegateArity);

    private static bool MatchesCollectionSelectorParameter(DynamicQueryProviderKind provider, Type type)
    {
        if (provider == DynamicQueryProviderKind.Enumerable)
            return MatchesDelegateReturningEnumerable(type);

        return MatchesExpressionReturningEnumerable(type);
    }

    private static bool MatchesBinarySelector(DynamicQueryProviderKind provider, Type type) =>
        provider == DynamicQueryProviderKind.Enumerable
            ? MatchesDelegate(type, 3)
            : MatchesExpression(type, 3);

    private static bool MatchesNumericSelector(
        DynamicQueryProviderKind provider,
        ParameterInfo[] parameters,
        Type? selectorResultType)
    {
        if (selectorResultType is null ||
            parameters.Length != 2 ||
            !MatchesSource(provider, parameters[0].ParameterType))
        {
            return false;
        }

        return provider == DynamicQueryProviderKind.Enumerable
            ? MatchesDelegateReturningType(parameters[1].ParameterType, selectorResultType)
            : MatchesExpressionReturningType(parameters[1].ParameterType, selectorResultType);
    }

    private static bool IsEnumerableSource(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    private static bool IsQueryableSource(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>);

    private static bool MatchesDelegate(Type type, int genericArity) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == GetFuncType(genericArity);

    private static bool MatchesExpression(Type type, int funcGenericArity)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Expression<>))
            return false;

        return MatchesDelegate(type.GetGenericArguments()[0], funcGenericArity);
    }

    private static bool MatchesDelegateReturningEnumerable(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Func<,>) &&
        IsEnumerableLike(type.GetGenericArguments()[1]);

    private static bool MatchesExpressionReturningEnumerable(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Expression<>))
            return false;

        var inner = type.GetGenericArguments()[0];
        return inner.IsGenericType &&
            inner.GetGenericTypeDefinition() == typeof(Func<,>) &&
            IsEnumerableLike(inner.GetGenericArguments()[1]);
    }

    private static bool MatchesDelegateReturningType(Type type, Type expectedReturnType) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(Func<,>) &&
        type.GetGenericArguments()[1] == expectedReturnType;

    private static bool MatchesExpressionReturningType(Type type, Type expectedReturnType)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Expression<>))
            return false;

        return MatchesDelegateReturningType(type.GetGenericArguments()[0], expectedReturnType);
    }

    private static bool IsEnumerableLike(Type type) =>
        type != typeof(string) &&
        ((type.IsGenericType &&
          (type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
           || type.GetGenericTypeDefinition() == typeof(IQueryable<>)))
         || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

    private static Type GetFuncType(int genericArity) => genericArity switch
    {
        2 => typeof(Func<,>),
        3 => typeof(Func<,,>),
        _ => throw new ArgumentOutOfRangeException(nameof(genericArity))
    };
}
