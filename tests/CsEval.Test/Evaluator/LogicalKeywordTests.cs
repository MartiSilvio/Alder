using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class LogicalKeywordTests : EvaluatorTestBase
{
    #region 'and' keyword

    [Test]
    public void And_BothTrue_ReturnsTrue()
    {
        Assert.That(Eval("true and true"), Is.True);
    }

    [Test]
    public void And_LeftFalse_ReturnsFalse()
    {
        Assert.That(Eval("false and true"), Is.False);
    }

    [Test]
    public void And_RightFalse_ReturnsFalse()
    {
        Assert.That(Eval("true and false"), Is.False);
    }

    [Test]
    public void And_WithExpressions_Works()
    {
        Assert.That(Eval("(1 < 2) and (3 < 4)"), Is.True);
    }

    [Test]
    public void And_ShortCircuits()
    {
        var context = new EvalContext();
        context.Define("x", 0);
        // If short-circuit works, division by zero shouldn't happen
        Assert.That(Eval("false and (1/x > 0)", context), Is.False);
    }

    #endregion

    #region 'or' keyword

    [Test]
    public void Or_BothFalse_ReturnsFalse()
    {
        Assert.That(Eval("false or false"), Is.False);
    }

    [Test]
    public void Or_LeftTrue_ReturnsTrue()
    {
        Assert.That(Eval("true or false"), Is.True);
    }

    [Test]
    public void Or_RightTrue_ReturnsTrue()
    {
        Assert.That(Eval("false or true"), Is.True);
    }

    [Test]
    public void Or_WithExpressions_Works()
    {
        Assert.That(Eval("(1 > 2) or (3 < 4)"), Is.True);
    }

    [Test]
    public void Or_ShortCircuits()
    {
        var context = new EvalContext();
        context.Define("x", 0);
        // If short-circuit works, division by zero shouldn't happen
        Assert.That(Eval("true or (1/x > 0)", context), Is.True);
    }

    #endregion

    #region 'not' keyword

    [Test]
    public void Not_True_ReturnsFalse()
    {
        Assert.That(Eval("not true"), Is.False);
    }

    [Test]
    public void Not_False_ReturnsTrue()
    {
        Assert.That(Eval("not false"), Is.True);
    }

    [Test]
    public void Not_WithExpression_Works()
    {
        Assert.That(Eval("not (1 > 2)"), Is.True);
    }

    [Test]
    public void Not_DoubleNegation_Works()
    {
        Assert.That(Eval("not not true"), Is.True);
    }

    #endregion

    #region Combined usage

    [Test]
    public void Combined_AndOrNot_Works()
    {
        Assert.That(Eval("true and not false"), Is.True);
        Assert.That(Eval("false or not false"), Is.True);
        Assert.That(Eval("not true or not false"), Is.True);
    }

    [Test]
    public void Combined_MixedWithSymbols_Works()
    {
        // Can mix 'and'/'or'/'not' with &&/||/!
        Assert.That(Eval("true && true and true"), Is.True);
        Assert.That(Eval("false || true or false"), Is.True);
        Assert.That(Eval("!false and not false"), Is.True);
    }

    [Test]
    public void Combined_WithInOperator_Works()
    {
        Assert.That(Eval("(2 in [1, 2, 3]) and (5 in [4, 5, 6])"), Is.True);
        Assert.That(Eval("not (5 in [1, 2, 3])"), Is.True);
    }

    #endregion
}
