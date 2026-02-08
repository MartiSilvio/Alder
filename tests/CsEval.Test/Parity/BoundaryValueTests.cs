namespace CsEval.Test.Parity;

public class BoundaryValueTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/BoundaryValue"])]
    public Task BoundaryValue(string csxPath) => RunMatchesCSharp(csxPath);
}
