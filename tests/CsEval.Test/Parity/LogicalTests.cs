namespace CsEval.Test.Parity;

public class LogicalTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Logical"])]
    public Task Logical(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Logical/NotAllowed"])]
    public Task Logical_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
