namespace CsEval.Test.Parity;

public class TypeofTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Typeof"])]
    public Task Typeof(string csxPath) => RunMatchesCSharp(csxPath);
}
