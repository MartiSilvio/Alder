namespace CsEval.Test;

[TestFixture]
public sealed class BoundCompilationTests
{
    [Test]
    public void StrictCompiled_ShouldEvaluateBoundCapableExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });
        engine.SetVariable("x", -4);
        engine.SetVariable("y", 2);
        engine.SetVariable("z", 3);

        var expression = engine.Parse("Math.Abs(x - y) + Math.Max(y, z)");
        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(9));

        var info = expression.GetCompiledInfo();
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldFallbackForBoundUnsupportedNodes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var expression = engine.Parse("numbers.Where((n) => n > 2).Count()");
        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(3));

        var info = expression.GetCompiledInfo();
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Pipeline, Is.EqualTo(CompiledPipeline.Ast));
    }
}
