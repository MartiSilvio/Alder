namespace CsEval.Test.Parity;

public class ConstructorTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Constructor"])]
    public Task Constructor(string csxPath) => RunMatchesCSharp(csxPath);
}
