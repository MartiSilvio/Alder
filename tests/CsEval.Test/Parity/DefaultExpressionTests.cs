namespace CsEval.Test.Parity;

public class DefaultExpressionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/DefaultExpression"])]
    public Task DefaultExpression(string csxPath) => RunMatchesCSharp(csxPath);
}
