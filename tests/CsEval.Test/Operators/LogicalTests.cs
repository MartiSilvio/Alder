namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalTests(CompilationMode mode)
{
    [TestCase("true && true", true, TestName = "And_TrueTrue")]
    [TestCase("true && false", false, TestName = "And_TrueFalse")]
    [TestCase("false && true", false, TestName = "And_FalseTrue")]
    [TestCase("false && false", false, TestName = "And_FalseFalse")]
    [TestCase("true || false", true, TestName = "Or_TrueFalse")]
    [TestCase("false || true", true, TestName = "Or_FalseTrue")]
    [TestCase("false || false", false, TestName = "Or_FalseFalse")]
    [TestCase("true || true", true, TestName = "Or_TrueTrue")]
    [TestCase("!true", false, TestName = "Not_True")]
    [TestCase("!false", true, TestName = "Not_False")]
    // ECMA-334 §12.14 - Short-circuit evaluation order
    [TestCase("true || false && false", true, TestName = "Precedence_AndBeforeOr")]
    [TestCase("(true || false) && false", false, TestName = "Precedence_ParenOverride")]
    // ECMA-334 §12.14 - Chained logical operators
    [TestCase("true && true && true", true, TestName = "And_Chained_AllTrue")]
    [TestCase("true && true && false", false, TestName = "And_Chained_LastFalse")]
    [TestCase("false || false || true", true, TestName = "Or_Chained_LastTrue")]
    // ECMA-334 §12.14 - Double negation
    [TestCase("!!true", true, TestName = "Not_DoubleNegation")]
    [TestCase("!!!false", true, TestName = "Not_TripleNegation")]
    // ECMA-334 §12.14.4 - Combined with comparison
    [TestCase("1 < 2 && 3 < 4", true, TestName = "And_WithComparisons")]
    [TestCase("1 > 2 || 3 < 4", true, TestName = "Or_WithComparisons")]
    [TestCase("!(1 > 2)", true, TestName = "Not_WithComparison")]
    public async Task Eval_Logical(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

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
}
