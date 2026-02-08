namespace CsEval.Test.Parity;

public class LambdaTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Lambda"])]
    public Task Lambda(string csxPath) => RunMatchesCSharp(csxPath);
}
