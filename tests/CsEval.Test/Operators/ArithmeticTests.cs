using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 S12.9 -- Arithmetic operators, S12.4.2 -- Operator precedence.
/// Tests addition, subtraction, multiplication, division, modulo, unary operators,
/// lifted nullable arithmetic (S12.4.8), and char arithmetic (S12.4.7).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ArithmeticTests(CompilationMode mode)
{
    [TestCaseSource(typeof(ArithmeticData), nameof(ArithmeticData.ValueCases))]
    public async Task Eval_Arithmetic(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(ArithmeticData), nameof(ArithmeticData.ErrorCases))]
    public async Task Eval_Arithmetic_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }
}
