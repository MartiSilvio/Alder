using Alder.Test._Infrastructure;

namespace Alder.Test.Loops;

// Engine-only: this file keeps only while-specific control-flow coverage.
// Generic limit, cancellation, parsing, and pre-parsed API coverage lives elsewhere.

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class WhileLoopTests(CompilationMode mode)
{
    [Test]
    public void WhileLoop_Break_TryParse_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("{ var i = 0; while (true) { break; } return i; }", out var expr, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void WhileLoop_Continue_TryParse_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode);
        var success = engine.TryParse("{ var i = 0; while (i < 5) { i = i + 1; continue; } return i; }", out var expr, out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void WhileLoop_BreakWithSemicolon_ParsesCorrectly()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("""
            {
                var i = 0;
                while (i < 10) {
                    i = i + 1;
                    break;
                }
                return i;
            }
            """);

        Assert.That(result, Is.EqualTo(1));
    }
}
