namespace CsEval.Test.Parity;

/// <summary>
/// Universal parity test runner that discovers all valid .csx files recursively.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AllParityTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTestsRecursive), ["TestData"])]
    public Task ParityTest(string csxPath) => RunMatchesCSharp(csxPath);
}

/// <summary>
/// Universal parity test runner for invalid .csx files that should throw exceptions.
/// Discovers all .csx files in TestData/Invalid recursively.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AllParityThrowsTests(CompilationMode mode) : CsxParityTestsBase(mode)
{
    [TestCaseSource(nameof(DiscoverTestsRecursive), ["TestData/Invalid"])]
    public Task ParityThrowsTest(string csxPath) => RunShouldThrow(csxPath);
}
