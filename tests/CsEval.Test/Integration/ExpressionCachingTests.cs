using NUnit.Framework;

namespace CsEval.Test.Integration;

[TestFixture]
public class ExpressionCachingTests
{
    [Test]
    public void Parse_ReturnsCsEvalExpression()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("1 + 2");

        Assert.That(expression, Is.Not.Null);
        Assert.That(expression.Expression, Is.EqualTo("1 + 2"));
    }

    [Test]
    public void EvaluateParsed_ReturnsCorrectResult()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("1 + 2");

        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void EvaluateParsed_MultipleTimesWithDifferentVariables()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("x * 2");

        engine.SetVariable("x", 5L);
        var result1 = engine.Evaluate(expression);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("x", 10L);
        var result2 = engine.Evaluate(expression);
        Assert.That(result2, Is.EqualTo(20));

        engine.SetVariable("x", 100L);
        var result3 = engine.Evaluate(expression);
        Assert.That(result3, Is.EqualTo(200));
    }

    [Test]
    public void EvaluateParsed_Generic()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("1 + 2");

        var result = engine.Evaluate<long>(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task EvaluateParsedAsync_ReturnsCorrectResult()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("1 + 2");

        var result = await engine.EvaluateAsync(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task EvaluateParsedAsync_Generic()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("1 + 2");

        var result = await engine.EvaluateAsync<long>(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void EvaluateParsed_ComplexExpression()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new List<object?> { 1L, 2L, 3L, 4L, 5L });

        var expression = engine.Parse("items.Where((x) => x > threshold).Select((x) => x * multiplier)");

        engine.SetVariable("threshold", 2L);
        engine.SetVariable("multiplier", 2L);
        var result1 = engine.Evaluate(expression) as List<object?>;
        Assert.That(result1, Is.EqualTo(new List<object?> { 6L, 8L, 10L }));

        engine.SetVariable("threshold", 3L);
        engine.SetVariable("multiplier", 10L);
        var result2 = engine.Evaluate(expression) as List<object?>;
        Assert.That(result2, Is.EqualTo(new List<object?> { 40L, 50L }));
    }

    [Test]
    public void EvaluateParsed_WithModuleCalls_ReturnsDifferentResults()
    {
        var engine = new CsEvalEngine();
        var expression = engine.Parse("Math.Max(a, b)");

        engine.SetVariable("a", 5.0);
        engine.SetVariable("b", 10.0);
        var result1 = engine.Evaluate(expression);
        Assert.That(result1, Is.EqualTo(10.0));

        engine.SetVariable("a", 100.0);
        engine.SetVariable("b", 50.0);
        var result2 = engine.Evaluate(expression);
        Assert.That(result2, Is.EqualTo(100.0));
    }

    [Test]
    public void ParsedExpressionCanBeReusedAcrossMultipleEngines()
    {
        var engine1 = new CsEvalEngine();
        var expression = engine1.Parse("x + y");

        engine1.SetVariable("x", 1L);
        engine1.SetVariable("y", 2L);
        var result1 = engine1.Evaluate(expression);
        Assert.That(result1, Is.EqualTo(3));

        var engine2 = new CsEvalEngine();
        engine2.SetVariable("x", 10L);
        engine2.SetVariable("y", 20L);
        var result2 = engine2.Evaluate(expression);
        Assert.That(result2, Is.EqualTo(30));
    }
}
