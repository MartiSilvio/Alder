using System.Linq.Dynamic.Core;
using Alder.Compiled;

namespace Alder.Benchmarks;

/// <summary>
/// Head-to-head scenario where Alder, Roslyn, and the external expression libraries can all participate.
/// </summary>
public sealed record CompetitorScenario(
    string Name,
    string AlderExpr,
    string RoslynExpr,
    string NCalcExpr,
    string DExpressoExpr,
    string FleeExpr,
    Func<BenchmarkData, object?> Native)
{
    public override string ToString() => Name;
}

/// <summary>
/// Scenario for Alder and Roslyn when the language surface exceeds the competitor libraries.
/// </summary>
public sealed record AlderScenario(
    string Name,
    string AlderExpr,
    string RoslynExpr,
    Func<BenchmarkData, object?> Native)
{
    public override string ToString() => Name;
}

/// <summary>
/// Paired scenario that compares Alder extended syntax with an equivalent standard expression.
/// </summary>
public sealed record ExtendedScenario(
    string Name,
    string ExtendedExpr,
    string StandardExpr)
{
    public override string ToString() => Name;
}

/// <summary>
/// Dynamic string-query scenario over in-memory collections.
/// </summary>
public sealed record DynamicLinqScenario(
    string Name,
    Func<BenchmarkData, object?> Native,
    Func<BenchmarkData, object?> Alder,
    Func<BenchmarkData, object?> DynLinqCore)
{
    public override string ToString() => Name;
}

