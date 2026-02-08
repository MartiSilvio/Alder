namespace CsEval.Test.Parity;

public class IsAsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/IsAs"])]
    public Task IsAs(string csxPath) => RunMatchesCSharp(csxPath);
}
