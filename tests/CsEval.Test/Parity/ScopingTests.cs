namespace CsEval.Test.Parity;

public class ScopingTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Scoping"])]
    public Task Scoping(string csxPath) => RunMatchesCSharp(csxPath);
}
