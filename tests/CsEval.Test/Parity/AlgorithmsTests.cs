namespace CsEval.Test.Parity;

public class AlgorithmsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Algorithms"])]
    public Task Algorithms(string csxPath) => RunMatchesCSharp(csxPath);
}
