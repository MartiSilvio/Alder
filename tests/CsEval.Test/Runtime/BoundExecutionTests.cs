namespace CsEval.Test;

[TestFixture]
public sealed class BoundExecutionTests
{
    [Test]
    public void Interpreted_ShouldUseBoundEvaluator_WhenBindingIsSupported()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.Interpreted
        });
        engine.SetVariable("x", -4);
        engine.SetVariable("y", 2);
        engine.SetVariable("z", 3);

        var expression = engine.Parse("Math.Abs(x - y) + Math.Max(y, z)");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo(9));
        Assert.That(expression.BoundExecutionCount, Is.GreaterThan(0));
    }
}
