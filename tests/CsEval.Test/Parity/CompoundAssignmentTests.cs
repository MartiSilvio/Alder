namespace CsEval.Test.Parity;

public class CompoundAssignmentTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/CompoundAssignment"])]
    public Task CompoundAssignment(string csxPath) => RunMatchesCSharp(csxPath);
}
