namespace CsEval.Test.Parity;

public class ExceptionHandlingTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/ExceptionHandling"])]
    public Task ExceptionHandling(string csxPath) => RunMatchesCSharp(csxPath);
}
