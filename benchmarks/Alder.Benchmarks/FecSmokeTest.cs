using Alder.Compiled;

namespace Alder.Benchmarks;

public static class FecSmokeTest
{
    public static void Run()
    {
        var globals = BenchmarkGlobalData.CreateDefault();

        // Standard mode FEC engine
        var standardFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, globals);

        // Extended mode FEC engine
        var extendedFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, globals, LanguageMode.Extended);
        extendedFec.RegisterFunction("inc", args => Convert.ToInt32(args[0]) + 1);

        // Standard FEC engine with InvocationTarget
        var invocationFec = BenchmarkBase.CreateEngine(CompilationMode.CompiledFec, globals);
        var target = new InvocationTarget();
        invocationFec.SetVariable("target", target);

        var expressions = new (string name, string expr, AlderEngine engine)[]
        {
            // === Comparable Execution Scenarios (Standard mode) ===
            ("Comparable/Arithmetic/Precedence", "1 + 2 * 3 - 4 / 2", standardFec),
            ("Comparable/Arithmetic/WithVariables", "(x + y) * z - x / 2", standardFec),
            ("Comparable/Boolean/Composite", "x > y && y < z || x == 10", standardFec),
            ("Comparable/Conditional/Ternary", "x > y ? x : y", standardFec),
            ("Comparable/Functions/MathMix", "Math.Abs(x - y) + Math.Max(y, z)", standardFec),
            ("Comparable/Arithmetic/ModuloEquality", "(x % y) == 1", standardFec),
            ("Comparable/Mix/NumericAndPredicate", "(x * y) + (z * 2) > 20", standardFec),
            ("Comparable/Flee/FastVariables_Addition", "x + y", standardFec),
            ("Comparable/Flee/SmallBranching",
                "((23 > 15 && 3 * 7 == 21) || (25 / 5 > 10 && 6 + 8 == 14)) ? ((2.1 == 2.1) ? ((4 * 3 - x) * (14.0 / 3.0) + y) : 0.0) : ((14.0 / 3.0) + y)",
                standardFec),
            ("Comparable/Flee/BigBooleanStress",
                CompetitorExpressionFactory.BuildBigBooleanStress(CompetitorExpressionDialect.CSharp),
                standardFec),
            ("Comparable/NCalc/SimpleEvaluation", "(3.14 == 3.14) || (text == \"Chers\")", standardFec),
            ("Comparable/NCalc/EvaluateVsLambda_Equality", "(1 + x == 5 + y) == (42 == value)", standardFec),
            ("Comparable/String/Concatenation", "\"hello\" + \" \" + text", standardFec),
            ("Comparable/String/CompareAndConcat", "text == \"alpha\" ? \"yes-\" + text : \"no\"", standardFec),

            // === Advanced Language Scenarios (Standard mode) ===
            ("Advanced/NestedMath", "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)", standardFec),
            ("Advanced/NestedConditional", "x > y ? (y > z ? y : z) : x", standardFec),
            ("Advanced/StringPredicate", "text.StartsWith(\"a\") && text.Length > 3", standardFec),
            ("Advanced/CollectionProperties", "numbers.Count > 500 && orders.Count == 5", standardFec),
            ("Advanced/ObjectGraphAccess", "orders[0].Quantity + orders[1].Quantity + orders.Count", standardFec),
            ("Advanced/StringChain", "text.Trim().ToUpper().Length", standardFec),
            ("Advanced/StringContains", "text.Contains(\"lph\") && text.StartsWith(\"a\")", standardFec),
            ("Advanced/NestedFunctionCalls", "Math.Max(Math.Abs(x - y), Math.Min(y, z))", standardFec),

            // === LINQ Scenarios (Standard mode) ===
            ("LINQ/WhereCount", "numbers.Where(x => x > 500).Count()", standardFec),
            ("LINQ/SelectSum", "numbers.Select(x => x * 2).Sum()", standardFec),
            ("LINQ/WhereSelectSum", "numbers.Where(x => x > 100).Select(x => x * x).Sum()", standardFec),
            ("LINQ/AnyPredicate", "numbers.Any(x => x > 999)", standardFec),
            ("LINQ/OrderByFirst", "numbers.OrderByDescending(x => x).First()", standardFec),

            // === Compilation Scenarios (Standard mode, parse only — but we evaluate too) ===
            ("Compilation/Flee/SmallArithmetic", "((4 * 3.4 * 18) - x) * (14.0 / 3.0) + y", standardFec),

            // === Micro Scenarios (Standard mode) ===
            ("Micro/StaticMethodCall", "Math.Abs(-5)", standardFec),
            ("Micro/ChainedStaticCalls", "Math.Abs(x - y) + Math.Max(y, z)", standardFec),
            ("Micro/InstanceMethodCall", "text.Contains(\"a\")", standardFec),
            ("Micro/PropertyAccess", "text.Length", standardFec),
            ("Micro/LinqWhereCount", "numbers.Where((n) => n > value).Count()", standardFec),
            ("Micro/ArithmeticOnly", "x + y * z", standardFec),
            ("Micro/TernaryOnly", "x > 0 ? x : -x", standardFec),
            ("Micro/PropertyChain", "text.Length > 3 && text.Contains(\"l\")", standardFec),
            ("Micro/CastExpression", "(double)x / y", standardFec),
            ("Micro/StringInterpolation", "$\"{text} is {x + y}\"", standardFec),

            // === Invocation Scenarios (Standard mode, needs InvocationTarget) ===
            ("Invocation/StaticMathMix", "Math.Abs(x - y) + Math.Max(y, z)", invocationFec),
            ("Invocation/InstanceStringContains", "text.Contains(\"a\")", invocationFec),
            ("Invocation/OverloadResolution_Int", "target.Overload(x)", invocationFec),
            ("Invocation/OverloadResolution_LongLiteral", "target.Overload(10000000000L)", invocationFec),
            ("Invocation/ImplicitNumericConversion", "target.ConsumeDouble(x)", invocationFec),
            ("Invocation/ParamsExpansion", "target.Sum(x, y, z, value)", invocationFec),
            ("Invocation/OptionalArgument", "target.WithOptional(x)", invocationFec),
            ("Invocation/ChainedInstanceCalls", "target.Normalize(text).Contains(\"AL\")", invocationFec),

            // === Binding Scenarios (Standard mode) ===
            ("Binding/MethodHeavy", "Math.Abs(x - y) + Math.Max(y, z)", standardFec),
            ("Binding/LoopMutation", "{ var sum = 0; for (var i = 0; i < 128; i++) { sum += i; } return sum; }", standardFec),

            // === ArithmeticPrecedenceParity (Standard mode) ===
            ("ArithmeticPrecedence", "1 + 2 * 3 - 4 / 2", standardFec),

            // === Extended Parity Scenarios (Extended mode) ===
            ("Extended/BareMath", "sin(x)", extendedFec),
            ("Extended/PipelineFunction", "x |> inc", extendedFec),
            ("Extended/ChainedComparison", "0 < x < y", extendedFec),
            ("Extended/PowerOperator", "x ** 2", extendedFec),
            ("Extended/InOperator", "x in numbers", extendedFec),
            ("Extended/NotInAlias", "x not in numbers", extendedFec),
            ("Extended/LikeOperator", "text like \"a%\"", extendedFec),
            ("Extended/NotLikeAlias", "text not like \"z%\"", extendedFec),
            ("Extended/RegexMatch", "text =~ \"^a\"", extendedFec),
            ("Extended/RegexNotMatch", "text !~ \"^z\"", extendedFec),
            ("Extended/Spaceship", "x <=> y", extendedFec),
            ("Extended/ImplicitIt", "numbers.Where(it > x).Count()", extendedFec),
            ("Extended/ComprehensionCount", "count([n * n for n in numbers if n % 2 == 0])", extendedFec),
            ("Extended/LetInExpression", "let tax = x * 0.1m in x + tax", extendedFec),
            ("Extended/IfExpression", "if (x > y) x else y", extendedFec),
            ("Extended/AggregateBuiltins", "sum(numbers.Where(it > x))", extendedFec),
            ("Extended/DateArithmeticSugar", "new DateTime(2026, 1, 1) + 30.days", extendedFec),

            // === Extended Standard equivalents (Standard mode) ===
            ("ExtendedStd/BareMath", "Math.Sin(x)", standardFec),
            ("ExtendedStd/PipelineFunction_inc", "inc(x)", extendedFec),
            ("ExtendedStd/ChainedComparison", "0 < x && x < y", standardFec),
            ("ExtendedStd/PowerOperator", "Math.Pow(x, 2)", standardFec),
            ("ExtendedStd/InOperator", "numbers.Contains(x)", standardFec),
            ("ExtendedStd/NotInAlias", "!numbers.Contains(x)", standardFec),
            ("ExtendedStd/LikeOperator", "text.StartsWith(\"a\")", standardFec),
            ("ExtendedStd/NotLikeAlias", "!text.StartsWith(\"z\")", standardFec),
            ("ExtendedStd/RegexMatch", "System.Text.RegularExpressions.Regex.IsMatch(text, \"^a\")", standardFec),
            ("ExtendedStd/RegexNotMatch", "!System.Text.RegularExpressions.Regex.IsMatch(text, \"^z\")", standardFec),
            ("ExtendedStd/Spaceship", "x.CompareTo(y)", standardFec),
            ("ExtendedStd/ImplicitIt", "numbers.Where(n => n > x).Count()", standardFec),
            ("ExtendedStd/ComprehensionCount", "numbers.Where(n => n % 2 == 0).Select(n => n * n).Count()", standardFec),
            ("ExtendedStd/LetInExpression", "{ var tax = x * 0.1m; return x + tax; }", standardFec),
            ("ExtendedStd/IfExpression", "x > y ? x : y", standardFec),
            ("ExtendedStd/AggregateBuiltins", "numbers.Where(n => n > x).Sum()", standardFec),
            ("ExtendedStd/DateArithmeticSugar", "new DateTime(2026, 1, 1) + TimeSpan.FromDays(30)", standardFec),
        };

        var passed = 0;
        var failed = 0;
        var failures = new List<(string name, string error)>();

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

        Console.WriteLine($"\nResults: {passed} passed, {failed} failed out of {expressions.Length}");

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
