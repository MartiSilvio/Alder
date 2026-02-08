namespace CsEval.Test.Parity;

public class ThrowExpressionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/ThrowExpression"])]
    public Task ThrowExpression(string csxPath) => RunMatchesCSharp(csxPath);
}
