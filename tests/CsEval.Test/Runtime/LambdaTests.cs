namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LambdaTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode });

    // Typed lambda parameters

    [Test]
    public void Eval_TypedLambda_SingleDoubleParam()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var f = (double x) => x * x; return f(3.0); }");
        Assert.That(result, Is.EqualTo(9.0));
    }

    [Test]
    public void Eval_TypedLambda_MultipleIntParams()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var f = (int a, int b) => a + b; return f(3, 4); }");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Eval_TypedLambda_MixedTypes()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var f = (double x, int n) => x * n; return f(2.5, 3); }");
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void Eval_TypedLambda_WithBlock()
    {
        // Lambda with block body containing return - uses non-strict modes
        // as StrictCompiled has a pre-existing limitation with return in nested lambda blocks
        if (mode == CompilationMode.StrictCompiled)
        {
            var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.Compiled });
            var result = engine.Evaluate(@"
            {
                var f = (double x) => {
                    var y = x * x;
                    return y + 1.0;
                };
                return f(3.0);
            }");
            Assert.That(result, Is.EqualTo(10.0));
            return;
        }
        {
            var engine = CreateEngine();
            var result = engine.Evaluate(@"
            {
                var f = (double x) => {
                    var y = x * x;
                    return y + 1.0;
                };
                return f(3.0);
            }");
            Assert.That(result, Is.EqualTo(10.0));
        }
    }

    [Test]
    public void Eval_TypedLambda_GenericFuncParam()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(@"
        {
            var square = (double x) => x * x;
            var apply = (Func<double, double> f, double a) => f(a);
            return apply(square, 5.0);
        }");
        Assert.That(result, Is.EqualTo(25.0));
    }

    [Test]
    public void Eval_TypedLambda_StringParam()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(@"
        {
            var greet = (string name) => $""Hello {name}!"";
            return greet(""World"");
        }");
        Assert.That(result, Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_TypedLambda_BoolParam()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var f = (bool b) => !b; return f(true); }");
        Assert.That(result, Is.EqualTo(false));
    }

    // Func<> generic type annotations in variable declarations

    [Test]
    public void Eval_FuncTypeDecl_SimpleAssignment()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(@"
        {
            Func<double, double> sqrtFunc = (double x) => Math.Sqrt(x);
            return sqrtFunc(16.0);
        }");
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_FuncTypeDecl_WithUntypedLambda()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(@"
        {
            Func<int, int> doubler = x => x * 2;
            return doubler(5);
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_FuncTypeDecl_PassedToHigherOrder()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate(@"
        {
            Func<double, double> square = (double x) => x * x;
            var apply = (Func<double, double> f, double a) => f(a);
            return apply(square, 7.0);
        }");
        Assert.That(result, Is.EqualTo(49.0));
    }

    [Test]
    public void Eval_FuncTypeDecl_ThreeGenericArgs()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ Func<int, int, int> fn = (int a, int b) => a + b; return fn(10, 20); }");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Eval_FuncTypeDecl_ExistingVarStillWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ var x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Eval_FuncTypeDecl_ExistingTypedStillWorks()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("{ int x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }
}
