using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class InOperatorTests : EvaluatorTestBase
{
    [Test]
    public void InOperator_NumberInArray_ReturnsTrue()
    {
        Assert.That(Eval("2 in [1, 2, 3]"), Is.True);
    }

    [Test]
    public void InOperator_NumberNotInArray_ReturnsFalse()
    {
        Assert.That(Eval("5 in [1, 2, 3]"), Is.False);
    }

    [Test]
    public void InOperator_StringInArray_ReturnsTrue()
    {
        Assert.That(Eval("\"b\" in [\"a\", \"b\", \"c\"]"), Is.True);
    }

    [Test]
    public void InOperator_StringNotInArray_ReturnsFalse()
    {
        Assert.That(Eval("\"x\" in [\"a\", \"b\", \"c\"]"), Is.False);
    }

    [Test]
    public void InOperator_SubstringInString_ReturnsTrue()
    {
        Assert.That(Eval("\"bc\" in \"abcd\""), Is.True);
    }

    [Test]
    public void InOperator_SubstringNotInString_ReturnsFalse()
    {
        Assert.That(Eval("\"xy\" in \"abcd\""), Is.False);
    }

    [Test]
    public void InOperator_WithVariable_Works()
    {
        var context = new EvalContext();
        context.Define("arr", new List<int> { 1, 2, 3, 4, 5 });
        Assert.That(Eval("3 in arr", context), Is.True);
    }

    [Test]
    public void InOperator_WithVariableValue_Works()
    {
        var context = new EvalContext();
        context.Define("x", 3);
        Assert.That(Eval("x in [1, 2, 3, 4, 5]", context), Is.True);
    }

    [Test]
    public void InOperator_CombinedWithAnd_Works()
    {
        Assert.That(Eval("(2 in [1, 2, 3]) && (5 in [4, 5, 6])"), Is.True);
    }

    [Test]
    public void InOperator_CombinedWithOr_Works()
    {
        Assert.That(Eval("(10 in [1, 2, 3]) || (5 in [4, 5, 6])"), Is.True);
    }

    [Test]
    public void InOperator_NegatedWithNot_Works()
    {
        Assert.That(Eval("!(5 in [1, 2, 3])"), Is.True);
    }

    [Test]
    public void InOperator_InTernary_Works()
    {
        Assert.That(Eval("2 in [1, 2, 3] ? \"yes\" : \"no\""), Is.EqualTo("yes"));
    }

    [Test]
    public void InOperator_NullInArray_Works()
    {
        Assert.That(Eval("null in [1, null, 3]"), Is.True);
    }

    [Test]
    public void InOperator_NullNotInArray_ReturnsFalse()
    {
        Assert.That(Eval("null in [1, 2, 3]"), Is.False);
    }

    [Test]
    public void InOperator_WithNullCollection_Throws()
    {
        var context = new EvalContext();
        context.Define("arr", null);
        Assert.Throws<CsEval.Evaluation.EvalException>(() => Eval("1 in arr", context));
    }

    [Test]
    public void InOperator_EmptyArray_ReturnsFalse()
    {
        Assert.That(Eval("1 in []"), Is.False);
    }

    [Test]
    public void InOperator_MixedTypeArray_Works()
    {
        Assert.That(Eval("\"hello\" in [1, \"hello\", true]"), Is.True);
    }
}
