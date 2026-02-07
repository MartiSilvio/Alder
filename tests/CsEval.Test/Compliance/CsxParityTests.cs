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
    [TestCase("Algorithms/fibonacci-memoized.csx", TestName = "FibonacciMemoized", Ignore = "Gap: typed array creation (new long[n])")]
    [TestCase("Algorithms/linked-list-operations.csx", TestName = "LinkedListOperations", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/matrix-multiply.csx", TestName = "MatrixMultiply", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/quicksort.csx", TestName = "Quicksort", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/graph-bfs.csx", TestName = "GraphBFS", Ignore = "Gap: typed array creation (new bool[n])")]
    [TestCase("Algorithms/sieve-of-eratosthenes.csx", TestName = "SieveOfEratosthenes", Ignore = "Gap: typed array creation (new bool[n])")]
    [TestCase("Algorithms/dijkstra.csx", TestName = "Dijkstra", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/conways-game-of-life.csx", TestName = "ConwaysGameOfLife", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/maze-solver.csx", TestName = "MazeSolver", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/sudoku-solver.csx", TestName = "SudokuSolver", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/astar-pathfinding.csx", TestName = "AStarPathfinding", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/n-queens.csx", TestName = "NQueens", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/gaussian-elimination.csx", TestName = "GaussianElimination", Ignore = "Gap: typed array creation (new double[n])")]
    [TestCase("Algorithms/mandelbrot-ascii.csx", TestName = "MandelbrotAscii", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/knapsack-01.csx", TestName = "Knapsack01", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/levenshtein-distance.csx", TestName = "LevenshteinDistance", Ignore = "Gap: typed array creation (new int[n]), string indexing")]
    [TestCase("Algorithms/topological-sort.csx", TestName = "TopologicalSort", Ignore = "Gap: typed array creation (new int[n])")]
    [TestCase("Algorithms/lcs-dp.csx", TestName = "LcsDp", Ignore = "Gap: typed array creation (new int[n]), string indexing")]
    [TestCase("Algorithms/numerical-integration.csx", TestName = "NumericalIntegration", Ignore = "Gap: typed lambda parameters, Func<> type annotations")]
    [TestCase("Algorithms/state-machine.csx", TestName = "StateMachine", Ignore = "Gap: typed array creation (new int[n]), string indexing")]
    [TestCase("Algorithms/run-length-encoding.csx", TestName = "RunLengthEncoding", Ignore = "Gap: string indexing")]
    [TestCase("Algorithms/expression-evaluator.csx", TestName = "ExpressionEvaluator", Ignore = "Gap: typed array creation (new int[n]), string indexing")]
    [TestCase("Algorithms/huffman-frequency.csx", TestName = "HuffmanFrequency", Ignore = "Gap: char array literals")]
    public async Task RunCsxParityTest(string path)
    {
        var expr = TestHelpers.LoadTestExpression(path);
        await TestHelpers.RunCSharpParityTestAsync(expr, Options);
    }
}
