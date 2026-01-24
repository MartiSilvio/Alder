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
        { TokenType.Plus, (e, l, r) => e.Add(l, r) },
        { TokenType.Minus, (_, l, r) => Subtract(l, r) },
        { TokenType.Star, (_, l, r) => Multiply(l, r) },
        { TokenType.Slash, (_, l, r) => Divide(l, r) },
        { TokenType.Percent, (_, l, r) => Modulo(l, r) },

        // Comparison
        { TokenType.EqualEqual, (_, l, r) => Equals(l, r) },
        { TokenType.BangEqual, (_, l, r) => !Equals(l, r) },
        { TokenType.EqualEqualEqual, (_, l, r) => Equals(l, r) },  // JavaScript === (same as == in C#)
        { TokenType.BangEqualEqual, (_, l, r) => !Equals(l, r) },  // JavaScript !== (same as != in C#)
        { TokenType.Less, (e, l, r) => e.Compare(l, r) < 0 },
        { TokenType.LessEqual, (e, l, r) => e.Compare(l, r) <= 0 },
        { TokenType.Greater, (e, l, r) => e.Compare(l, r) > 0 },
        { TokenType.GreaterEqual, (e, l, r) => e.Compare(l, r) >= 0 },

        // Bitwise
        { TokenType.Amp, (_, l, r) => BitwiseAnd(l, r) },
        { TokenType.Pipe, (_, l, r) => BitwiseOr(l, r) },
        { TokenType.Caret, (_, l, r) => BitwiseXor(l, r) },
        { TokenType.LessLess, (_, l, r) => LeftShift(l, r) },
        { TokenType.GreaterGreater, (_, l, r) => RightShift(l, r) },
    };

    /// <summary>
    /// Registry of unary operators. Add new operators by adding entries here.
    /// Key: TokenType, Value: (value) => result
    /// </summary>
    private static readonly Dictionary<TokenType, Func<object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, Negate },
        { TokenType.Bang, v => !IsTruthy(v) },
        { TokenType.Tilde, BitwiseNot },
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
