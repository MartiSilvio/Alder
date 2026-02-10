namespace CsEval.Test.Parity;

public class RuntimeIncrementDecrementTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/IncrementDecrement"])]
    public Task RuntimeIncrementDecrement(string csxPath) => RunMatchesCSharp(csxPath);
}
