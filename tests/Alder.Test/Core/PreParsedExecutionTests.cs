namespace Alder.Test.Core;

[TestFixture]
public class PreParsedExecutionTests
{
    [Test]
    public void PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new AlderEngine();
        var expr = engine.Parse(@"
        {
            var sum = 0;
            foreach (var item in items) {
                sum = sum + item;
            }
            return sum;
        }");

        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(6));

        engine.SetVariable("items", new List<int> { 10, 20, 30 });
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(60));
    }

    [Test]
    public void TryParse_ValidExpression_Succeeds()
    {
        var engine = new AlderEngine();
        var success = engine.TryParse("{ foreach (var item in new[] {1,2,3}) { } return 0; }", out var expr, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void PreParsed_ParamsVariables_AreBoundByPosition()
    {
        var engine = new AlderEngine();
        var expression = engine.Parse("@0 + @1 + @2");

        var result = engine.Evaluate<int>(expression, 1, 2, 3);

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void PreParsed_TryEvaluate_WithDictionaryVariables_Succeeds()
    {
        var engine = new AlderEngine();
        var expression = engine.Parse("x + y");
        var variables = new Dictionary<string, object?>
        {
            ["x"] = 10,
            ["y"] = 15,
        };

        var success = engine.TryEvaluate<int>(expression, out var result, variables);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(25));
    }
}
