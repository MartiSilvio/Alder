namespace CsEval.Test.Parity;

public class EcmaSpecExampleTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/EcmaSpecExample"])]
    public Task EcmaSpecExample(string csxPath) => RunMatchesCSharp(csxPath);
}
