namespace CsEval.Test.Parity;

public class OverloadResolutionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/OverloadResolution"])]
    public Task OverloadResolution(string csxPath) => RunMatchesCSharp(csxPath);
}
