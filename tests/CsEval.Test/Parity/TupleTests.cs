namespace CsEval.Test.Parity;

public class TupleTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Types/Tuple"])]
    public Task Tuple(string csxPath) => RunMatchesCSharp(csxPath);
}
