using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    private static AlderEngine GetGlobalEngine()
    {
        var engine = AlderEval.GetEngine();
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call AlderEval.Configure(o => o.UseCompiler()) before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ValidateEngine(AlderEngine engine)
    {
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call engine options UseCompiler() before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ResolveEngine(AlderEngine? engine) =>
        engine is null ? GetGlobalEngine() : ValidateEngine(engine);

    private static Dictionary<string, object?>? BuildPositionalVars(object?[] variables) =>
        variables.Length == 0 ? null : VariableBindingProjector.BuildPositionalVariables(variables);

    private static IReadOnlyList<KeyValuePair<string, object?>>? AsOrderedValues(Dictionary<string, object?>? vars) =>
        vars == null ? null : [.. vars];

    private static Func<T, bool> CompilePredicate<T>(AlderEngine engine, string predicate, Dictionary<string, object?>? vars)
        => ((Expression<Func<T, bool>>)engine
            .ParsePredicateExpression(typeof(T), predicate, AsOrderedValues(vars)))
            .Compile();

    private static Func<T, TResult> CompileSelector<T, TResult>(AlderEngine engine, string selector, Dictionary<string, object?>? vars)
        => ParseSelector<T, TResult>(engine, selector, vars).Compile();

    private static Expression<Func<T, bool>> ParsePredicate<T>(AlderEngine engine, string predicate, Dictionary<string, object?>? vars)
        => (Expression<Func<T, bool>>)engine
            .ParsePredicateExpression(typeof(T), predicate, AsOrderedValues(vars));

    private static Expression<Func<T, TResult>> ParseSelector<T, TResult>(AlderEngine engine, string selector, Dictionary<string, object?>? vars)
        => CoerceSingleParameterLambda<T, TResult>((LambdaExpression)engine
            .ParseSelectorExpression(typeof(T), typeof(TResult), selector, AsOrderedValues(vars)));

    private static Func<T, IEnumerable<TElement>> CompileCollectionSelector<T, TElement>(
        AlderEngine engine,
        string selector,
        Dictionary<string, object?>? vars)
        => CompileSelector<T, IEnumerable<TElement>>(engine, selector, vars);

    private static Expression<Func<T, IEnumerable<TElement>>> ParseCollectionSelector<T, TElement>(
        AlderEngine engine,
        string selector,
        Dictionary<string, object?>? vars)
        => ParseSelector<T, IEnumerable<TElement>>(engine, selector, vars);

    private static Func<TOuter, TInner, TResult> CompileBinaryLambda<TOuter, TInner, TResult>(
        AlderEngine engine,
        string expression,
        Dictionary<string, object?>? vars,
        string leftName,
        string rightName)
        => ParseBinaryLambda<TOuter, TInner, TResult>(
            engine,
            expression,
            vars,
            leftName,
            rightName)
            .Compile();

    private static Expression<Func<TOuter, TInner, TResult>> ParseBinaryLambda<TOuter, TInner, TResult>(
        AlderEngine engine,
        string expression,
        Dictionary<string, object?>? vars,
        string leftName,
        string rightName)
        => CoerceBinaryLambda<TOuter, TInner, TResult>((LambdaExpression)engine.ParseLambdaExpression(
            [typeof(TOuter), typeof(TInner)],
            [leftName, rightName],
            typeof(TResult),
            expression,
            AsOrderedValues(vars)));

    private static Expression<Func<T, TResult>> CoerceSingleParameterLambda<T, TResult>(LambdaExpression lambda)
    {
        if (lambda is Expression<Func<T, TResult>> typed)
            return typed;

        if (lambda.Parameters.Count != 1 || lambda.Parameters[0].Type != typeof(T))
            throw new InvalidCastException(
                $"Unable to coerce parsed lambda to Expression<Func<{typeof(T).Name}, {typeof(TResult).Name}>>.");

        return Expression.Lambda<Func<T, TResult>>(
            CoerceLambdaBody(lambda.Body, typeof(TResult)),
            lambda.Parameters[0]);
    }

    private static Expression<Func<TOuter, TInner, TResult>> CoerceBinaryLambda<TOuter, TInner, TResult>(LambdaExpression lambda)
    {
        if (lambda is Expression<Func<TOuter, TInner, TResult>> typed)
            return typed;

        if (lambda.Parameters.Count != 2 ||
            lambda.Parameters[0].Type != typeof(TOuter) ||
            lambda.Parameters[1].Type != typeof(TInner))
        {
            throw new InvalidCastException(
                $"Unable to coerce parsed lambda to Expression<Func<{typeof(TOuter).Name}, {typeof(TInner).Name}, {typeof(TResult).Name}>>.");
        }

        return Expression.Lambda<Func<TOuter, TInner, TResult>>(
            CoerceLambdaBody(lambda.Body, typeof(TResult)),
            lambda.Parameters[0],
            lambda.Parameters[1]);
    }

    private static Expression CoerceLambdaBody(Expression body, Type resultType)
    {
        if (body.Type == resultType)
            return body;

        if (!body.Type.IsValueType && resultType.IsAssignableFrom(body.Type))
            return body;

        return Expression.Convert(body, resultType);
    }

    private static Func<T, bool> CompilePredicate<T>(AlderEngine? engine, string predicate, object?[] variables) =>
        CompilePredicate<T>(ResolveEngine(engine), predicate, BuildPositionalVars(variables));

    private static Expression<Func<T, bool>> ParsePredicate<T>(AlderEngine? engine, string predicate, object?[] variables) =>
        ParsePredicate<T>(ResolveEngine(engine), predicate, BuildPositionalVars(variables));

    private static Func<T, TResult> CompileSelector<T, TResult>(AlderEngine? engine, string selector, object?[] variables) =>
        CompileSelector<T, TResult>(ResolveEngine(engine), selector, BuildPositionalVars(variables));

    private static Expression<Func<T, TResult>> ParseSelector<T, TResult>(AlderEngine? engine, string selector, object?[] variables) =>
        ParseSelector<T, TResult>(ResolveEngine(engine), selector, BuildPositionalVars(variables));

    private static Func<T, IEnumerable<TElement>> CompileCollectionSelector<T, TElement>(AlderEngine? engine, string selector, object?[] variables) =>
        CompileCollectionSelector<T, TElement>(ResolveEngine(engine), selector, BuildPositionalVars(variables));

    private static Expression<Func<T, IEnumerable<TElement>>> ParseCollectionSelector<T, TElement>(AlderEngine? engine, string selector, object?[] variables) =>
        ParseCollectionSelector<T, TElement>(ResolveEngine(engine), selector, BuildPositionalVars(variables));

    private static Func<TOuter, TInner, TResult> CompileBinaryLambda<TOuter, TInner, TResult>(
        AlderEngine? engine,
        string expression,
        object?[] variables,
        string leftName,
        string rightName)
        => CompileBinaryLambda<TOuter, TInner, TResult>(ResolveEngine(engine), expression, BuildPositionalVars(variables), leftName, rightName);

    private static Expression<Func<TOuter, TInner, TResult>> ParseBinaryLambda<TOuter, TInner, TResult>(
        AlderEngine? engine,
        string expression,
        object?[] variables,
        string leftName,
        string rightName)
        => ParseBinaryLambda<TOuter, TInner, TResult>(ResolveEngine(engine), expression, BuildPositionalVars(variables), leftName, rightName);

    private static Func<T, bool> CompilePredicate<T>(Expression<Func<T, bool>> predicateExpr)
    {
        ArgumentNullException.ThrowIfNull(predicateExpr);
        return predicateExpr.Compile();
    }

    private static Func<T, TResult> CompileSelector<T, TResult>(Expression<Func<T, TResult>> selectorExpr)
    {
        ArgumentNullException.ThrowIfNull(selectorExpr);
        return selectorExpr.Compile();
    }
}
