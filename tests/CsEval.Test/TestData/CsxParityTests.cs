namespace CsEval.Test.TestData;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CsxParityTests(CompilationMode mode)
{
    [Test]
    public async Task BubbleSort()
    {
        var expr = TestHelpers.LoadTestExpression("Algorithms/bubble-sort.csx");
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    public async Task FibonacciMemoized()
    {
        var expr = TestHelpers.LoadTestExpression("Algorithms/fibonacci-memoized.csx");
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    public async Task LinkedListOperations()
    {
        var expr = TestHelpers.LoadTestExpression("Algorithms/linked-list-operations.csx");
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    public async Task MatrixMultiply()
    {
        var expr = TestHelpers.LoadTestExpression("Algorithms/matrix-multiply.csx");
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }
}
