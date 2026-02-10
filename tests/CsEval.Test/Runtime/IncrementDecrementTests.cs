// Engine-only: pre-parsed engine reuse pattern.
// Migratable parity tests extracted to TestData/Runtime/IncrementDecrement/*.csx.

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IncrementDecrementTests(CompilationMode mode)
{
    #region Pre-Parsed (Engine-Only)

    // Engine-only: engine.Parse() + SetVariable reuse pattern
    [Test]
    public void IncrementDecrement_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x++;
            ++x;
            return x;
        }");

        engine.SetVariable("startVal", 0L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(2L));

        engine.SetVariable("startVal", 100L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(102L));
    }

    #endregion
}
