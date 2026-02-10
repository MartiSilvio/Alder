namespace CsEval.Test.Parity;

public class RuntimeNamedParameterTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/NamedParameter"])]
    public Task NamedParameter(string csxPath) => RunMatchesCSharp(csxPath);
}
