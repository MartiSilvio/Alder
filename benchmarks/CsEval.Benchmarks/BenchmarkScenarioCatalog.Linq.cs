namespace CsEval.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    public static IReadOnlyList<LinqScenario> GetLinqScenarios() =>
    [
        new(
            "LINQ/WhereCount",
            "numbers.Where(x => x > 500).Count()",
            "Numbers.Where(x => x > 500).Count()",
            g => g.Numbers.Where(x => x > 500).Count()),
        new(
            "LINQ/SelectSum",
            "numbers.Select(x => x * 2).Sum()",
            "Numbers.Select(x => x * 2).Sum()",
            g => g.Numbers.Select(x => x * 2).Sum()),
        new(
            "LINQ/WhereSelectSum",
            "numbers.Where(x => x > 100).Select(x => x * x).Sum()",
            "Numbers.Where(x => x > 100).Select(x => x * x).Sum()",
            g => g.Numbers.Where(x => x > 100).Select(x => x * x).Sum()),
        new(
            "LINQ/AnyPredicate",
            "numbers.Any(x => x > 999)",
            "Numbers.Any(x => x > 999)",
            g => g.Numbers.Any(x => x > 999)),
        new(
            "LINQ/OrderByFirst",
            "numbers.OrderByDescending(x => x).First()",
            "Numbers.OrderByDescending(x => x).First()",
            g => g.Numbers.OrderByDescending(x => x).First())
    ];
}
