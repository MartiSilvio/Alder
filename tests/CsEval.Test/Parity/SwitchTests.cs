namespace CsEval.Test.Parity;

public class SwitchTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Switch/SwitchExpression"])]
    public Task SwitchExpression(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Switch/SwitchStatement"])]
    public Task SwitchStatement(string csxPath) => RunMatchesCSharp(csxPath);
}
