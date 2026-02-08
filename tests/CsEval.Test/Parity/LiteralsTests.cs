namespace CsEval.Test.Parity;

public class LiteralsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/Literal"])]
    public Task Literal(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/BinaryLiteral"])]
    public Task BinaryLiteral(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/CharLiteral"])]
    public Task CharLiteral(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/DigitSeparator"])]
    public Task DigitSeparator(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/EscapeSequence"])]
    public Task EscapeSequence(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/ExponentLiteral"])]
    public Task ExponentLiteral(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/HexLiteral"])]
    public Task HexLiteral(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/HexEscape"])]
    public Task HexEscape(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/UnicodeEscape"])]
    public Task UnicodeEscape(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/String"])]
    public Task StringLiteral(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/VerbatimString"])]
    public Task VerbatimString(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Literals/Literal/NotAllowed"])]
    public Task Literal_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
