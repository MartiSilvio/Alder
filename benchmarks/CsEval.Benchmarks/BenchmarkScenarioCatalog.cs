namespace CsEval.Benchmarks;

public sealed record ComparableScenario(
    string Name,
    string CsEvalExpression,
    string RoslynExpression,
    string NCalcExpression,
    string DynamicExpressoExpression,
    string FleeExpression,
    Func<BenchmarkGlobalData, object?> NativeEvaluator)
{
    public override string ToString() => Name;
}

public sealed record AdvancedScenario(
    string Name,
    string CsEvalExpression,
    string RoslynExpression,
    string DynamicExpressoExpression,
    string FleeExpression)
{
    public override string ToString() => Name;
}

public sealed record ExtendedParityScenario(
    string Name,
    string ExtendedExpression,
    string StandardExpression)
{
    public override string ToString() => Name;
}

public sealed record CompilationScenario(
    string Name,
    string CsEvalExpression,
    string RoslynExpression,
    string NCalcExpression,
    string DynamicExpressoExpression,
    string FleeExpression)
{
    public override string ToString() => Name;
}

public static class BenchmarkScenarioCatalog
{
    private const string FleeSmallBranchingCsEvalExpression =
        "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)";
    private const string FleeSmallBranchingRoslynExpression =
        "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - X) * (14.0 / 3.0) + Y) : 0.0) : ((14.0 / 3.0) + Y)";
    private const string FleeSmallBranchingNCalcExpression =
        "if((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14), if(2.1 == 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))";
    private const string FleeSmallBranchingFleeExpression =
        "if((23 > 15 and 3 * 7 = 21) or (25 / 5 > 10 and 6 + 8 = 14), if(2.1 = 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))";
    private const string FleeSmallArithmeticCSharpExpression =
        "((4 * 3.4 * 18) - x) * (14.0 / 3.0) + y";
    private const string FleeSmallArithmeticRoslynExpression =
        "((4 * 3.4 * 18) - X) * (14.0 / 3.0) + Y";
    private const string NCalcSimpleEvaluationExpression =
        "(3.14 == 3.14) || (text == \"Chers\")";
    private const string NCalcSimpleEvaluationRoslynExpression =
        "(3.14 == 3.14) || (Text == \"Chers\")";

