namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class ArithmeticTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_Arithmetic_Addition()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("1 + 2"), Is.EqualTo(3));
        Assert.That(engine.Evaluate("1.5 + 2.5"), Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_Arithmetic_Subtraction()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("5 - 3"), Is.EqualTo(2));
    }

    [Test]
    public void Eval_Arithmetic_Multiplication()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("3 * 4"), Is.EqualTo(12));
    }

    [Test]
    public void Eval_Arithmetic_Division()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int / int → int (truncating)
        Assert.That(engine.Evaluate("10 / 4"), Is.EqualTo(2));
    }

    [Test]
    public void Eval_Arithmetic_Division_Double()
    {
        var engine = CreateEngine(mode);
        // Use double to get fractional result
        Assert.That(engine.Evaluate("10.0 / 4.0"), Is.EqualTo(2.5));
    }

    [Test]
    public void Eval_Arithmetic_Modulo()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("10 % 3"), Is.EqualTo(1));
    }

    [Test]
    public void Eval_Arithmetic_Precedence()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("1 + 2 * 3"), Is.EqualTo(7));
        Assert.That(engine.Evaluate("(1 + 2) * 3"), Is.EqualTo(9));
    }

    [Test]
    public void Eval_Unary_Negation()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("-5"), Is.EqualTo(-5L).Or.EqualTo(-5));
        Assert.That(engine.Evaluate("-3.14"), Is.EqualTo(-3.14));
    }
}
