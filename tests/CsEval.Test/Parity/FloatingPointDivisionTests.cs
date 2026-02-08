namespace CsEval.Test.Parity;

public class FloatingPointDivisionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/FloatingPointDivision"])]
    public Task FloatingPointDivision(string csxPath) => RunMatchesCSharp(csxPath);
}
