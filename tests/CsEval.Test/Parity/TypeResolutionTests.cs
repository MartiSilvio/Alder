namespace CsEval.Test.Parity;

public class TypeResolutionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/TypeResolution"])]
    public Task TypeResolution(string csxPath) => RunMatchesCSharp(csxPath);
}
