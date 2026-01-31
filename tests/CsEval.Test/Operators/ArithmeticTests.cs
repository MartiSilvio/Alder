namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ArithmeticTests(CompilationMode mode)
{
    [Test]
    public async Task Eval_Arithmetic_Addition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 + 2";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(3));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "1.5 + 2.5";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(4.0));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Arithmetic_Subtraction()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "5 - 3";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(2));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Arithmetic_Multiplication()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "3 * 4";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(12));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Arithmetic_Division()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "10 / 4";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(2));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Arithmetic_Division_Double()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "10.0 / 4.0";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(2.5));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Arithmetic_Modulo()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "10 % 3";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Arithmetic_Precedence()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 + 2 * 3";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(7));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "(1 + 2) * 3";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(9));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Unary_Negation()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "-5";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(-5L).Or.EqualTo(-5));

        const string expr2 = "-3.14";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(-3.14));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }
}
