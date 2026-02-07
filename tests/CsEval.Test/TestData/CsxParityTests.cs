namespace CsEval.Test.TestData;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CsxParityTests(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        MaxIterations = 1_000_000
    };

    [TestCase("Algorithms/bubble-sort.csx", TestName = "BubbleSort")]
    [TestCase("Algorithms/fibonacci-memoized.csx", TestName = "FibonacciMemoized")]
    [TestCase("Algorithms/linked-list-operations.csx", TestName = "LinkedListOperations")]
    [TestCase("Algorithms/matrix-multiply.csx", TestName = "MatrixMultiply")]
    [TestCase("Algorithms/binary-search.csx", TestName = "BinarySearch")]
    [TestCase("Algorithms/quicksort.csx", TestName = "Quicksort")]
    [TestCase("Algorithms/graph-bfs.csx", TestName = "GraphBFS")]
    [TestCase("Algorithms/sieve-of-eratosthenes.csx", TestName = "SieveOfEratosthenes")]
    [TestCase("Algorithms/dijkstra.csx", TestName = "Dijkstra")]
    [TestCase("Algorithms/conways-game-of-life.csx", TestName = "ConwaysGameOfLife")]
    [TestCase("Algorithms/maze-solver.csx", TestName = "MazeSolver")]
    [TestCase("Algorithms/sudoku-solver.csx", TestName = "SudokuSolver")]
    [TestCase("Algorithms/astar-pathfinding.csx", TestName = "AStarPathfinding")]
    [TestCase("Algorithms/state-machine.csx", TestName = "StateMachine", Ignore = "Gap: string indexing")]
    [TestCase("Algorithms/run-length-encoding.csx", TestName = "RunLengthEncoding", Ignore = "Gap: string indexing")]
    [TestCase("Algorithms/expression-evaluator.csx", TestName = "ExpressionEvaluator", Ignore = "Gap: string indexing")]
    [TestCase("Algorithms/huffman-frequency.csx", TestName = "HuffmanFrequency", Ignore = "Gap: char array literals")]
    public async Task RunCsxParityTest(string path)
    {
        var expr = TestHelpers.LoadTestExpression(path);
        await TestHelpers.RunCSharpParityTestAsync(expr, Options);
    }
}
