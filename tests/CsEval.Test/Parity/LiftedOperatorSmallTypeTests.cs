namespace CsEval.Test.Parity;

public class LiftedOperatorSmallTypeTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/LiftedOperatorSmallType"])]
    public Task LiftedOperatorSmallType(string csxPath) => RunMatchesCSharp(csxPath);
}
