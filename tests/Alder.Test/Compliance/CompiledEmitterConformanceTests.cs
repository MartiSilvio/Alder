using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CompiledEmitterConformanceTests(CompilationMode mode)
{
    [Test]
    public void CompiledParity_CheckedArithmetic()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked(int.MaxValue + 1)"));
    }
}
