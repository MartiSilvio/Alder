using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.13 — Logical operators (integer bitwise AND, OR, XOR, NOT),
/// §12.11 — Shift operators, §12.13.5 — Boolean logical operators (non-short-circuit).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class BitwiseTests(CompilationMode mode)
{
    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.IntegerBitwiseCases))]
    public async Task Eval_IntegerBitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.ShiftCases))]
    public async Task Eval_ShiftOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.PrecedenceCases))]
    public async Task Eval_BitwisePrecedence(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.BooleanBitwiseCases))]
    public async Task Eval_BooleanBitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.ShiftMaskingParityCases))]
    public async Task ShiftMasking(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.ShiftPrecedenceCases))]
    public async Task Precedence_ShiftAndArithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(BitwiseData), nameof(BitwiseData.ErrorCases))]
    public async Task Eval_Bitwise_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }
}
