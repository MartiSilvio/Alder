using System.Reflection;
using Alder.Diagnostics;

namespace Alder.Test.Core;

[TestFixture]
public class AlderExceptionInnerExceptionTests
{
    [Test]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("root failure");
        var ex = new AlderException(DiagnosticDescriptors.BindingFailed, inner, "binding failed");

        Assert.That(ex.InnerException, Is.SameAs(inner));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0002));
    }

    [Test]
    public void RuntimeWrapper_IndexerAccessFailed_PreservesInnerException()
    {
        var engine = new AlderEngine();
        engine.SetVariable("obj", new ThrowingIndexer());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("obj[0]"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0307));
        Assert.That(ex.InnerException, Is.Not.Null);
        Assert.That(ex.InnerException, Is.TypeOf<TargetInvocationException>());

        var tie = (TargetInvocationException)ex.InnerException!;
        Assert.That(tie.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(tie.InnerException!.Message, Is.EqualTo("indexer boom"));
    }

    private sealed class ThrowingIndexer
    {
        public int this[int index] => throw new InvalidOperationException("indexer boom");
    }
}
