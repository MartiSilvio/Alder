using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    /// <summary>
    /// Registry of binary operators. Add new operators by adding entries here.
    /// Key: TokenType, Value: (left, right) => result
    /// </summary>
    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?, object?>> BinaryOperators = new()
    {
        // Arithmetic
        { TokenType.Plus, (e, l, r) => RuntimeHelpers.Add(l, r, e._options, e._context) },
        { TokenType.Minus, (e, l, r) => RuntimeHelpers.Subtract(l, r, e._options) },
        { TokenType.Star, (e, l, r) => RuntimeHelpers.Multiply(l, r, e._options) },
        { TokenType.Slash, (e, l, r) => RuntimeHelpers.Divide(l, r, e._options) },
        { TokenType.Percent, (e, l, r) => RuntimeHelpers.Modulo(l, r, e._options) },

        // Comparison
        { TokenType.EqualEqual, (e, l, r) => RuntimeHelpers.Equals(l, r, e._options) },
        { TokenType.BangEqual, (e, l, r) => ! (bool)RuntimeHelpers.Equals(l, r, e._options) },
        { TokenType.EqualEqualEqual, (e, l, r) => RuntimeHelpers.Equals(l, r, e._options) },  // JavaScript === (same as == in C#)
        { TokenType.BangEqualEqual, (e, l, r) => ! (bool)RuntimeHelpers.Equals(l, r, e._options) },  // JavaScript !== (same as != in C#)
        { TokenType.Less, (e, l, r) => RuntimeHelpers.LessThan(l, r, e._options) },
        { TokenType.LessEqual, (e, l, r) => RuntimeHelpers.LessThanOrEqual(l, r, e._options) },
        { TokenType.Greater, (e, l, r) => RuntimeHelpers.GreaterThan(l, r, e._options) },
        { TokenType.GreaterEqual, (e, l, r) => RuntimeHelpers.GreaterThanOrEqual(l, r, e._options) },
        { TokenType.In, (e, l, r) => RuntimeHelpers.Contains(r, l, e._options) },  // Python-style: x in [1, 2, 3]

        // Bitwise
        { TokenType.Amp, (e, l, r) => RuntimeHelpers.BitwiseAnd(l, r, e._options) },
        { TokenType.Pipe, (e, l, r) => RuntimeHelpers.BitwiseOr(l, r, e._options) },
        { TokenType.Caret, (e, l, r) => RuntimeHelpers.BitwiseXor(l, r, e._options) },
        { TokenType.LessLess, (e, l, r) => RuntimeHelpers.LeftShift(l, r) },
        { TokenType.GreaterGreater, (e, l, r) => RuntimeHelpers.RightShift(l, r) },
    };

    /// <summary>
    /// Registry of unary operators. Add new operators by adding entries here.
    /// Key: TokenType, Value: (value) => result
    /// </summary>
    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, (_, v) => RuntimeHelpers.Negate(v) },
        { TokenType.Bang, (_, v) => !RuntimeHelpers.IsTruthy(v) },
        { TokenType.Tilde, (_, v) => RuntimeHelpers.BitwiseNot(v) },
    };

    /// <summary>
    /// Registry mapping compound assignment operators to their base operators.
    /// Add new compound operators by adding entries here.
    /// </summary>
    private static readonly Dictionary<TokenType, TokenType> CompoundToBaseOperator = new()
    {
        { TokenType.PlusEqual, TokenType.Plus },
        { TokenType.MinusEqual, TokenType.Minus },
        { TokenType.StarEqual, TokenType.Star },
        { TokenType.SlashEqual, TokenType.Slash },
        { TokenType.PercentEqual, TokenType.Percent },
        { TokenType.AmpEqual, TokenType.Amp },
        { TokenType.PipeEqual, TokenType.Pipe },
        { TokenType.CaretEqual, TokenType.Caret },
        { TokenType.LessLessEqual, TokenType.LessLess },
        { TokenType.GreaterGreaterEqual, TokenType.GreaterGreater },
    };

    /// <summary>
    /// Set of LINQ method names (lowercase). Add new LINQ methods by adding entries here.
    /// Includes both C# LINQ names and JavaScript aliases.
    /// </summary>
    private static readonly HashSet<string> LinqMethodNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // C# LINQ methods
        "where", "select", "selectmany", "aggregate",
        "first", "firstordefault", "last", "lastordefault",
        "single", "singleordefault", "any", "all", "count",
        "sum", "average", "min", "max", "minby", "maxby",
        "orderby", "orderbydescending", "groupby", "zip",
        "distinct", "take", "skip", "contains", "reverse",
        "except", "intersect", "union",
        "tolist", "toarray", "concat",
        // JavaScript aliases
        "map", "filter", "reduce", "find", "findindex",
        "some", "every", "includes", "foreach", "flat", "flatmap",
        "indexof", "lastindexof", "slice", "sort", "push", "pop",
        "shift", "unshift", "join", "length"
    };

    /// <summary>
    /// Method aliases mapping JS names to C# LINQ names.
    /// </summary>
    private static readonly Dictionary<string, string> MethodAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "map", "select" },
        { "filter", "where" },
        { "reduce", "aggregate" },
        { "find", "firstordefault" },
        { "some", "any" },
        { "every", "all" },
        { "includes", "contains" },
        { "flatmap", "selectmany" },
    };

    /// <summary>
    /// Resolves a method alias to its canonical name.
    /// </summary>
    internal static string ResolveMethodAlias(string methodName)
    {
        return MethodAliases.TryGetValue(methodName, out var canonical) ? canonical : methodName;
    }
}
