namespace CsEval.Test.Parity;

public class LinqExpressionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/LinqExpression"])]
    public Task LinqExpression(string csxPath) => RunMatchesCSharp(csxPath);
}
