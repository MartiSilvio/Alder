namespace CsEval.Test.Parity;

public class RuntimeAssignmentTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/Assignment"])]
    public Task RuntimeAssignment(string csxPath) => RunMatchesCSharp(csxPath);
}
