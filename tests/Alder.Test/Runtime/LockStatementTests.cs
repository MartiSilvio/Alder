using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class LockStatementTests(CompilationMode mode)
{
    [Test]
    public void LockOnNull_ThrowsAlderExceptionWithConsistentMessage()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ object o = null; lock (o) { } }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0185));
        Assert.That(ex.Message, Does.Contain("CS0185"));
    }

    [Test]
    public void UsingStatement_DisposesIAsyncDisposableInAllModes()
    {
        var probe = new AsyncDisposeProbe();
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("probe", probe);

        engine.Evaluate("{ using (probe) { } }");

        Assert.That(probe.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void UsingResourceDeclaration_Assignment_ThrowsCs1656()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("probe", new AsyncDisposeProbe());
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("{ using (var r = probe) { r = null; } }"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1656));
    }

    [Test]
    public void UsingResourceDeclaration_DoesNotLeakOutsideStatement()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("probe", new AsyncDisposeProbe());
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("{ using (var r = probe) { } return r; }"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void UsingResourceDeclaration_SiblingStatementsCanReuseName()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("first", new object());
        engine.SetVariable("second", new object());

        var result = engine.Evaluate("{ using (object r = first); using (object r = second); return 42; }");

        Assert.That(result, Is.EqualTo(42));
    }

    private sealed class AsyncDisposeProbe : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return new ValueTask();
        }
    }
}
