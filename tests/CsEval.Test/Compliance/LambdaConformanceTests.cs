namespace CsEval.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LambdaConformanceTests(CompilationMode mode)
{
    private CsEvalEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §12.19 Lambda expressions — various forms
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Lambda_ExpressionBody()
    {
        var result = Eval(@"
            Func<int, int> square = x => x * x;
            return square(5);
        ");
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void Lambda_StatementBody()
    {
        var result = Eval(@"
            Func<int, int> abs = x => { if (x < 0) return -x; return x; };
            return abs(-5);
        ");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Lambda_Closure_CapturesVariable()
    {
        var result = Eval(@"{
            var offset = 10;
            Func<int, int> add = x => x + offset;
            return add(5);
        }");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Lambda_Closure_MutatesVariable()
    {
        var result = Eval(@"{
            var count = 0;
            Action inc = () => count++;
            inc();
            inc();
            inc();
            return count;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Lambda_MultiParameter()
    {
        var result = Eval(@"{
            Func<int, int, int> add = (a, b) => a + b;
            return add(3, 4);
        }");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Lambda_NoParameter()
    {
        var result = Eval(@"
            Func<int> f = () => 42;
            return f();
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: nested lambdas
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedLambda_ReturnFunction()
    {
        var result = Eval(@"
            Func<int, Func<int, int>> adder = x => y => x + y;
            var add5 = adder(5);
            return add5(3);
        ");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void NestedLambda_ImmediateInvoke()
    {
        var result = Eval(@"
            Func<int, Func<int, int>> multiply = x => y => x * y;
            return multiply(3)(4);
        ");
        Assert.That(result, Is.EqualTo(12));
    }
}
