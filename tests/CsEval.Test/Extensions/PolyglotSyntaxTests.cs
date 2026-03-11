using System.Dynamic;
using CsEval.Parsing;

namespace CsEval.Test.Extensions;

/// <summary>
/// Tests for polyglot syntax sugar features that work alongside C# syntax.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PolyglotSyntaxTests(CompilationMode mode)
{
    [TestCase("{ var super = 1; return super; }", TestName = "Super_IsReservedKeyword")]
    public void ReservedKeyword_ThrowsCsEvalParserException(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        Assert.Throws<CsEvalParserException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void ConstDeclaration_DefinesImmutableLocal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        Assert.That(engine.Evaluate("{ const int x = 7; return x * x; }"), Is.EqualTo(49));
    }

    [Test]
    public void ConstDeclaration_Assignment_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("{ const int x = 7; x = 8; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0131));
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
        var result = engine.Evaluate<IDictionary<string, object?>>("new { Name = \"John\" }");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
    }

    [Test]
    public void AnonymousObject_MultipleProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate<IDictionary<string, object?>>("new { Name = \"John\", Age = 30 }");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result!["Age"], Is.EqualTo(30));
    }

    [Test]
    public void AnonymousObject_GenericExpandoObject_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });
        var result = engine.Evaluate<ExpandoObject>("new { Name = \"John\", Age = 30 }");

        Assert.That(result, Is.Not.Null);
        var dict = (IDictionary<string, object?>)result!;
        Assert.That(dict["Name"], Is.EqualTo("John"));
        Assert.That(dict["Age"], Is.EqualTo(30));
    }
}
