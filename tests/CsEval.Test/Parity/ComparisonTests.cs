namespace CsEval.Test.Parity;

public class ComparisonTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Comparison"])]
    public Task Comparison(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Comparison/NotAllowed"])]
    public Task Comparison_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
