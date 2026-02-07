namespace CsEval.Test.Compliance;

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
    [TestCase("Algorithms/binary-search.csx", TestName = "BinarySearch")]
    [TestCase("Algorithms/fibonacci-memoized.csx", TestName = "FibonacciMemoized")]
    [TestCase("Algorithms/linked-list-operations.csx", TestName = "LinkedListOperations")]
    [TestCase("Algorithms/matrix-multiply.csx", TestName = "MatrixMultiply")]
    [TestCase("Algorithms/quicksort.csx", TestName = "Quicksort")]
    [TestCase("Algorithms/graph-bfs.csx", TestName = "GraphBFS")]
    [TestCase("Algorithms/sieve-of-eratosthenes.csx", TestName = "SieveOfEratosthenes")]
    [TestCase("Algorithms/dijkstra.csx", TestName = "Dijkstra")]
    [TestCase("Algorithms/conways-game-of-life.csx", TestName = "ConwaysGameOfLife")]
    [TestCase("Algorithms/maze-solver.csx", TestName = "MazeSolver")]
    [TestCase("Algorithms/sudoku-solver.csx", TestName = "SudokuSolver")]
    [TestCase("Algorithms/astar-pathfinding.csx", TestName = "AStarPathfinding")]
    [TestCase("Algorithms/n-queens.csx", TestName = "NQueens")]
    [TestCase("Algorithms/gaussian-elimination.csx", TestName = "GaussianElimination", Ignore = "Gap: explicit cast (int)doubleValue via object unboxing")]
    [TestCase("Algorithms/mandelbrot-ascii.csx", TestName = "MandelbrotAscii", Ignore = "Gap: indexed increment (arr[i]++)")]
    [TestCase("Algorithms/knapsack-01.csx", TestName = "Knapsack01")]
    [TestCase("Algorithms/levenshtein-distance.csx", TestName = "LevenshteinDistance")]
    [TestCase("Algorithms/topological-sort.csx", TestName = "TopologicalSort", Ignore = "Gap: indexed increment (arr[i]++)")]
    [TestCase("Algorithms/lcs-dp.csx", TestName = "LcsDp")]
    [TestCase("Algorithms/numerical-integration.csx", TestName = "NumericalIntegration", Ignore = "Gap: typed lambda parameters, Func<> type annotations")]
    [TestCase("Algorithms/state-machine.csx", TestName = "StateMachine")]
    [TestCase("Algorithms/run-length-encoding.csx", TestName = "RunLengthEncoding")]
    [TestCase("Algorithms/expression-evaluator.csx", TestName = "ExpressionEvaluator")]
    [TestCase("Algorithms/huffman-frequency.csx", TestName = "HuffmanFrequency", Ignore = "Gap: char array literals")]
    public async Task RunCsxParityTest(string path)
    {
        var expr = TestHelpers.LoadTestExpression(path);
        await TestHelpers.RunCSharpParityTestAsync(expr, Options);
    }
}
