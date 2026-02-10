namespace CsEval.Test.Parity;

public class VariableDeclarationTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Runtime/VariableDeclaration"])]
    public Task VariableDeclaration(string csxPath) => RunMatchesCSharp(csxPath);
}
