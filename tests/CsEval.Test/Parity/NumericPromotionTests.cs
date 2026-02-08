namespace CsEval.Test.Parity;

public class NumericPromotionTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/NumericPromotion"])]
    public Task NumericPromotion(string csxPath) => RunMatchesCSharp(csxPath);
}
