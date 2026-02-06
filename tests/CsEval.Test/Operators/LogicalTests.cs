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
    #region ECMA-334 §12.14 — Conditional Logical AND/OR and Logical NOT

    // ECMA-334 §12.14: "The && and || operators are conditional versions of the & and | operators"
    // with short-circuit evaluation semantics.
    [TestCase("true && true", true, TestName = "And_TrueTrue")]
    [TestCase("true && false", false, TestName = "And_TrueFalse")]
    [TestCase("false && true", false, TestName = "And_FalseTrue")]
    [TestCase("false && false", false, TestName = "And_FalseFalse")]
    [TestCase("true || false", true, TestName = "Or_TrueFalse")]
    [TestCase("false || true", true, TestName = "Or_FalseTrue")]
    [TestCase("false || false", false, TestName = "Or_FalseFalse")]
    [TestCase("true || true", true, TestName = "Or_TrueTrue")]
    // ECMA-334 §12.9.4: Logical negation operator
    [TestCase("!true", false, TestName = "Not_True")]
    [TestCase("!false", true, TestName = "Not_False")]
    public async Task Eval_LogicalOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.2 — Precedence and Chaining

    // ECMA-334 §12.4.2: && binds tighter than ||
    [TestCase("true || false && false", true, TestName = "Precedence_AndBeforeOr")]
    [TestCase("(true || false) && false", false, TestName = "Precedence_ParenOverride")]
    [TestCase("true && true && true", true, TestName = "And_Chained_AllTrue")]
    [TestCase("true && true && false", false, TestName = "And_Chained_LastFalse")]
    [TestCase("false || false || true", true, TestName = "Or_Chained_LastTrue")]
    [TestCase("!!true", true, TestName = "Not_DoubleNegation")]
    [TestCase("!!!false", true, TestName = "Not_TripleNegation")]
    public async Task Eval_Precedence(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.14 — Combined with Comparison Operators

    [TestCase("1 < 2 && 3 < 4", true, TestName = "And_WithComparisons")]
    [TestCase("1 > 2 || 3 < 4", true, TestName = "Or_WithComparisons")]
    [TestCase("!(1 > 2)", true, TestName = "Not_WithComparison")]
    public async Task Eval_WithComparisons(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.14 — Type Errors (Non-Boolean Operands)

    // ECMA-334 §12.14: "The operands of && and || shall be of type bool"
    [TestCase("5 && 3", TestName = "And_IntWithInt")]
    [TestCase("5 || 3", TestName = "Or_IntWithInt")]
    [TestCase("!5", TestName = "Not_Int")]
    [TestCase("!\"hello\"", TestName = "Not_String")]
    public async Task Eval_Logical_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion
}
