using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class BoxingConformanceTests(CompilationMode mode)
{
    [Test]
    public void Unboxing_WrongType_Throws()
    {
        // §10.3.7: unboxing to wrong type should fail
        var ex = Assert.Throws<AlderException>(() => TestEngineFactory.Create(mode).Evaluate(@"
            object o = 42;
            return (long)o;
        "));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030));
    }
}
