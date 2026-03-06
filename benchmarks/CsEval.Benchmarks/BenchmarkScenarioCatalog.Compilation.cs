namespace CsEval.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    private const string FleeSmallArithmeticCSharpExpression =
        "((4 * 3.4 * 18) - x) * (14.0 / 3.0) + y";
    private const string FleeSmallArithmeticRoslynExpression =
        "((4 * 3.4 * 18) - X) * (14.0 / 3.0) + Y";

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
}
