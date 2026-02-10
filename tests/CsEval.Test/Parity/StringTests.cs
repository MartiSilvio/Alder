namespace CsEval.Test.Parity;

public class StringTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Types/String"])]
    public Task String(string csxPath) => RunMatchesCSharp(csxPath);
}
