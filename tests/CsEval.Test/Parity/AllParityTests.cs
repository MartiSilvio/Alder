namespace CsEval.Test.Parity;

/// <summary>
/// Universal parity test runner that discovers all .csx files recursively.
/// Replaces individual test runners (NumericTests, StringTests, etc.).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AllParityTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTestsRecursive), ["TestData"])]
    public Task ParityTest(string csxPath) => RunMatchesCSharp(csxPath);
}
