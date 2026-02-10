namespace CsEval.Test.Parity;

public class RuntimeSwitchStatementTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/SwitchStatement"])]
    public Task RuntimeSwitchStatement(string csxPath) => RunMatchesCSharp(csxPath);
}
