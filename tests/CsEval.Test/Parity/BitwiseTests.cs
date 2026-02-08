namespace CsEval.Test.Parity;

public class BitwiseTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Bitwise"])]
    public Task Bitwise(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Bitwise/NotAllowed"])]
    public Task Bitwise_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
