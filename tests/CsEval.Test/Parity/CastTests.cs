namespace CsEval.Test.Parity;

public class CastTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Cast"])]
    public Task Cast(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Cast/NotAllowed"])]
    public Task Cast_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
