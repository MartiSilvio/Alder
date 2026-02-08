namespace CsEval.Test.Parity;

public class LiftedOperatorTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/LiftedOperator"])]
    public Task LiftedOperator(string csxPath) => RunMatchesCSharp(csxPath);
}
