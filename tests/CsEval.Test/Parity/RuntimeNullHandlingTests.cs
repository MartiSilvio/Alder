namespace CsEval.Test.Parity;

public class RuntimeNullHandlingTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/NullHandling"])]
    public Task NullHandling(string csxPath) => RunMatchesCSharp(csxPath);
}
