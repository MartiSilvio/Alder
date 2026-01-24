namespace CsEval.Test.Evaluator;

[TestFixture]
public class ArithmeticTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Arithmetic_Addition()
    {
        Assert.That(Eval("1 + 2"), Is.EqualTo(3));
        Assert.That(Eval("1.5 + 2.5"), Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_Arithmetic_Subtraction()
    {
        Assert.That(Eval("5 - 3"), Is.EqualTo(2));
    }

    [Test]
    public void Eval_Arithmetic_Multiplication()
    {
        Assert.That(Eval("3 * 4"), Is.EqualTo(12));
    }

    [Test]
    public void Eval_Arithmetic_Division()
    {
        // C# behavior: int / int → int (truncating)
        Assert.That(Eval("10 / 4"), Is.EqualTo(2));
    }

    [Test]
    public void Eval_Arithmetic_Division_Double()
    {
        // Use double to get fractional result
        Assert.That(Eval("10.0 / 4.0"), Is.EqualTo(2.5));
    }

    [Test]
    public void Eval_Arithmetic_Modulo()
    {
        Assert.That(Eval("10 % 3"), Is.EqualTo(1));
    }

    [Test]
    public void Eval_Arithmetic_Precedence()
    {
        Assert.That(Eval("1 + 2 * 3"), Is.EqualTo(7));
        Assert.That(Eval("(1 + 2) * 3"), Is.EqualTo(9));
    }

    [Test]
    public void Eval_Unary_Negation()
    {
        Assert.That(Eval("-5"), Is.EqualTo(-5L));
        Assert.That(Eval("-3.14"), Is.EqualTo(-3.14));
    }
}
