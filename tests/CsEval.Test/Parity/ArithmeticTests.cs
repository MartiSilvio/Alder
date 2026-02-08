namespace CsEval.Test.Parity;

public class ArithmeticTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Arithmetic"])]
    public Task Arithmetic(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Arithmetic/NotAllowed"])]
    public Task Arithmetic_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