public static class BenchmarkScenarios
{
    public static IReadOnlyList<CompetitorScenario> GetCompetitorScenarios() =>
    [
        new("Arithmetic/Constant",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            _ => 5),

        new("Arithmetic/Variables",
            "(x + y) * z - x / 2",
            "(X + Y) * Z - X / 2",
            "(x + y) * z - x / 2",
            "(x + y) * z - x / 2",
            "(x + y) * z - x / 2",
            g => (g.X + g.Y) * g.Z - g.X / 2),

        new("Arithmetic/Modulo",
            "(x % y) == 1",
            "(X % Y) == 1",
            "(x % y) == 1",
            "(x % y) == 1",
            "(x % y) = 1",
            g => (g.X % g.Y) == 1),

        new("Boolean/Composite",
            "x > y && y < z || x == 10",
            "X > Y && Y < Z || X == 10",
            "x > y && y < z || x == 10",
            "x > y && y < z || x == 10",
            "(x > y and y < z) or x = 10",
            g => g.X > g.Y && g.Y < g.Z || g.X == 10),

        new("Boolean/MixedPredicate",
            "(x * y) + (z * 2) > 20",
            "(X * Y) + (Z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            g => (g.X * g.Y) + (g.Z * 2) > 20),

        new("Conditional/Ternary",
            "x > y ? x : y",
            "X > Y ? X : Y",
            "if(x > y, x, y)",
            "x > y ? x : y",
            "if(x > y, x, y)",
            g => g.X > g.Y ? g.X : g.Y),

        new("Functions/MathMix",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Math.Abs(X - Y) + Math.Max(Y, Z)",
            "Abs(x - y) + Max(y, z)",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Abs(x - y) + Max(y, z)",
            g => Math.Abs(g.X - g.Y) + Math.Max(g.Y, g.Z)),

        new("String/Concatenation",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + Text",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + text",
            g => "hello" + " " + g.Text),

        new("String/ConditionalConcat",
            "text == \"alpha\" ? \"yes-\" + text : \"no\"",
            "Text == \"alpha\" ? \"yes-\" + Text : \"no\"",
            "if(text == \"alpha\", \"yes-\" + text, \"no\")",
            "text == \"alpha\" ? \"yes-\" + text : \"no\"",
            "if(text = \"alpha\", \"yes-\" + text, \"no\")",
            g => g.Text == "alpha" ? "yes-" + g.Text : "no"),

        new("Equality/Nested",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + X == 5 + Y) == (42 == Value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x = 5 + y) = (42 = value)",
            g => (1 + g.X == 5 + g.Y) == (42 == g.Value)),

        new("Stress/SmallBranching",
            "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)",
            "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - X) * (14.0 / 3.0) + Y) : 0.0) : ((14.0 / 3.0) + Y)",
            "if((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14), if(2.1 == 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))",
            "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)",
            "if((23 > 15 and 3 * 7 = 21) or (25 / 5 > 10 and 6 + 8 = 14), if(2.1 = 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))",
            g => ((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14))
                ? ((2.1 == 2.1) ? ((4 * 3 - g.X) * (14.0 / 3.0) + g.Y) : 0.0)
                : ((14.0 / 3.0) + g.Y)),

        new("Stress/BigBoolean",
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStressForRoslyn(),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.Flee),
            g => CompetitorExpressionFactory.EvaluateBigBooleanStress(g)),
    ];

    public static IReadOnlyList<AlderScenario> GetAdvancedScenarios() =>
    [
        new("Advanced/NestedMath",
            "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)",
            "Math.Abs((X - Y) * (Z + 2)) + Math.Max(X, Z)",
            g => Math.Abs((g.X - g.Y) * (g.Z + 2)) + Math.Max(g.X, g.Z)),

        new("Advanced/NestedConditional",
            "x > y ? (y > z ? y : z) : x",
            "X > Y ? (Y > Z ? Y : Z) : X",
            g => g.X > g.Y ? (g.Y > g.Z ? g.Y : g.Z) : g.X),

        new("Advanced/StringPredicate",
            "text.StartsWith(\"a\") && text.Length > 3",
            "Text.StartsWith(\"a\") && Text.Length > 3",
            g => g.Text.StartsWith("a") && g.Text.Length > 3),

        new("Advanced/CollectionProperties",
            "numbers.Count > 500 && orders.Count == 5",
            "Numbers.Count > 500 && Orders.Count == 5",
            g => g.Numbers.Count > 500 && g.Orders.Count == 5),

        new("Advanced/ObjectGraphAccess",
            "orders[0].Quantity + orders[1].Quantity + orders.Count",
            "Orders[0].Quantity + Orders[1].Quantity + Orders.Count",
            g => g.Orders[0].Quantity + g.Orders[1].Quantity + g.Orders.Count),

        new("Advanced/StringChain",
            "text.Trim().ToUpper().Length",
            "Text.Trim().ToUpper().Length",
            g => g.Text.Trim().ToUpper().Length),

        new("Advanced/NestedFunctionCalls",
            "Math.Max(Math.Abs(x - y), Math.Min(y, z))",
            "Math.Max(Math.Abs(X - Y), Math.Min(Y, Z))",
            g => Math.Max(Math.Abs(g.X - g.Y), Math.Min(g.Y, g.Z))),

        new("Advanced/StringInterpolation",
            "$\"{text} is {x + y}\"",
            "$\"{Text} is {X + Y}\"",
            g => $"{g.Text} is {g.X + g.Y}"),

        new("Advanced/SwitchExpression",
            """
            x switch {
                > 7 => "high",
                > 3 => "mid",
                _ => "low"
            }
            """,
            """
            X switch {
                > 7 => "high",
                > 3 => "mid",
                _ => "low"
            }
            """,
            g => g.X switch { > 7 => "high", > 3 => "mid", _ => "low" }),

        new("Advanced/NullConditional",
            "text?.Length ?? -1",
            "Text?.Length ?? -1",
            g => g.Text?.Length ?? -1),

        new("Advanced/TupleCreation",
            "(x, y, x + y)",
            "(X, Y, X + Y)",
            g => (g.X, g.Y, g.X + g.Y)),

        new("Advanced/LinqAggregate",
            "numbers.Where(n => n % 2 == 0).Select(n => n * n).Take(10).Sum()",
            "Numbers.Where(n => n % 2 == 0).Select(n => n * n).Take(10).Sum()",
            g => g.Numbers.Where(n => n % 2 == 0).Select(n => n * n).Take(10).Sum()),
    ];

    public static IReadOnlyList<AlderScenario> GetLinqScenarios() =>
    [
        new("LINQ/WhereCount",
            "numbers.Where(x => x > 500).Count()",
            "Numbers.Where(x => x > 500).Count()",
            g => g.Numbers.Where(x => x > 500).Count()),

        new("LINQ/SelectSum",
            "numbers.Select(x => x * 2).Sum()",
            "Numbers.Select(x => x * 2).Sum()",
            g => g.Numbers.Select(x => x * 2).Sum()),

        new("LINQ/WhereSelectSum",
            "numbers.Where(x => x > 100).Select(x => x * x).Sum()",
            "Numbers.Where(x => x > 100).Select(x => x * x).Sum()",
            g => g.Numbers.Where(x => x > 100).Select(x => x * x).Sum()),

        new("LINQ/AnyPredicate",
            "numbers.Any(x => x > 999)",
            "Numbers.Any(x => x > 999)",
            g => g.Numbers.Any(x => x > 999)),

        new("LINQ/OrderByFirst",
            "numbers.OrderByDescending(x => x).First()",
            "Numbers.OrderByDescending(x => x).First()",
            g => g.Numbers.OrderByDescending(x => x).First()),

        new("LINQ/ProductFilter",
            "products.Where(p => p.Price > 100m && p.IsActive).Count()",
            "Products.Where(p => p.Price > 100m && p.IsActive).Count()",
            g => g.Products.Where(p => p.Price > 100m && p.IsActive).Count()),

        new("LINQ/ProductPipeline",
            "products.Where(p => p.Category == \"Electronics\" && p.Stock > 0).Select(p => p.Price).Sum()",
            "Products.Where(p => p.Category == \"Electronics\" && p.Stock > 0).Select(p => p.Price).Sum()",
            g => g.Products.Where(p => p.Category == "Electronics" && p.Stock > 0).Select(p => p.Price).Sum()),

        new("LINQ/EmployeeAggregate",
            "employees.Where(e => e.YearsOfService > 5).Select(e => e.Salary).Average()",
            "Employees.Where(e => e.YearsOfService > 5).Select(e => e.Salary).Average()",
            g => g.Employees.Where(e => e.YearsOfService > 5).Select(e => e.Salary).Average()),
    ];

    public static IReadOnlyList<DynamicLinqScenario> GetDynamicLinqScenarios() =>
    [
        new("DynLINQ/WhereCount",
            g => g.Numbers.Where(x => x > 500).Count(),
            g => g.Numbers.WhereDynamic<int>("x => x > 500").Count(),
            g => g.Numbers.AsQueryable().Where("it > 500").Count()),

        new("DynLINQ/SelectSum",
            g => g.Numbers.Select(x => x * 2).Sum(),
            g => g.Numbers.SelectDynamic<int, int>("x => x * 2").Sum(),
            g => (int)g.Numbers.AsQueryable().Select("it * 2").Sum()),

        new("DynLINQ/WhereSelectSum",
            g => g.Numbers.Where(x => x > 100).Select(x => x * x).Sum(),
            g => g.Numbers.WhereDynamic<int>("x => x > 100").SelectDynamic<int, int>("x => x * x").Sum(),
            g => (int)g.Numbers.AsQueryable().Where("it > 100").Select("it * it").Sum()),

        new("DynLINQ/AnyPredicate",
            g => g.Numbers.Any(x => x > 999),
            g => g.Numbers.AnyDynamic<int>("x => x > 999"),
            g => g.Numbers.AsQueryable().Any("it > 999")),

        new("DynLINQ/OrderByFirst",
            g => g.Numbers.OrderByDescending(x => x).First(),
            g => g.Numbers.OrderByDescendingDynamic<int, int>("x => x").First(),
            g => g.Numbers.AsQueryable().OrderBy("it descending").First()),

        new("DynLINQ/ProductFilter",
            g => g.Products.Where(p => p.Price > 50m && p.IsActive).Count(),
            g => g.Products.WhereDynamic<Product>("p => p.Price > 50m && p.IsActive").Count(),
            g => g.Products.AsQueryable().Where("Price > 50 && IsActive").Count()),

        new("DynLINQ/OrderSort",
            g => g.Orders.OrderByDescending(o => o.UnitPrice).First(),
            g => g.Orders.OrderByDescendingDynamic<Order, decimal>("o => o.UnitPrice").First(),
            g => g.Orders.AsQueryable().OrderBy("UnitPrice descending").First()),
    ];

    public static IReadOnlyList<ExtendedScenario> GetExtendedScenarios() =>
    [
        new("Extended/BareMath", "sin(x)", "Math.Sin(x)"),
        new("Extended/Pipeline", "x |> inc", "inc(x)"),
        new("Extended/ChainedComparison", "0 < x < y", "0 < x && x < y"),
        new("Extended/PowerOperator", "x ** 2", "Math.Pow(x, 2)"),
        new("Extended/InOperator", "x in numbers", "numbers.Contains(x)"),
        new("Extended/NotIn", "x not in numbers", "!numbers.Contains(x)"),
        new("Extended/Like", "text like \"a%\"", "text.StartsWith(\"a\")"),
        new("Extended/Regex", "text =~ \"^a\"", "System.Text.RegularExpressions.Regex.IsMatch(text, \"^a\")"),
        new("Extended/Spaceship", "x <=> y", "x.CompareTo(y)"),
        new("Extended/Comprehension", "count([n * n for n in numbers if n % 2 == 0])",
            "numbers.Where(n => n % 2 == 0).Select(n => n * n).Count()"),
        new("Extended/LetIn", "let tax = x * 0.1m in x + tax",
            "{ var tax = x * 0.1m; return x + tax; }"),
        new("Extended/IfExpression", "if (x > y) x else y", "x > y ? x : y"),
        new("Extended/AggregateBuiltin", "sum(numbers.Where(n => n > x))",
            "numbers.Where(n => n > x).Sum()"),
        new("Extended/DateArithmetic", "new DateTime(2026, 1, 1) + 30.days",
            "new DateTime(2026, 1, 1) + TimeSpan.FromDays(30)"),
    ];

    public static string GenerateArithmeticExpression(int nodeCount)
    {
        var ops = new[] { "+", "-", "*" };
        var vars = new[] { "x", "y", "z" };
        var parts = new List<string> { vars[0] };

        for (int i = 1; i < nodeCount; i++)
        {
            var op = ops[i % ops.Length];
            var v = vars[i % vars.Length];
            parts.Add($" {op} {v}");
        }

        return string.Join("", parts);
    }

    public static string GenerateMathCallExpression(int callCount)
    {
        var calls = new[] {
            "Math.Abs(x - y)",
            "Math.Max(y, z)",
            "Math.Min(x, z)",
            "Math.Abs(z - x)",
            "Math.Max(x, y)"
        };

        var parts = new List<string>(callCount);
        for (int i = 0; i < callCount; i++)
            parts.Add(calls[i % calls.Length]);

        return string.Join(" + ", parts);
    }

    public static string GenerateRoslynArithmeticExpression(int nodeCount)
    {
        var ops = new[] { "+", "-", "*" };
        var vars = new[] { "X", "Y", "Z" };
        var parts = new List<string> { vars[0] };

        for (int i = 1; i < nodeCount; i++)
        {
            var op = ops[i % ops.Length];
            var v = vars[i % vars.Length];
            parts.Add($" {op} {v}");
        }

        return string.Join("", parts);
    }

    public static string GenerateRoslynMathCallExpression(int callCount)
    {
        var calls = new[] {
            "Math.Abs(X - Y)",
            "Math.Max(Y, Z)",
            "Math.Min(X, Z)",
            "Math.Abs(Z - X)",
            "Math.Max(X, Y)"
        };

        var parts = new List<string>(callCount);
        for (int i = 0; i < callCount; i++)
            parts.Add(calls[i % calls.Length]);

        return string.Join(" + ", parts);
    }

    public static int EvaluateArithmeticExpression(int nodeCount, BenchmarkData data)
    {
        var ops = new[] { '+', '-', '*' };
        var vals = new[] { data.X, data.Y, data.Z };
        var result = vals[0];

        for (int i = 1; i < nodeCount; i++)
        {
            var op = ops[i % ops.Length];
            var v = vals[i % vals.Length];
            result = op switch
            {
                '+' => result + v,
                '-' => result - v,
                '*' => result * v,
                _ => result
            };
        }

        return result;
    }

    #endregion
}
