namespace CsEval.Test.Parity;

public class RuntimeScopingTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/Scoping"])]
    public Task RuntimeScoping(string csxPath) => RunMatchesCSharp(csxPath);
}
