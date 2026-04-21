using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Extensions;

/// <summary>
/// Tests for polyglot syntax sugar features that work alongside C# syntax.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PolyglotSyntaxTests(CompilationMode mode)
{
    [TestCase("{ var super = 1; return super; }", TestName = "Super_IsReservedKeyword")]
    public void ReservedKeyword_ThrowsAlderException(string expr)
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1003));
    }

    [Test]
    public void ConstDeclaration_DefinesImmutableLocal()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        Assert.That(engine.Evaluate("{ const int x = 7; return x * x; }"), Is.EqualTo(49));
    }

    [Test]
    public void ConstDeclaration_Assignment_Throws()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 7; x = 8; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
    }

    // Strict equality with variables
    [Test]
    public void StrictEquality_InExpression_WithVariable()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate("""x === 5 ? "yes" : "no" """), Is.EqualTo("yes"));
        Assert.That(engine.Evaluate("""x !== 5 ? "yes" : "no" """), Is.EqualTo("no"));
    }

    // Structural projection tests
    [Test]
    public void AnonymousObject_SingleProperty()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var result = engine.Evaluate<object>("""new { Name = "John" } """);
        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("John"));
    }

    [Test]
    public void AnonymousObject_MultipleProperties()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var result = engine.Evaluate<object>("""new { Name = "John", Age = 30 } """);
        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("John"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Age"), Is.EqualTo(30));
    }

    [Test]
    public void AnonymousObject_GenericObject_Works()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);
        var result = engine.Evaluate<object>("""new { Name = "John", Age = 30 } """);

        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("John"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Age"), Is.EqualTo(30));
    }
}
