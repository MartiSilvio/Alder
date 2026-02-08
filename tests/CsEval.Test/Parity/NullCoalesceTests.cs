namespace CsEval.Test.Parity;

public class NullCoalesceTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/NullCoalesce"])]
    public Task NullCoalesce(string csxPath) => RunMatchesCSharp(csxPath);
}
