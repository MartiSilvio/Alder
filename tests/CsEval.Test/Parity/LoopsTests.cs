namespace CsEval.Test.Parity;

public class LoopsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/ForLoop"])]
    public Task ForLoop(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/ForEachLoop"])]
    public Task ForEachLoop(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/WhileLoop"])]
    public Task WhileLoop(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/DoWhileLoop"])]
    public Task DoWhileLoop(string csxPath) => RunMatchesCSharp(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/ForLoop/NotAllowed"])]
    public Task ForLoop_NotAllowed(string csxPath) => RunShouldThrow(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/WhileLoop/NotAllowed"])]
    public Task WhileLoop_NotAllowed(string csxPath) => RunShouldThrow(csxPath);

    [TestCaseSource(nameof(DiscoverTests), ["TestData/Loops/DoWhileLoop/NotAllowed"])]
    public Task DoWhileLoop_NotAllowed(string csxPath) => RunShouldThrow(csxPath);
}
