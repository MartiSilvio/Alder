using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.14 — Conditional logical operators (&amp;&amp;, ||).
/// Tests short-circuit evaluation, operator precedence (§12.4.2),
/// logical NOT (§12.9.4), and type errors for non-boolean operands.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalTests(CompilationMode mode)
{
    [TestCaseSource(typeof(LogicalData), nameof(LogicalData.ValueCases))]
    public async Task Eval_LogicalOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LogicalData), nameof(LogicalData.PrecedenceCases))]
    public async Task Eval_Precedence(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LogicalData), nameof(LogicalData.WithComparisonsCases))]
    public async Task Eval_WithComparisons(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(LogicalData), nameof(LogicalData.ErrorCases))]
    public async Task Eval_Logical_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }
}
