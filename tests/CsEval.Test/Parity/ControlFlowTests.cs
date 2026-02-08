namespace CsEval.Test.Parity;

public class ControlFlowTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/ControlFlow"])]
    public Task ControlFlow(string csxPath) => RunMatchesCSharp(csxPath);
}
