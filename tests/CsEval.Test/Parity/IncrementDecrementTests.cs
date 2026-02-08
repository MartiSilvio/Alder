namespace CsEval.Test.Parity;

public class IncrementDecrementTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/IncrementDecrement"])]
    public Task IncrementDecrement(string csxPath) => RunMatchesCSharp(csxPath);
}
