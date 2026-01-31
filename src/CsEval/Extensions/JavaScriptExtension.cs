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

    public IReadOnlyDictionary<string, Func<List<object?>, object?[], CsEvalContext, (bool, object?)>> LinqHandlers { get; } =
        new Dictionary<string, Func<List<object?>, object?[], CsEvalContext, (bool, object?)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Map"] = LinqDispatcher.HandleSelect,
            ["Filter"] = LinqDispatcher.HandleWhere,
            ["Find"] = LinqDispatcher.HandleFirstOrDefault,
            ["Some"] = LinqDispatcher.HandleAny,
            ["Every"] = LinqDispatcher.HandleAll,
            ["Includes"] = LinqDispatcher.HandleContains,
            ["Slice"] = LinqDispatcher.HandleSkip,
            ["Flat"] = LinqDispatcher.HandleSelectMany,
            ["Reduce"] = HandleReduce,
        };

    public IReadOnlyDictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>> BinaryOperators { get; } =
        new Dictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>>
        {
            { TokenType.EqualEqualEqual, (l, r, opts) => Operators.Equals(l, r, opts) },
            { TokenType.BangEqualEqual, (l, r, opts) => !(bool)Operators.Equals(l, r, opts) },
        };

    private static (bool, object?) HandleReduce(List<object?> list, object?[] args, CsEvalContext ctx)
    {
        if (args is [LambdaValue reducer, var seed])
            return (true, list.Aggregate(seed, (acc, item) => LinqDispatcher.InvokeLambdaForLinq(reducer, [acc, item], ctx)));

        if (args is [LambdaValue reducerOnly])
            return (true, list.Skip(1).Aggregate(list.FirstOrDefault(), (acc, item) => LinqDispatcher.InvokeLambdaForLinq(reducerOnly, [acc, item], ctx)));

        return (false, null);
    }
}
