using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Alder.Compiled.Compilation;
using Alder.Compiled.DynamicLinq;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    private static readonly MethodInfo TypedResultConverterMethod = typeof(AlderTypedResultConverter)
        .GetMethod(nameof(AlderTypedResultConverter.Convert), BindingFlags.NonPublic | BindingFlags.Static)!
        .GetGenericMethodDefinition();

    private static Func<T, TResult> CompileMaterializingSelector<T, TResult>(
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
        => CreateMaterializingSelector<T, TResult>(engine, selector, values).Compile();

    private static Expression<Func<T, TResult>> ParseMaterializingSelector<T, TResult>(
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        return CreateMaterializingSelector<T, TResult>(engine, selector, values);
    }

    private static Expression<Func<T, TResult>> CreateMaterializingSelector<T, TResult>(
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        var prepared = PrepareSingleParameterDynamicLambda<T>(engine, selector, values, DynamicQueryLambdaKind.Selector);
        return Expression.Lambda<Func<T, TResult>>(
            CreateMaterializingSelectorBody<TResult>(prepared.ExportedLambda.Body),
            prepared.Parameters[0]);
    }

    private static Expression CreateMaterializingSelectorBody<TResult>(Expression body)
    {
        if (body.Type == typeof(TResult))
            return body;

        if (!body.Type.IsValueType && typeof(TResult).IsAssignableFrom(body.Type))
            return body;

        var boxedBody = body.Type.IsValueType
            ? Expression.Convert(body, typeof(object))
            : body;

        return Expression.Call(
            TypedResultConverterMethod.MakeGenericMethod(typeof(TResult)),
            boxedBody);
    }

    private static PreparedDynamicQueryLambda PrepareSingleParameterDynamicLambda<T>(
        AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        DynamicQueryLambdaKind kind)
        => QueryExpressionPreparer.PrepareDynamicQueryLambda(
            engine,
            [Expression.Parameter(typeof(T), "it")],
            expression,
            values,
            enableImplicitReceiver: true,
            expectedKind: kind);

    private static PreparedDynamicQueryLambda PrepareBinaryDynamicLambda<TOuter, TInner>(
        AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string leftName,
        string rightName)
        => QueryExpressionPreparer.PrepareDynamicQueryLambda(
            engine,
            [
                Expression.Parameter(typeof(TOuter), leftName),
                Expression.Parameter(typeof(TInner), rightName)
            ],
            expression,
            values,
            enableImplicitReceiver: false,
            expectedKind: DynamicQueryLambdaKind.BinarySelector);

    private static Func<T, object?> CompileBoxedSelector<T>(
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        DynamicQueryLambdaKind kind)
    {
        var prepared = PrepareSingleParameterDynamicLambda<T>(engine, selector, values, kind);
        var lambda = Expression.Lambda<Func<T, object?>>(
            CoerceLambdaBody(prepared.ExportedLambda.Body, typeof(object)),
            prepared.Parameters[0]);
        return lambda.Compile();
    }

    private static Func<T, IEnumerable> CompileUntypedCollectionSelector<T>(
        AlderEngine engine,
        string selector,
        IReadOnlyList<KeyValuePair<string, object?>>? values)
    {
        var prepared = PrepareSingleParameterDynamicLambda<T>(engine, selector, values, DynamicQueryLambdaKind.CollectionSelector);
        var lambda = Expression.Lambda<Func<T, IEnumerable>>(
            CoerceLambdaBody(prepared.ExportedLambda.Body, typeof(IEnumerable)),
            prepared.Parameters[0]);
        return lambda.Compile();
    }

    private static Func<TOuter, TInner, object?> CompileBoxedBinaryLambda<TOuter, TInner>(
        AlderEngine engine,
        string expression,
        IReadOnlyList<KeyValuePair<string, object?>>? values,
        string leftName,
        string rightName)
    {
        var prepared = PrepareBinaryDynamicLambda<TOuter, TInner>(engine, expression, values, leftName, rightName);
        var lambda = Expression.Lambda<Func<TOuter, TInner, object?>>(
            CoerceLambdaBody(prepared.ExportedLambda.Body, typeof(object)),
            prepared.Parameters[0],
            prepared.Parameters[1]);
        return lambda.Compile();
    }
}
