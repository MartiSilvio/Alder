namespace CsEval.Test.Core;

[TestFixture]
public class ValidationTests
{
    [Test]
    public void TryParse_ValidExpression_ReturnsTrue()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("1 + 2", out var result, out var error);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void TryParse_InvalidExpression_ReturnsFalse()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("1 +", out var result, out var error);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void TryParse_UnmatchedParenthesis_ReturnsFalse()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("(1 + 2", out var result, out var error);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void TryParse_InvalidOperator_ReturnsFalse()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("1 @ 2", out var result, out var error);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void TryParse_ComplexValidExpression_ReturnsTrue()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("items.Where((x) => x > 2).Select((x) => x * 2)", out var result, out var error);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void TryParse_ValidExpression_CanBeEvaluated()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("x * 2", out var result, out _);

        Assert.That(success, Is.True);

        engine.SetVariable("x", 5L);
        var evalResult = engine.Evaluate(result!);
        Assert.That(evalResult, Is.EqualTo(10));
    }

    [Test]
    public void TryParse_OverloadWithoutError_Works()
    {
        var engine = new CsEvalEngine();

        Assert.That(engine.TryParse("1 + 2", out var valid), Is.True);
        Assert.That(valid, Is.Not.Null);

        Assert.That(engine.TryParse("1 +", out var invalid), Is.False);
        Assert.That(invalid, Is.Null);
    }

    [Test]
    public void TryParse_EmptyExpression_ReturnsFalse()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("", out var result, out var error);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void TryParse_UnterminatedString_ReturnsFalse()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("\"hello", out var result, out var error);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(error, Is.Not.Null);
    }
}
