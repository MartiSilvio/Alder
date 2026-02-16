namespace CsEval.Test.Operators;

// Engine-only: CsEval-specific logical keywords (and, or, not) - not standard C# syntax

/// <summary>
/// Tests for 'and', 'or', 'not' keywords (CsEval extension, not standard C#).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalKeywordTests(CompilationMode mode)
{
    // Short-circuit tests (engine-only: use SetVariable)
    [Test]
    public void And_ShortCircuits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("false and (1/x > 0)"), Is.False);
    }

    [Test]
    public void Or_ShortCircuits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("true or (1/x > 0)"), Is.True);
    }

}
