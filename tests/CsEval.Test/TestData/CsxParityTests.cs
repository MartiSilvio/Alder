namespace CsEval.Test.TestData;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CsxParityTests(CompilationMode mode)
{
    [TestCase("Algorithms/bubble-sort.csx", TestName = "BubbleSort")]
    [TestCase("Algorithms/fibonacci-memoized.csx", TestName = "FibonacciMemoized")]
    [TestCase("Algorithms/linked-list-operations.csx", TestName = "LinkedListOperations")]
    [TestCase("Algorithms/matrix-multiply.csx", TestName = "MatrixMultiply")]
    public async Task RunCsxParityTest(string path)
    {
        var expr = TestHelpers.LoadTestExpression(path);
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }
}
