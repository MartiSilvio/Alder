namespace CsEval.Test.Parity;

public class AssignmentTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Assignment"])]
    public Task Assignment(string csxPath) => RunMatchesCSharp(csxPath);
}
