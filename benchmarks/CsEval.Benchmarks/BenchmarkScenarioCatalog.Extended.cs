namespace CsEval.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    public static IReadOnlyList<ExtendedParityScenario> GetExtendedParityScenarios() =>
    [
        new(
            "ExtendedParity/BareMath",
            "sin(x)",
            "Math.Sin(x)"),
        new(
            "ExtendedParity/PipelineFunction",
            "x |> inc",
            "inc(x)"),
        new(
            "ExtendedParity/ChainedComparison",
            "0 < x < y",
            "0 < x && x < y"),
        new(
            "ExtendedParity/PowerOperator",
            "x ** 2",
            "Math.Pow(x, 2)"),
        new(
            "ExtendedParity/InOperator",
            "x in numbers",
            "numbers.Contains(x)"),
        new(
            "ExtendedParity/NotInAlias",
            "x not in numbers",
            "!numbers.Contains(x)"),
        new(
            "ExtendedParity/LikeOperator",
            "text like \"a%\"",
            "text.StartsWith(\"a\")"),
        new(
            "ExtendedParity/NotLikeAlias",
            "text not like \"z%\"",
            "!text.StartsWith(\"z\")"),
        new(
            "ExtendedParity/RegexMatch",
            "text =~ \"^a\"",
            "System.Text.RegularExpressions.Regex.IsMatch(text, \"^a\")"),
        new(
            "ExtendedParity/RegexNotMatch",
            "text !~ \"^z\"",
            "!System.Text.RegularExpressions.Regex.IsMatch(text, \"^z\")"),
        new(
            "ExtendedParity/Spaceship",
            "x <=> y",
            "x.CompareTo(y)"),
        new(
            "ExtendedParity/ImplicitIt",
            "numbers.Where(it > x).Count()",
            "numbers.Where(n => n > x).Count()"),
        new(
            "ExtendedParity/ComprehensionCount",
            "count([n * n for n in numbers if n % 2 == 0])",
            "numbers.Where(n => n % 2 == 0).Select(n => n * n).Count()"),
        new(
            "ExtendedParity/LetInExpression",
            "let tax = x * 0.1m in x + tax",
            "{ var tax = x * 0.1m; return x + tax; }"),
        new(
            "ExtendedParity/IfExpression",
            "if (x > y) x else y",
            "x > y ? x : y"),
        new(
            "ExtendedParity/AggregateBuiltins",
            "sum(numbers.Where(it > x))",
            "numbers.Where(n => n > x).Sum()"),
        new(
            "ExtendedParity/DateArithmeticSugar",
            "new DateTime(2026, 1, 1) + 30.days",
            "new DateTime(2026, 1, 1) + TimeSpan.FromDays(30)")
    ];
}
