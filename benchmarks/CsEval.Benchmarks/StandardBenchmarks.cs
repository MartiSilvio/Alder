using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CsEval.Benchmarks;

/// <summary>
/// Standard language benchmarks based on the Computer Language Benchmarks Game.
/// These provide comparable metrics across different language implementations.
/// https://benchmarksgame-team.pages.debian.net/benchmarksgame/
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class StandardBenchmarks
{
    private CsEvalEngine _engine = null!;

    // Pre-parsed expressions for fair comparison
    private CsEvalExpression _fibonacciIterative = null!;
    private CsEvalExpression _sumLoop = null!;
    private CsEvalExpression _primeCheck = null!;
    private CsEvalExpression _factorial = null!;
    private CsEvalExpression _collatzSequence = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();

        // Fibonacci iterative - standard benchmark
        _fibonacciIterative = _engine.Parse(@"
        {
            var a = 0;
            var b = 1;
            for (var i = 0; i < n; i++) {
                var temp = a;
                a = b;
                b = temp + b;
            }
            return a;
        }");

        // Sum loop - basic iteration benchmark
        _sumLoop = _engine.Parse(@"
        {
            var sum = 0;
            for (var i = 1; i <= n; i++) {
                sum += i;
            }
            return sum;
        }");

        // Prime check - conditional + loop benchmark
        _primeCheck = _engine.Parse(@"
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;
            var i = 3;
            while (i * i <= n) {
                if (n % i == 0) return false;
                i += 2;
            }
            return true;
        }");

        // Factorial iterative
        _factorial = _engine.Parse(@"
        {
            var result = 1;
            for (var i = 2; i <= n; i++) {
                result *= i;
            }
            return result;
        }");

        // Collatz sequence length - while loop + conditionals
        _collatzSequence = _engine.Parse(@"
        {
            var steps = 0;
            var current = n;
            while (current != 1) {
                if (current % 2 == 0) {
                    current = current / 2;
                } else {
                    current = current * 3 + 1;
                }
                steps++;
            }
            return steps;
        }");
    }

    #region Fibonacci - Classic recursive/iterative benchmark

    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(30)]
    public object Fibonacci_Iterative(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_fibonacciIterative)!;
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(30)]
    public long Fibonacci_NativeCSharp(int n)
    {
        long a = 0, b = 1;
        for (int i = 0; i < n; i++)
        {
            var temp = a;
            a = b;
            b = temp + b;
        }
        return a;
    }

    #endregion

    #region Sum Loop - Basic iteration benchmark

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public object SumLoop(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_sumLoop)!;
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public long SumLoop_NativeCSharp(int n)
    {
        long sum = 0;
        for (int i = 1; i <= n; i++)
            sum += i;
        return sum;
    }

    #endregion

    #region Prime Check - Conditional + loop benchmark

    [Benchmark]
    [Arguments(97)]      // Prime
    [Arguments(7919)]    // Larger prime
    [Arguments(104729)]  // Even larger prime
    public object PrimeCheck(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_primeCheck)!;
    }

    [Benchmark]
    [Arguments(97)]
    [Arguments(7919)]
    [Arguments(104729)]
    public bool PrimeCheck_NativeCSharp(int n)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if (n % 2 == 0) return false;
        int i = 3;
        while (i * i <= n)
        {
            if (n % i == 0) return false;
            i += 2;
        }
        return true;
    }

    #endregion

    #region Factorial - Multiplication loop benchmark

    [Benchmark]
    [Arguments(10)]
    [Arguments(15)]
    [Arguments(20)]
    public object Factorial(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_factorial)!;
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(15)]
    [Arguments(20)]
    public long Factorial_NativeCSharp(int n)
    {
        long result = 1;
        for (int i = 2; i <= n; i++)
            result *= i;
        return result;
    }

    #endregion

    #region Collatz Sequence - While loop + conditionals benchmark

    [Benchmark]
    [Arguments(27)]      // 111 steps
    [Arguments(97)]      // 118 steps
    [Arguments(871)]     // 178 steps
    public object CollatzSequence(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_collatzSequence)!;
    }

    [Benchmark]
    [Arguments(27)]
    [Arguments(97)]
    [Arguments(871)]
    public int CollatzSequence_NativeCSharp(int n)
    {
        int steps = 0;
        long current = n;
        while (current != 1)
        {
            if (current % 2 == 0)
                current = current / 2;
            else
                current = current * 3 + 1;
            steps++;
        }
        return steps;
    }

    #endregion
}
