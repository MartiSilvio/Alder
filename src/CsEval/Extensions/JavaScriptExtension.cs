using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Extensions;

/// <summary>
/// JavaScript language extension for CsEval.
/// </summary>
public sealed class JavaScriptExtension : ILanguageExtension
{
    public static JavaScriptExtension Instance { get; } = new();

    public string Name => "JavaScript";

    public IReadOnlyDictionary<string, LinqDispatcher.LinqHandler> LinqHandlers { get; } =
        new Dictionary<string, LinqDispatcher.LinqHandler>(StringComparer.OrdinalIgnoreCase)
        {
            ["Map"] = LinqDispatcher.HandleSelect,
            ["Filter"] = LinqDispatcher.HandleWhere,
            ["Find"] = LinqDispatcher.HandleFirstOrDefault,
            ["Some"] = LinqDispatcher.HandleAny,
            ["Every"] = LinqDispatcher.HandleAll,
            ["Includes"] = LinqDispatcher.HandleContains,
            ["Slice"] = LinqDispatcher.HandleSkip,
            ["Flat"] = LinqDispatcher.HandleSelectMany,
            ["FlatMap"] = LinqDispatcher.HandleSelectMany,
            ["Reduce"] = HandleReduce,
        };

    public IReadOnlyDictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>> BinaryOperators { get; } =
        new Dictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>>
        {
            { TokenType.EqualEqualEqual, (l, r, opts) => Operators.Equals(l, r, opts) },
            { TokenType.BangEqualEqual, (l, r, opts) => !(bool)Operators.Equals(l, r, opts) },
        };

    private static (bool, object?) HandleReduce(IEnumerable<object?> source, Type elementType, object?[] args, CsEvalContext ctx, CsEvalOptions opts, CancellationToken ct)
    {
        if (args is [var reducer, var seed] && LinqDispatcher.IsLambda(reducer))
            return (true, source.Aggregate(seed, (acc, item) => LinqDispatcher.InvokeAnyLambdaForLinq(reducer, [acc, item], ctx, opts, ct)));

        if (args is [var reducerOnly] && LinqDispatcher.IsLambda(reducerOnly))
            return (true, source.Skip(1).Aggregate(source.FirstOrDefault(), (acc, item) => LinqDispatcher.InvokeAnyLambdaForLinq(reducerOnly, [acc, item], ctx, opts, ct)));

        return (false, null);
    }
}
