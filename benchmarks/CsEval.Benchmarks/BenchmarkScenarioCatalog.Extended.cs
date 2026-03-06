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
            "x.CompareTo(y)")
    ];
}
