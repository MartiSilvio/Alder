namespace Alder.Benchmarks;

public enum CompetitorExpressionDialect
{
    CSharp,
    Flee
}

public static class CompetitorExpressionFactory
{
    public static string BuildBigBooleanStress(CompetitorExpressionDialect dialect)
    {
        return dialect switch
        {
            CompetitorExpressionDialect.CSharp => BuildBigBooleanStressCore("x", "y", "z", "value", "&&", "||", "!=", "=="),
            CompetitorExpressionDialect.Flee => BuildBigBooleanStressCore("x", "y", "z", "value", "and", "or", "<>", "="),
            _ => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, "Unsupported dialect")
        };
    }

    public static string BuildBigBooleanStressForRoslyn()
    {
        return BuildBigBooleanStressCore("X", "Y", "Z", "Value", "&&", "||", "!=", "==");
    }

    public static bool EvaluateBigBooleanStress(BenchmarkGlobalData globals)
    {
        bool disjunction = false;
        for (int i = 1; i <= 64; i++)
        {
            var multiplier = (i % 6) + 1;
            var clause = (globals.X + i) > globals.Y && (globals.Z * multiplier) != globals.Value;
            disjunction = disjunction || clause;
        }

        bool conjunction = true;
        for (int i = 1; i <= 40; i++)
        {
            var clause = (globals.X + i) > globals.Y;
            conjunction = conjunction && clause;
        }

        return disjunction && conjunction;
    }

    private static string BuildBigBooleanStressCore(
        string x,
        string y,
        string z,
        string value,
        string andToken,
        string orToken,
        string notEqualsToken,
        string equalsToken)
    {
        var disjunctionClauses = new List<string>(64);
        for (int i = 1; i <= 64; i++)
        {
            var multiplier = (i % 6) + 1;
            disjunctionClauses.Add($"(({x} + {i}) > {y} {andToken} ({z} * {multiplier}) {notEqualsToken} {value})");
        }

        var conjunctionClauses = new List<string>(40);
        for (int i = 1; i <= 40; i++)
        {
            conjunctionClauses.Add($"(({x} + {i}) > {y})");
        }

        var disjunction = string.Join($" {orToken} ", disjunctionClauses);
        var conjunction = string.Join($" {andToken} ", conjunctionClauses);

        return $"(({disjunction}) {andToken} ({conjunction})) {orToken} (({x} {equalsToken} {x}) {andToken} ({y} {equalsToken} {y}))";
    }
}
