using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class CompiledEmitterConformanceTests(CompilationMode mode)
{
    [Test]
    public void CompiledParity_CheckedArithmetic()
    {
        Assert.Throws<OverflowException>(() => TestEngineFactory.Create(mode).Evaluate("checked(int.MaxValue + 1)"));
    }
}
