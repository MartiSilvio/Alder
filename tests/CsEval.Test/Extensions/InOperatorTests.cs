using NUnit.Framework;

namespace CsEval.Test.Extensions;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class InOperatorTests(CompilationMode mode) 
{
    [Test]
    public void InOperator_NumberInArray_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("2 in [1, 2, 3]"), Is.True);
    }

    [Test]
    public void InOperator_NumberNotInArray_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("5 in [1, 2, 3]"), Is.False);
    }

    [Test]
    public void InOperator_StringInArray_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"b\" in [\"a\", \"b\", \"c\"]"), Is.True);
    }

    [Test]
    public void InOperator_StringNotInArray_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"x\" in [\"a\", \"b\", \"c\"]"), Is.False);
    }

    [Test]
    public void InOperator_SubstringInString_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"bc\" in \"abcd\""), Is.True);
    }

    [Test]
    public void InOperator_SubstringNotInString_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"xy\" in \"abcd\""), Is.False);
    }

    [Test]
    public void InOperator_WithVariable_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new List<int> { 1, 2, 3, 4, 5 });
        Assert.That(engine.Evaluate("3 in arr"), Is.True);
    }

    [Test]
    public void InOperator_WithVariableValue_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 3);
        Assert.That(engine.Evaluate("x in [1, 2, 3, 4, 5]"), Is.True);
    }

    [Test]
    public void InOperator_CombinedWithAnd_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("(2 in [1, 2, 3]) && (5 in [4, 5, 6])"), Is.True);
    }

    [Test]
    public void InOperator_CombinedWithOr_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("(10 in [1, 2, 3]) || (5 in [4, 5, 6])"), Is.True);
    }

    [Test]
    public void InOperator_NegatedWithNot_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("!(5 in [1, 2, 3])"), Is.True);
    }

    [Test]
    public void InOperator_InTernary_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("2 in [1, 2, 3] ? \"yes\" : \"no\""), Is.EqualTo("yes"));
    }

    [Test]
    public void InOperator_NullInArray_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("null in [1, null, 3]"), Is.True);
    }

    [Test]
    public void InOperator_NullNotInArray_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("null in [1, 2, 3]"), Is.False);
    }

    [Test]
    public void InOperator_WithNullCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("1 in arr"));
    }

    [Test]
    public void InOperator_EmptyArray_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("1 in []"), Is.False);
    }

    [Test]
    public void InOperator_MixedTypeArray_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"hello\" in [1, \"hello\", true]"), Is.True);
    }
}
