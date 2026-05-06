using Alder.Test._Infrastructure;

namespace Alder.Test.Core;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class ValidationTests(CompilationMode mode)
{
    [Test]
    public void TryParse_ValidExpression_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("1 + 2", out var result, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryParse_InvalidExpression_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("1 +", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostics[0].Code, Is.Not.Null);
        Assert.That(diagnostics[0].Span, Is.Not.EqualTo(default(Text.TextSpan)));
    }

    [Test]
    public void TryParse_UnmatchedParenthesis_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("(1 + 2", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
    }

    [Test]
    public void TryParse_InvalidOperator_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("1 @ 2", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
    }

    [Test]
    public void TryParse_ComplexValidExpression_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("items.Where((x) => x > 2).Select((x) => x * 2)", out var result, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryParse_ValidExpression_CanBeEvaluated()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("x * 2", out var result, out _);

        Assert.That(success, Is.True);

        engine.SetVariable("x", 5L);
        var evalResult = engine.Evaluate(result!);
        Assert.That(evalResult, Is.EqualTo(10));
    }

    [Test]
    public void TryParse_CallerCanIgnoreDiagnostics_WhenUnused()
    {
        var engine = TestEngineFactory.Create(mode);

        Assert.That(engine.TryParse("1 + 2", out var valid, out _), Is.True);
        Assert.That(valid, Is.Not.Null);

        Assert.That(engine.TryParse("1 +", out var invalid, out _), Is.False);
        Assert.That(invalid, Is.Null);
    }

    [Test]
    public void TryParse_EmptyExpression_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
    }

    [Test]
    public void TryParse_UnterminatedString_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("\"hello", out var result, out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
    }
}
