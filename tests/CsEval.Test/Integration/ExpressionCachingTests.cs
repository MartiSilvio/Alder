namespace CsEval.Test.Integration;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class ExpressionCachingTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Parse_ReturnsCsEvalExpression()
    {
        var engine = CreateEngine(mode);
        var expression = engine.Parse("1 + 2");

        Assert.That(expression, Is.Not.Null);
        Assert.That(expression.Expression, Is.EqualTo("1 + 2"));
    }

    [Test]
    public void EvaluateParsed_ReturnsCorrectResult()
    {
        var engine = CreateEngine(mode);
        var expression = engine.Parse("1 + 2");

        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void EvaluateParsed_MultipleTimesWithDifferentVariables()
    {
        var engine = CreateEngine(mode);
        var expression = engine.Parse("x * 2");

        engine.SetVariable("x", 5);
        var result1 = engine.Evaluate(expression);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("x", 10);
        var result2 = engine.Evaluate(expression);
        Assert.That(result2, Is.EqualTo(20));

        engine.SetVariable("x", 100);
        var result3 = engine.Evaluate(expression);
        Assert.That(result3, Is.EqualTo(200));
    }

    [Test]
    public void EvaluateParsed_Generic()
    {
        var engine = CreateEngine(mode);
        var expression = engine.Parse("1 + 2");

        var result = engine.Evaluate<long>(expression);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void EvaluateParsed_ComplexExpression()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var expression = engine.Parse("items.Where((x) => x > threshold).Select((x) => x * multiplier)");

        engine.SetVariable("threshold", 2);
        engine.SetVariable("multiplier", 2);
        var result1 = engine.Evaluate(expression) as List<object?>;
        Assert.That(result1, Is.EqualTo(new List<object?> { 6, 8, 10 }));

        engine.SetVariable("threshold", 3);
        engine.SetVariable("multiplier", 10);
        var result2 = engine.Evaluate(expression) as List<object?>;
        Assert.That(result2, Is.EqualTo(new List<object?> { 40, 50 }));
    }

    [Test]
    public void EvaluateParsed_WithModuleCalls_ReturnsDifferentResults()
    {
        var engine = CreateEngine(mode);
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
        var engine1 = CreateEngine(mode);
        var expression = engine1.Parse("x + y");

        engine1.SetVariable("x", 1);
        engine1.SetVariable("y", 2);
        var result1 = engine1.Evaluate(expression);
        Assert.That(result1, Is.EqualTo(3));

        var engine2 = CreateEngine(mode);
        engine2.SetVariable("x", 10);
        engine2.SetVariable("y", 20);
        var result2 = engine2.Evaluate(expression);
        Assert.That(result2, Is.EqualTo(30));
    }
}
