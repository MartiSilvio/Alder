namespace CsEval.Test.Parity;

public class ConversionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Conversion"])]
    public Task Conversion(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Conversion/NotAllowed"])]
    public Task Conversion_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
