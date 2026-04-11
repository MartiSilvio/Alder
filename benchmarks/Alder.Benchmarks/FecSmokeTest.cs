using Alder.Compiled;

namespace Alder.Benchmarks;

public static class FecSmokeTest
{
    public static void Run()
    {
        var data = BenchmarkData.CreateStandard();

        var standardFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, data);
        var extendedFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, data, LanguageMode.Extended,
            o => o.Functions.Register("inc", args => Convert.ToInt32(args[0]) + 1));
        var invocationFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, data);
        invocationFec.SetVariable("target", new InvocationTarget());

        var expressions = new (string name, string expr, AlderEngine engine)[]
        {
            ("Arithmetic/Constant", "1 + 2 * 3 - 4 / 2", standardFec),
            ("Arithmetic/Variables", "(x + y) * z - x / 2", standardFec),
            ("Boolean/Composite", "x > y && y < z || x == 10", standardFec),
            ("Conditional/Ternary", "x > y ? x : y", standardFec),
            ("Functions/MathMix", "Math.Abs(x - y) + Math.Max(y, z)", standardFec),
            ("String/Concatenation", "\"hello\" + \" \" + text", standardFec),
            ("Stress/SmallBranching",
                "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)",
                standardFec),
            ("Stress/BigBoolean", CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp), standardFec),

            ("Advanced/NestedMath", "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)", standardFec),
            ("Advanced/StringPredicate", "text.StartsWith(\"a\") && text.Length > 3", standardFec),
            ("Advanced/StringChain", "text.Trim().ToUpper().Length", standardFec),
            ("Advanced/StringInterpolation", "$\"{text} is {x + y}\"", standardFec),
            ("Advanced/CollectionProperties", "numbers.Count > 500 && orders.Count == 5", standardFec),
            ("Advanced/ObjectGraphAccess", "orders[0].Quantity + orders[1].Quantity + orders.Count", standardFec),

            ("LINQ/WhereCount", "numbers.Where(x => x > 500).Count()", standardFec),
            ("LINQ/SelectSum", "numbers.Select(x => x * 2).Sum()", standardFec),
            ("LINQ/WhereSelectSum", "numbers.Where(x => x > 100).Select(x => x * x).Sum()", standardFec),
            ("LINQ/AnyPredicate", "numbers.Any(x => x > 999)", standardFec),
            ("LINQ/OrderByFirst", "numbers.OrderByDescending(x => x).First()", standardFec),

            ("Invocation/OverloadResolution_Int", "target.Overload(x)", invocationFec),
            ("Invocation/OverloadResolution_Long", "target.Overload(10000000000L)", invocationFec),
            ("Invocation/ImplicitConversion", "target.ConsumeDouble(x)", invocationFec),
            ("Invocation/ParamsExpansion", "target.Sum(x, y, z, value)", invocationFec),
            ("Invocation/OptionalArgument", "target.WithOptional(x)", invocationFec),
            ("Invocation/ChainedInstance", "target.Normalize(text).Contains(\"AL\")", invocationFec),

            ("Extended/BareMath", "sin(x)", extendedFec),
            ("Extended/Pipeline", "x |> inc", extendedFec),
            ("Extended/ChainedComparison", "0 < x < y", extendedFec),
            ("Extended/PowerOperator", "x ** 2", extendedFec),
            ("Extended/InOperator", "x in numbers", extendedFec),
            ("Extended/NotIn", "x not in numbers", extendedFec),
            ("Extended/Like", "text like \"a%\"", extendedFec),
            ("Extended/Regex", "text =~ \"^a\"", extendedFec),
            ("Extended/Spaceship", "x <=> y", extendedFec),
            ("Extended/Comprehension", "count([n * n for n in numbers if n % 2 == 0])", extendedFec),
            ("Extended/LetIn", "let tax = x * 0.1m in x + tax", extendedFec),
            ("Extended/IfExpression", "if (x > y) x else y", extendedFec),
            ("Extended/AggregateBuiltin", "sum(numbers.Where(n => n > x))", extendedFec),
            ("Extended/DateArithmetic", "new DateTime(2026, 1, 1) + 30.days", extendedFec),
        };

        var passed = 0;
        var failed = 0;
        var failures = new List<(string name, string error)>();

        var typedDelegateTests = new (string name, Action test)[]
        {
            ("TypedDelegate/Func1Param", () =>
            {
                var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
                var fn = engine.Compile<Func<int, int>>("x * 2", "x");
                var result = fn(5);
                if (result != 10) throw new Exception($"Expected 10, got {result}");
            }),
            ("TypedDelegate/Func2Params", () =>
            {
                var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
                var fn = engine.Compile<Func<int, int, int>>("a + b", "a", "b");
                var result = fn(3, 7);
                if (result != 10) throw new Exception($"Expected 10, got {result}");
            }),
            ("TypedDelegate/FuncStringReturn", () =>
            {
                var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
                var fn = engine.Compile<Func<string, int, string>>(
                    """$"Hello {name}, age {age}" """, "name", "age");
                var result = fn("Alice", 30);
                if (result != "Hello Alice, age 30") throw new Exception($"Unexpected: {result}");
            }),
            ("TypedDelegate/FuncBoolReturn", () =>
            {
                var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
                var fn = engine.Compile<Func<int, int, bool>>("a > b", "a", "b");
                if (!fn(5, 3)) throw new Exception("Expected true");
                if (fn(3, 5)) throw new Exception("Expected false");
            }),
            ("TypedDelegate/MultiStatement", () =>
            {
                var engine = new AlderEngine(new AlderOptions().UseCompiler(new FastExpressionCompilerAdapter()));
                var fn = engine.Compile<Func<int, string>>("""
                    if (n > 0) return "positive";
                    else return "non-positive";
                    """, "n");
                if (fn(5) != "positive") throw new Exception("Expected positive");
                if (fn(-1) != "non-positive") throw new Exception("Expected non-positive");
            }),
        };

        foreach (var (name, test) in typedDelegateTests)
        {
            try
            {
                test();
                Console.WriteLine($"  PASS: {name}");
                passed++;
            }
            catch (Exception ex)
            {
                var msg = $"{ex.GetType().Name}: {ex.Message}";
                Console.WriteLine($"  FAIL: {name} -- {msg}");
                failures.Add((name, msg));
                failed++;
            }
        }

        foreach (var (name, expr, engine) in expressions)
        {
            try
            {
                engine.Evaluate(expr);
                Console.WriteLine($"  PASS: {name}");
                passed++;
            }
            catch (Exception ex)
            {
                var msg = $"{ex.GetType().Name}: {ex.Message}";
                Console.WriteLine($"  FAIL: {name} -- {msg}");
                failures.Add((name, msg));
                failed++;
            }
        }

        var total = typedDelegateTests.Length + expressions.Length;
        Console.WriteLine($"\nResults: {passed} passed, {failed} failed out of {total}");

        if (failures.Count > 0)
        {
            Console.WriteLine("\n=== FAILURES ===");
            foreach (var (name, error) in failures)
                Console.WriteLine($"  {name}: {error}");
        }

        standardFec.Dispose();
        extendedFec.Dispose();
        invocationFec.Dispose();
    }
}
