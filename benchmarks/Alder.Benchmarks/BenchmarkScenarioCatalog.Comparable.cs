namespace Alder.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    private const string FleeSmallBranchingAlderExpression =
        "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)";
    private const string FleeSmallBranchingRoslynExpression =
        "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - X) * (14.0 / 3.0) + Y) : 0.0) : ((14.0 / 3.0) + Y)";
    private const string FleeSmallBranchingNCalcExpression =
        "if((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14), if(2.1 == 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))";
    private const string FleeSmallBranchingFleeExpression =
        "if((23 > 15 and 3 * 7 = 21) or (25 / 5 > 10 and 6 + 8 = 14), if(2.1 = 2.1, ((4 * 3 - x) * (14.0 / 3.0) + y), 0.0), ((14.0 / 3.0) + y))";
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
            FleeSmallBranchingAlderExpression,
            FleeSmallBranchingRoslynExpression,
            FleeSmallBranchingNCalcExpression,
            FleeSmallBranchingAlderExpression,
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
            g => (1 + g.X == 5 + g.Y) == (42 == g.Value)),
        new(
            "String/Concatenation",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + Text",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + text",
            "\"hello\" + \" \" + text",
            g => "hello" + " " + g.Text),
        new(
            "String/CompareAndConcat",
            "text == \"alpha\" ? \"yes-\" + text : \"no\"",
            "Text == \"alpha\" ? \"yes-\" + Text : \"no\"",
            "if(text == \"alpha\", \"yes-\" + text, \"no\")",
            "text == \"alpha\" ? \"yes-\" + text : \"no\"",
            "if(text = \"alpha\", \"yes-\" + text, \"no\")",
            g => g.Text == "alpha" ? "yes-" + g.Text : "no")
    ];
}
