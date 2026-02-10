namespace CsEval.Test.Parity;

public class RuntimeDeconstructionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/Deconstruction"])]
    public Task Deconstruction(string csxPath) => RunMatchesCSharp(csxPath);
}