    public static IReadOnlyList<ComparableScenario> GetComparableExecutionScenarios() =>
    [
        new(
            "Arithmetic/Precedence",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            "1 + 2 * 3 - 4 / 2",
            _ => 5),
        new(
            "Arithmetic/WithVariables",
            "(x + y) * z - x / 2",
            "(X + Y) * Z - X / 2",
            "(x + y) * z - x / 2",
            "(x + y) * z - x / 2",
            "(x + y) * z - x / 2",
            g => (g.X + g.Y) * g.Z - g.X / 2),
        new(
            "Boolean/Composite",
            "x > y && y < z || x == 10",
            "X > Y && Y < Z || X == 10",
            "x > y && y < z || x == 10",
            "x > y && y < z || x == 10",
            "(x > y and y < z) or x = 10",
            g => g.X > g.Y && g.Y < g.Z || g.X == 10),
        new(
            "Conditional/Ternary",
            "x > y ? x : y",
            "X > Y ? X : Y",
            "if(x > y, x, y)",
            "x > y ? x : y",
            "if(x > y, x, y)",
            g => g.X > g.Y ? g.X : g.Y),
        new(
            "Functions/MathMix",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Math.Abs(X - Y) + Math.Max(Y, Z)",
            "Abs(x - y) + Max(y, z)",
            "Math.Abs(x - y) + Math.Max(y, z)",
            "Abs(x - y) + Max(y, z)",
            g => Math.Abs(g.X - g.Y) + Math.Max(g.Y, g.Z)),
        new(
            "Arithmetic/ModuloEquality",
            "(x % y) == 1",
            "(X % Y) == 1",
            "(x % y) == 1",
            "(x % y) == 1",
            "(x % y) = 1",
            g => (g.X % g.Y) == 1),
        new(
            "Mix/NumericAndPredicate",
            "(x * y) + (z * 2) > 20",
            "(X * Y) + (Z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            "(x * y) + (z * 2) > 20",
            g => (g.X * g.Y) + (g.Z * 2) > 20),
        new(
            "Competitor/Flee/FastVariables_Addition",
            "x + y",
            "X + Y",
            "x + y",
            "x + y",
            "x + y",
            g => g.X + g.Y),
        new(
            "Competitor/Flee/SmallBranching",
            FleeSmallBranchingCsEvalExpression,
            FleeSmallBranchingRoslynExpression,
            FleeSmallBranchingNCalcExpression,
            FleeSmallBranchingCsEvalExpression,
            FleeSmallBranchingFleeExpression,
            g => ((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14))
                ? ((2.1 == 2.1) ? ((4 * 3 - g.X) * (14.0 / 3.0) + g.Y) : 0.0)
                : ((14.0 / 3.0) + g.Y)),
        new(
            "Competitor/Flee/BigBooleanStress",
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStressForRoslyn(),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.Flee),
            g => CompetitorExpressionFactory.EvaluateBigBooleanStress(g)),
        new(
            "Competitor/NCalc/SimpleEvaluation",
            NCalcSimpleEvaluationExpression,
            NCalcSimpleEvaluationRoslynExpression,
            NCalcSimpleEvaluationExpression,
            NCalcSimpleEvaluationExpression,
            "(3.14 = 3.14) or (text = \"Chers\")",
            _ => true),
        new(
            "Competitor/NCalc/EvaluateVsLambda_Equality",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + X == 5 + Y) == (42 == Value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x = 5 + y) = (42 = value)",
            g => (1 + g.X == 5 + g.Y) == (42 == g.Value))
    ];

    public static IReadOnlyList<CompilationScenario> GetCompilationScenarios() =>
    [
        new(
            "Compilation/Flee/SmallArithmetic",
            FleeSmallArithmeticCSharpExpression,
            FleeSmallArithmeticRoslynExpression,
            FleeSmallArithmeticCSharpExpression,
            FleeSmallArithmeticCSharpExpression,
            FleeSmallArithmeticCSharpExpression),
        new(
            "Compilation/Flee/SmallBranching",
            FleeSmallBranchingCsEvalExpression,
            FleeSmallBranchingRoslynExpression,
            FleeSmallBranchingNCalcExpression,
            FleeSmallBranchingCsEvalExpression,
            FleeSmallBranchingFleeExpression),
        new(
            "Compilation/Flee/BigBooleanStress",
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStressForRoslyn(),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
            CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.Flee)),
        new(
            "Compilation/NCalc/SimpleEvaluation",
            NCalcSimpleEvaluationExpression,
            NCalcSimpleEvaluationRoslynExpression,
            NCalcSimpleEvaluationExpression,
            NCalcSimpleEvaluationExpression,
            "(3.14 = 3.14) or (text = \"Chers\")"),
        new(
            "Compilation/NCalc/EvaluateVsLambda_Equality",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + X == 5 + Y) == (42 == Value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x == 5 + y) == (42 == value)",
            "(1 + x = 5 + y) = (42 = value)")
    ];

    public static IReadOnlyList<AdvancedScenario> GetAdvancedLanguageScenarios() =>
    [
        new(
            "Advanced/NestedMath",
            "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)",
            "Math.Abs((X - Y) * (Z + 2)) + Math.Max(X, Z)",
            "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)",
            "Abs((x - y) * (z + 2)) + Max(x, z)"),
        new(
            "Advanced/NestedConditional",
            "x > y ? (y > z ? y : z) : x",
            "X > Y ? (Y > Z ? Y : Z) : X",
            "x > y ? (y > z ? y : z) : x",
            "if(x > y, if(y > z, y, z), x)"),
        new(
            "Advanced/StringPredicate",
            "text.StartsWith(\"a\") && text.Length > 3",
            "Text.StartsWith(\"a\") && Text.Length > 3",
            "text.StartsWith(\"a\") && text.Length > 3",
            "text.StartsWith(\"a\") and text.Length > 3"),
        new(
            "Advanced/CollectionProperties",
            "numbers.Count > 500 && orders.Count == 5",
            "Numbers.Count > 500 && Orders.Count == 5",
            "numbers.Count > 500 && orders.Count == 5",
            "numbers.Count > 500 and orders.Count = 5"),
        new(
            "Advanced/ObjectGraphAccess",
            "orders[0].Quantity + orders[1].Quantity + orders.Count",
            "Orders[0].Quantity + Orders[1].Quantity + Orders.Count",
            "orders[0].Quantity + orders[1].Quantity + orders.Count",
            "orders[0].Quantity + orders[1].Quantity + orders.Count")
    ];

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
