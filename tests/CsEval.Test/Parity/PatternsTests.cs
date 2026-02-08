namespace CsEval.Test.Parity;

public class PatternsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Patterns/ConstantPattern"])]
    public Task ConstantPattern(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Patterns/TypePattern"])]
    public Task TypePattern(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Patterns/RelationalPattern"])]
    public Task RelationalPattern(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Patterns/LogicalPattern"])]
    public Task LogicalPattern(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Patterns/PropertyPattern"])]
    public Task PropertyPattern(string csxPath) => RunMatchesCSharp(csxPath);
}
