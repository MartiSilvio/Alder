using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.12 — Relational and type-testing operators.
/// Tests equality (§12.12.7), relational comparisons (§12.12.2),
/// numeric promotion in mixed-type comparisons (§12.4.7.3), and null equality (§12.12.8).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ComparisonTests(CompilationMode mode)
{
    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.EqualityCases))]
    public async Task Eval_Equality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.RelationalCases))]
    public async Task Eval_Relational(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.MixedTypeCases))]
    public async Task Eval_MixedTypeComparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.NullEqualityCases))]
    public async Task Eval_NullEquality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.CharAndBoolCases))]
    public async Task Eval_CharAndBoolComparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ComparisonData), nameof(ComparisonData.ErrorCases))]
    public async Task Eval_Comparison_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }
}
