namespace CsEval.Test.Parity;

public class NumericTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Types/Numeric"])]
    public Task Numeric(string csxPath) => RunMatchesCSharp(csxPath);
}
