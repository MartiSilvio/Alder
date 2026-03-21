using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LockStatementTests(CompilationMode mode)
{
    [Test]
    public void LockOnNull_ThrowsAlderExceptionWithConsistentMessage()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ object o = null; lock (o) { } }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CS0185));
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

    private sealed class AsyncDisposeProbe : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }
}
