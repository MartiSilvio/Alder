namespace CsEval.Test.Parity;

public class DeconstructionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/Deconstruction"])]
    public Task Deconstruction(string csxPath) => RunMatchesCSharp(csxPath);
}
