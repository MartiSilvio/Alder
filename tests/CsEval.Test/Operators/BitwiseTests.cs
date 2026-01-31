namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class BitwiseTests(CompilationMode mode)
{
    #region Bitwise AND

    [Test]
    public async Task Eval_BitwiseAnd_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "5 & 3";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(1));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "12 & 10";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(8));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_BitwiseAnd_WithZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "255 & 0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseAnd_AllOnes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "15 & 15";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(15));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Bitwise OR

    [Test]
    public async Task Eval_BitwiseOr_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "5 | 3";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(7));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "12 | 10";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(14));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_BitwiseOr_WithZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "255 | 0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(255));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseOr_Combine()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1 | 2 | 4";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(7));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Bitwise XOR

    [Test]
    public async Task Eval_BitwiseXor_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "5 ^ 3";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(6));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "12 ^ 10";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(6));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_BitwiseXor_SameValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "42 ^ 42";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseXor_Toggle()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "15 ^ 255";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(240));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Bitwise NOT

    [Test]
    public async Task Eval_BitwiseNot_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "~0";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(-1L));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "~1";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(-2L));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_BitwiseNot_NegativeOne()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "~(-1)";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseNot_DoubleNot()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "~~42";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Left Shift

    [Test]
    public async Task Eval_LeftShift_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 << 1";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(2));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "1 << 2";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(4));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));

        const string expr3 = "1 << 3";
        var result3 = engine.Evaluate(expr3);
        Assert.That(result3, Is.EqualTo(8));
        Assert.That(result3, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr3)));
    }

    [Test]
    public async Task Eval_LeftShift_MultiplyByPowerOfTwo()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "5 << 2";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(20));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "3 << 4";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(48));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_LeftShift_Zero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "42 << 0";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(42));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "0 << 10";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(0));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    #endregion

    #region Right Shift

    [Test]
    public async Task Eval_RightShift_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "8 >> 1";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(4));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "8 >> 2";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(2));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));

        const string expr3 = "8 >> 3";
        var result3 = engine.Evaluate(expr3);
        Assert.That(result3, Is.EqualTo(1));
        Assert.That(result3, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr3)));
    }

    [Test]
    public async Task Eval_RightShift_DivideByPowerOfTwo()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "20 >> 2";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(5));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "48 >> 4";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(3));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_RightShift_Zero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "42 >> 0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Precedence

    [Test]
    public async Task Eval_Bitwise_Precedence_AndBeforeOr()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1 | 2 & 3";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(3));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Bitwise_Precedence_ShiftBeforeComparison()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1 << 2 < 5";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Bitwise_Precedence_AndBeforeXor()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "7 ^ 3 & 5";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(6));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Bitwise_Precedence_XorBeforeOr()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "1 | 2 ^ 3";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Combined with Other Operators

    [Test]
    public async Task Eval_Bitwise_WithArithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "(2 + 3) & 7";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(5));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "10 | (3 * 2)";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(14));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Bitwise_WithLogical()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "(5 & 1) == 1 && true";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Bitwise_InTernary()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true ? 5 & 3 : 0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Boolean AND (&) - Non-Short-Circuit

    [Test]
    public async Task Eval_BitwiseAnd_TrueAndTrue_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true & true";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseAnd_TrueAndFalse_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true & false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseAnd_FalseAndFalse_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "false & false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Boolean OR (|) - Non-Short-Circuit

    [Test]
    public async Task Eval_BitwiseOr_TrueOrFalse_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true | false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseOr_FalseOrTrue_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "false | true";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseOr_FalseOrFalse_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "false | false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region Boolean XOR (^)

    [Test]
    public async Task Eval_BitwiseXor_TrueXorTrue_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true ^ true";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseXor_TrueXorFalse_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "true ^ false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_BitwiseXor_FalseXorFalse_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "false ^ false";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    #endregion

    #region IL Compiled Boolean Bitwise

    [Test]
    public async Task ILCompiled_BitwiseAnd_Booleans()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string exprStr = "true & false";
        var expr = engine.Parse(exprStr);
        Assert.That(expr.TryCompile(), Is.True, "Boolean & should be IL-compilable");

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(exprStr)));
    }

    [Test]
    public async Task ILCompiled_BitwiseOr_Booleans()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string exprStr = "false | true";
        var expr = engine.Parse(exprStr);
        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(exprStr)));
    }

    [Test]
    public async Task ILCompiled_BitwiseXor_Booleans()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string exprStr = "true ^ true";
        var expr = engine.Parse(exprStr);
        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(exprStr)));
    }

    #endregion
}
