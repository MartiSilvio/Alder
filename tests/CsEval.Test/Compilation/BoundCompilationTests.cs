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
    public void StrictCompiled_ShouldUseBoundPipeline_ForSwitchStatements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });
        var expression = engine.Parse(
            "{ var x = 7; switch (x) { case > 0: return 1; default: return 0; } }");
        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(1));

        var info = expression.GetCompiledInfo();
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForNamedArguments()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        var namedArgExpr = engine.Parse("Math.Round(value: 3.14159, digits: 2)");
        var namedArgResult = engine.Evaluate(namedArgExpr);
        Assert.That(namedArgResult, Is.EqualTo(3.14d));
        Assert.That(namedArgExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldEvaluateTypeArgumentInvocation()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        var typeArgExpr = engine.Parse("Array.Empty<int>().Length");
        var typeArgResult = engine.Evaluate(typeArgExpr);
        Assert.That(typeArgResult, Is.EqualTo(0));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForExtendedSyntaxExpressions()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled,
            LanguageMode = LanguageMode.Extended
        });

        var chainedExpr = engine.Parse("1 < 2 < 3");
        var chainedResult = engine.Evaluate(chainedExpr);
        Assert.That(chainedResult, Is.EqualTo(true));
        Assert.That(chainedExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var rangeExpr = engine.Parse("(1..5).Count()");
        var rangeResult = engine.Evaluate(rangeExpr);
        Assert.That(rangeResult, Is.EqualTo(5));
        Assert.That(rangeExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForMutationExpressions()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled,
            LanguageMode = LanguageMode.Extended
        });

        engine.SetVariable<int>("x", 10);
        engine.SetVariable<int?>("nullable", null);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });
        engine.SetVariable("obj", new Dictionary<string, object?> { ["a"] = 5 });

        var assignExpr = engine.Parse("x = x + 1");
        Assert.That(engine.Evaluate(assignExpr), Is.EqualTo(11));
        Assert.That(assignExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var compoundExpr = engine.Parse("x += 5");
        Assert.That(engine.Evaluate(compoundExpr), Is.EqualTo(16));
        Assert.That(compoundExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var nullCoalesceAssignExpr = engine.Parse("nullable ??= 42");
        Assert.That(engine.Evaluate(nullCoalesceAssignExpr), Is.EqualTo(42));
        Assert.That(nullCoalesceAssignExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var memberAssignExpr = engine.Parse("obj.a = 9");
        Assert.That(engine.Evaluate(memberAssignExpr), Is.EqualTo(9));
        Assert.That(memberAssignExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var indexIncrementExpr = engine.Parse("arr[0]++");
        Assert.That(engine.Evaluate(indexIncrementExpr), Is.EqualTo(1));
        Assert.That(indexIncrementExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForIsPatternExpressions()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        engine.SetVariable("x", 42);
        var expression = engine.Parse("x is > 10 and < 100");

        var result = engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(true));
        Assert.That(expression.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForBlockIfReturn()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        var expression = engine.Parse("{ var x = 10; if (x > 5) { return \"big\"; } return \"small\"; }");
        var result = engine.Evaluate(expression);

        Assert.That(result, Is.EqualTo("big"));
        Assert.That(expression.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForLoopConstructs()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        var whileExpr = engine.Parse("{ var i = 0; var sum = 0; while (i < 5) { i++; if (i == 3) continue; sum += i; } return sum; }");
        Assert.That(engine.Evaluate(whileExpr), Is.EqualTo(12));
        Assert.That(whileExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        var forExpr = engine.Parse("{ var sum = 0; for (var i = 0; i < 5; i++) { if (i == 3) break; sum += i; } return sum; }");
        Assert.That(engine.Evaluate(forExpr), Is.EqualTo(3));
        Assert.That(forExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });
        var foreachExpr = engine.Parse("{ var sum = 0; foreach (var item in items) { if (item == 4) break; sum += item; } return sum; }");
        Assert.That(engine.Evaluate(foreachExpr), Is.EqualTo(6));
        Assert.That(foreachExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }

    [Test]
    public void StrictCompiled_ShouldUseBoundPipeline_ForUsingAndLockStatements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = CompilationMode.StrictCompiled
        });

        engine.SetVariable("res", new System.IO.MemoryStream());
        var usingExpr = engine.Parse("{ var x = 0; using (res) { x = 42; } return x; }");
        Assert.That(engine.Evaluate(usingExpr), Is.EqualTo(42));
        Assert.That(usingExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));

        engine.SetVariable("lockObj", new object());
        var lockExpr = engine.Parse("{ var x = 0; lock (lockObj) { x = 10; } return x; }");
        Assert.That(engine.Evaluate(lockExpr), Is.EqualTo(10));
        Assert.That(lockExpr.GetCompiledInfo()!.Pipeline, Is.EqualTo(CompiledPipeline.Bound));
    }
}
