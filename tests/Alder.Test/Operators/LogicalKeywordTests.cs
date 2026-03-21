using Alder.Test._Infrastructure;

namespace Alder.Test.Operators;

// Engine-only: Alder-specific logical keywords (and, or, not) - not standard C# syntax

/// <summary>
/// Tests for 'and', 'or', 'not' keywords (Alder extension, not standard C#).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LogicalKeywordTests(CompilationMode mode)
{
    // Short-circuit tests (engine-only: use SetVariable)
    [Test]
    public void And_ShortCircuits()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Extended });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("false and (1/x > 0)"), Is.False);
    }

    [Test]
    public void Or_ShortCircuits()
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = LanguageMode.Extended });
        engine.SetVariable("x", 0);
        Assert.That(engine.Evaluate("true or (1/x > 0)"), Is.True);
    }

    [TestCase("{ var and = 1; return and; }", 1, TestName = "And_AsIdentifier")]
    [TestCase("{ var or = 2; return or; }", 2, TestName = "Or_AsIdentifier")]
    [TestCase("{ var not = 3; return not; }", 3, TestName = "Not_AsIdentifier")]
    public void ContextualKeyword_UsableAsIdentifierInStandardMode(string expr, int expected)
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }
}
