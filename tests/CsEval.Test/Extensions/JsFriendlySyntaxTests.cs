using CsEval.Parsing;

namespace CsEval.Test.Extensions;

/// <summary>
/// Tests for JavaScript-friendly syntax features that work alongside C# syntax.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class JsFriendlySyntaxTests(CompilationMode mode)
{
    // Reserved keyword tests (engine-only: testing error behavior)
    [TestCase("{ const x = \"hello\"; return x; }", TestName = "Const_IsReservedKeyword")]
    [TestCase("{ var super = 1; return super; }", TestName = "Super_IsReservedKeyword")]
    public void ReservedKeyword_ThrowsCsEvalParserException(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        Assert.Throws<CsEvalParserException>(() => engine.Evaluate(expr));
    }

    // Strict equality with variables
    [Test]
    public void StrictEquality_InExpression_WithVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x === 5 ? \"yes\" : \"no\""), Is.EqualTo("yes"));
        Assert.That(engine.Evaluate("x !== 5 ? \"yes\" : \"no\""), Is.EqualTo("no"));
    }

    // Anonymous object tests (require dictionary casting)
    [Test]
    public void AnonymousObject_SingleProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate("new { Name = \"John\" }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
    }

    [Test]
    public void AnonymousObject_MultipleProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result!["Age"], Is.EqualTo(30));
    }
}
