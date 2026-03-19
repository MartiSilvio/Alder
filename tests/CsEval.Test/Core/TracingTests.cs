namespace CsEval.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class TracingTests(CompilationMode mode)
{
    [Test]
    public void EvaluateWithTrace_ReturnsResultAndSteps()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        LanguageMode = LanguageMode.Extended
        });

        var trace = engine.EvaluateWithTrace("4 * 5 + 2");

        Assert.That(trace.Result, Is.EqualTo(22));
        Assert.That(trace.Steps, Is.Not.Empty);
        Assert.That(trace.Steps[^1].Value, Is.EqualTo(22));
        Assert.That(trace.Steps.Any(step => step.NodeKind.Contains("BoundBinaryExpr", StringComparison.Ordinal)), Is.True);
    }
}
