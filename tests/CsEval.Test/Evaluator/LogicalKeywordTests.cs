using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalKeywordTests(CompilationMode mode) : TestBase
{
    #region 'and' keyword

    [Test]
    public void And_BothTrue_ReturnsTrue()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true and true"), Is.True);
    }

    [Test]
    public void And_LeftFalse_ReturnsFalse()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("false and true"), Is.False);
    }

    [Test]
    public void And_RightFalse_ReturnsFalse()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true and false"), Is.False);
    }

    [Test]
    public void And_WithExpressions_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("(1 < 2) and (3 < 4)"), Is.True);
    }

    [Test]
    public void And_ShortCircuits()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 0);
        // If short-circuit works, division by zero shouldn't happen
        Assert.That(engine.Evaluate("false and (1/x > 0)"), Is.False);
    }

    #endregion

    #region 'or' keyword

    [Test]
    public void Or_BothFalse_ReturnsFalse()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("false or false"), Is.False);
    }

    [Test]
    public void Or_LeftTrue_ReturnsTrue()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true or false"), Is.True);
    }

    [Test]
    public void Or_RightTrue_ReturnsTrue()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("false or true"), Is.True);
    }

    [Test]
    public void Or_WithExpressions_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("(1 > 2) or (3 < 4)"), Is.True);
    }

    [Test]
    public void Or_ShortCircuits()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 0);
        // If short-circuit works, division by zero shouldn't happen
        Assert.That(engine.Evaluate("true or (1/x > 0)"), Is.True);
    }

    #endregion

    #region 'not' keyword

    [Test]
    public void Not_True_ReturnsFalse()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("not true"), Is.False);
    }

    [Test]
    public void Not_False_ReturnsTrue()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("not false"), Is.True);
    }

    [Test]
    public void Not_WithExpression_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("not (1 > 2)"), Is.True);
    }

    [Test]
    public void Not_DoubleNegation_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("not not true"), Is.True);
    }

    #endregion

    #region Combined usage

    [Test]
    public void Combined_AndOrNot_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true and not false"), Is.True);
        Assert.That(engine.Evaluate("false or not false"), Is.True);
        Assert.That(engine.Evaluate("not true or not false"), Is.True);
    }

    [Test]
    public void Combined_MixedWithSymbols_Works()
    {
        var engine = CreateEngine(mode);
        // Can mix 'and'/'or'/'not' with &&/||/!
        Assert.That(engine.Evaluate("true && true and true"), Is.True);
        Assert.That(engine.Evaluate("false || true or false"), Is.True);
        Assert.That(engine.Evaluate("!false and not false"), Is.True);
    }

    [Test]
    public void Combined_WithInOperator_Works()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("(2 in [1, 2, 3]) and (5 in [4, 5, 6])"), Is.True);
        Assert.That(engine.Evaluate("not (5 in [1, 2, 3])"), Is.True);
    }

    #endregion
}
