namespace CsEval.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class InterpolationConformanceTests(CompilationMode mode)
{
    private CsEvalEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.3 Interpolated string — complex expressions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void InterpolatedString_NestedBraces()
    {
        var result = Eval(@"
            var x = 5;
            return $""Value: {x + 1}"";
        ");
        Assert.That(result, Is.EqualTo("Value: 6"));
    }

    [Test]
    public void InterpolatedString_TernaryInside()
    {
        var result = Eval(@"
            var x = 5;
            return $""{(x > 3 ? ""big"" : ""small"")}"";
        ");
        Assert.That(result, Is.EqualTo("big"));
    }

    [Test]
    public void InterpolatedString_MethodCallInside()
    {
        var result = Eval(@"
            return $""{""hello"".ToUpper()}"";
        ");
        Assert.That(result, Is.EqualTo("HELLO"));
    }
}
