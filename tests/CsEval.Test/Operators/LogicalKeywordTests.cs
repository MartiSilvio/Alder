namespace CsEval.Test.Operators;

// Engine-only: CsEval-specific logical keywords (and, or, not) - not standard C# syntax

/// <summary>
/// Tests for 'and', 'or', 'not' keywords (CsEval extension, not standard C#).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalKeywordTests(CompilationMode mode)
{
    // 'and' keyword
    [TestCase("true and true", true, TestName = "And_BothTrue")]
    [TestCase("false and true", false, TestName = "And_LeftFalse")]
    [TestCase("true and false", false, TestName = "And_RightFalse")]
    [TestCase("(1 < 2) and (3 < 4)", true, TestName = "And_WithExpressions")]
    // 'or' keyword
    [TestCase("false or false", false, TestName = "Or_BothFalse")]
    [TestCase("true or false", true, TestName = "Or_LeftTrue")]
    [TestCase("false or true", true, TestName = "Or_RightTrue")]
    [TestCase("(1 > 2) or (3 < 4)", true, TestName = "Or_WithExpressions")]
    // 'not' keyword
    [TestCase("not true", false, TestName = "Not_True")]
    [TestCase("not false", true, TestName = "Not_False")]
    [TestCase("not (1 > 2)", true, TestName = "Not_WithExpression")]
    [TestCase("not not true", true, TestName = "Not_DoubleNegation")]
    // Combined
    [TestCase("true and not false", true, TestName = "Combined_AndNot")]
    [TestCase("false or not false", true, TestName = "Combined_OrNot")]
    [TestCase("not true or not false", true, TestName = "Combined_NotOrNot")]
    // Mixed with C# symbols
    [TestCase("true && true and true", true, TestName = "Mixed_AndWithAmpAmp")]
    [TestCase("false || true or false", true, TestName = "Mixed_OrWithPipePipe")]
    [TestCase("!false and not false", true, TestName = "Mixed_BangWithNot")]
    public void MatchesExpected(string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    // Short-circuit tests (need variables, can't use TestCase)
    [Test]
    public void And_ShortCircuits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("false and (1/x > 0)"), Is.False);
    }

    [Test]
    public void Or_ShortCircuits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("true or (1/x > 0)"), Is.True);
    }

}
