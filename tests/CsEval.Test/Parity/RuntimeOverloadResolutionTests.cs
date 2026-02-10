namespace CsEval.Test.Parity;

public class RuntimeOverloadResolutionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/OverloadResolution"])]
    public Task RuntimeOverloadResolution(string csxPath) => RunMatchesCSharp(csxPath);
}
