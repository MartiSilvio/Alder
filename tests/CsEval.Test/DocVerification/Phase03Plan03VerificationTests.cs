namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase03Plan03VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private CsEvalEngine TrustedEngine()
        => TestEngineFactory.Create(mode, CsEvalOptions.Default with
        {
            Sandbox = SandboxOptions.Trusted()
        });

    private object? Eval(string expr) => Engine().Evaluate(expr);
    private object? TrustedEval(string expr) => TrustedEngine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: Type declarations rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ClassDeclaration_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("class Foo { }"));

    [Test]
    public void StructDeclaration_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("struct Point { }"));

    [Test]
    public void InterfaceDeclaration_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("interface IFoo { }"));

    [Test]
    public void EnumDeclaration_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("enum Color { Red }"));

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: Async/await rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AsyncLambda_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("async () => 1"));

    [Test]
    public void AwaitExpression_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("await 1"));

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: Yield rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void YieldReturn_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("yield return 1"));

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: Unsafe code rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void UnsafeBlock_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("unsafe { }"));

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: Using declaration (C# 8) rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void UsingDeclaration_Rejected()
        => Assert.Catch<CsEvalException>(() =>
            TrustedEval("{ using var x = new System.IO.MemoryStream(); return 1; }"));

    // ═══════════════════════════════════════════════════════════════
    // NEGATIVE: this/base rejected
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void This_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("this"));

    [Test]
    public void Base_Rejected()
        => Assert.Catch<CsEvalException>(() => Eval("base"));

    // ═══════════════════════════════════════════════════════════════
    // POSITIVE: LINQ query syntax works
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LinqQuerySyntax_Works()
    {
        var result = TrustedEval(
            "{ var nums = new int[] {1, 2, 3}; return (from n in nums where n > 1 select n * 2).Count(); }");
        Assert.That(result, Is.EqualTo(2));
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITIVE: Pattern matching works
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void PatternMatching_Is_Works()
    {
        var result = Eval("{ var x = 42; return x is int n ? n : 0; }");
        Assert.That(result, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITIVE: Tuple deconstruction works
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TupleDeconstruction_Works()
    {
        var result = Eval("{ var (a, b) = (1, 2); return a + b; }");
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITIVE: Null-conditional works
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullConditional_Works()
    {
        var result = Eval("{ var s = (string)null; return s?.Length ?? -1; }");
        Assert.That(result, Is.EqualTo(-1));
    }

    // ═══════════════════════════════════════════════════════════════
    // POSITIVE: Range/index operators work
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IndexFromEnd_Works()
    {
        var result = TrustedEval("{ var arr = new int[] { 10, 20, 30 }; return arr[^1]; }");
        Assert.That(result, Is.EqualTo(30));
    }
}
