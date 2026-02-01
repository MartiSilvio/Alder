using CsEval.Parsing;

namespace CsEval.Test.Extensions;

/// <summary>
/// Tests for JavaScript-friendly syntax features that work alongside C# syntax.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class JsFriendlySyntaxTests(CompilationMode mode)
{
    // Expressions that evaluate to expected values
    [TestCase("{ let x = 42; return x; }", 42, TestName = "Let_TreatedAsVar")]
    [TestCase("undefined", null, TestName = "Undefined_IsNull")]
    [TestCase("undefined == null", true, TestName = "Undefined_EqualsNull")]
    [TestCase("undefined === null", true, TestName = "Undefined_StrictEqualsNull")]
    [TestCase("5 === 5", true, TestName = "StrictEquality_SameInt_True")]
    [TestCase("5 === 10", false, TestName = "StrictEquality_DifferentInt_False")]
    [TestCase("'hello' === 'hello'", true, TestName = "StrictEquality_SameString_True")]
    [TestCase("5 !== 10", true, TestName = "StrictInequality_DifferentInt_True")]
    [TestCase("5 !== 5", false, TestName = "StrictInequality_SameInt_False")]
    [TestCase("'a' !== 'b'", true, TestName = "StrictInequality_DifferentString_True")]
    public void MatchesExpected(string expr, object? expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    // Reserved keyword tests
    [TestCase("{ const x = 'hello'; return x; }", TestName = "Const_IsReservedKeyword")]
    [TestCase("{ var super = 1; return super; }", TestName = "Super_IsReservedKeyword")]
    public void ReservedKeyword_ThrowsParserException(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<ParserException>(() => engine.Evaluate(expr));
    }

    // Strict equality with variables
    [Test]
    public void StrictEquality_InExpression_WithVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("x === 5 ? 'yes' : 'no'"), Is.EqualTo("yes"));
        Assert.That(engine.Evaluate("x !== 5 ? 'yes' : 'no'"), Is.EqualTo("no"));
    }

    // Anonymous object tests (require dictionary casting)
    [Test]
    public void AnonymousObject_SingleProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new { Name = 'John' }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
    }

    [Test]
    public void AnonymousObject_MultipleProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new { Name = 'John', Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result!["Age"], Is.EqualTo(30));
    }
}
