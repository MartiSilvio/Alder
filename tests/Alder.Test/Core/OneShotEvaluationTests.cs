using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class OneShotEvaluationTests(CompilationMode mode)
{
    [Test]
    public void Evaluate_WithVariables_CanAccessVariables()
    {
        var engine = TestEngineFactory.Create(mode);
        var variables = new Dictionary<string, object?>
        {
            { "x", 10 },
            { "y", 20 }
        };

        var result = engine.Evaluate("x + y", variables);

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Evaluate_WithVariables_DoesNotLeakToEngine()
    {
        var engine = TestEngineFactory.Create(mode);
        var variables = new Dictionary<string, object?>
        {
            { "x", 10 }
        };

        engine.Evaluate("x * 2", variables);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x"));
        Assert.That(ex!.Message, Does.Contain("does not exist in the current context"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void Evaluate_WithVariables_ShadowsEngineVariables()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 100);

        var variables = new Dictionary<string, object?>
        {
            { "x", 5 }
        };

        var result = engine.Evaluate("x", variables);

        Assert.That(result, Is.EqualTo(5));

        // Ensure original is untouched in the parent scope
        Assert.That(engine.Evaluate("x"), Is.EqualTo(100));
    }
}
