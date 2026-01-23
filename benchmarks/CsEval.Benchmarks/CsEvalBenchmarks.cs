using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CsEval.Benchmarks;

/// <summary>
/// CsEval-specific benchmarks measuring parsing, evaluation,
/// and the performance benefit of pre-parsing expressions.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CsEvalBenchmarks
{
    private CsEvalEngine _engine = null!;

    // Simple expressions
    private const string SimpleArithmetic = "1 + 2 * 3 - 4 / 2";
    private const string ComplexArithmetic = "((1 + 2) * (3 + 4) - (5 * 6)) / 2 + 10 % 3";
    private const string TernaryExpression = "x > 0 ? x * 2 : x * -1";
    private const string NullCoalesce = "a ?? b ?? c ?? 0";

    // Pre-parsed versions
    private CsEvalExpression _simpleArithmeticParsed = null!;
    private CsEvalExpression _complexArithmeticParsed = null!;
    private CsEvalExpression _ternaryParsed = null!;
    private CsEvalExpression _nullCoalesceParsed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();
        _engine.SetVariable("x", 10L);
        _engine.SetVariable("a", null);
        _engine.SetVariable("b", null);
        _engine.SetVariable("c", 42L);

        _simpleArithmeticParsed = _engine.Parse(SimpleArithmetic);
        _complexArithmeticParsed = _engine.Parse(ComplexArithmetic);
        _ternaryParsed = _engine.Parse(TernaryExpression);
        _nullCoalesceParsed = _engine.Parse(NullCoalesce);
    }

    #region Parse Time

    [Benchmark]
    public CsEvalExpression Parse_SimpleArithmetic() => _engine.Parse(SimpleArithmetic);

    [Benchmark]
    public CsEvalExpression Parse_ComplexArithmetic() => _engine.Parse(ComplexArithmetic);

    [Benchmark]
    public CsEvalExpression Parse_Ternary() => _engine.Parse(TernaryExpression);

    [Benchmark]
    public CsEvalExpression Parse_NullCoalesce() => _engine.Parse(NullCoalesce);

    #endregion

    #region Evaluate Time (pre-parsed)

    [Benchmark]
    public object Evaluate_SimpleArithmetic_PreParsed() => _engine.Evaluate(_simpleArithmeticParsed)!;

    [Benchmark]
    public object Evaluate_ComplexArithmetic_PreParsed() => _engine.Evaluate(_complexArithmeticParsed)!;

    [Benchmark]
    public object Evaluate_Ternary_PreParsed() => _engine.Evaluate(_ternaryParsed)!;

    [Benchmark]
    public object Evaluate_NullCoalesce_PreParsed() => _engine.Evaluate(_nullCoalesceParsed)!;

    #endregion

    #region Parse + Evaluate (full pipeline)

    [Benchmark]
    public object ParseAndEvaluate_SimpleArithmetic() => _engine.Evaluate(SimpleArithmetic)!;

    [Benchmark]
    public object ParseAndEvaluate_ComplexArithmetic() => _engine.Evaluate(ComplexArithmetic)!;

    [Benchmark]
    public object ParseAndEvaluate_Ternary() => _engine.Evaluate(TernaryExpression)!;

    [Benchmark]
    public object ParseAndEvaluate_NullCoalesce() => _engine.Evaluate(NullCoalesce)!;

    #endregion
}

/// <summary>
/// Benchmarks for LINQ operations in CsEval.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class LinqBenchmarks
{
    private CsEvalEngine _engine = null!;
    private List<object> _smallList = null!;   // 10 items
    private List<object> _mediumList = null!;  // 100 items
    private List<object> _largeList = null!;   // 1000 items

    private CsEvalExpression _whereExpr = null!;
    private CsEvalExpression _selectExpr = null!;
    private CsEvalExpression _aggregateExpr = null!;
    private CsEvalExpression _chainedExpr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();

        // Create test data as dictionaries (common pattern in CsEval)
        _smallList = Enumerable.Range(1, 10)
            .Select(i => (object)new Dictionary<string, object?> { ["Value"] = (long)i, ["IsEven"] = i % 2 == 0 })
            .ToList();

        _mediumList = Enumerable.Range(1, 100)
            .Select(i => (object)new Dictionary<string, object?> { ["Value"] = (long)i, ["IsEven"] = i % 2 == 0 })
            .ToList();

        _largeList = Enumerable.Range(1, 1000)
            .Select(i => (object)new Dictionary<string, object?> { ["Value"] = (long)i, ["IsEven"] = i % 2 == 0 })
            .ToList();

        _whereExpr = _engine.Parse("items.Where(x => x.IsEven)");
        _selectExpr = _engine.Parse("items.Select(x => x.Value * 2)");
        _aggregateExpr = _engine.Parse("items.Sum(x => x.Value)");
        _chainedExpr = _engine.Parse("items.Where(x => x.IsEven).Select(x => x.Value).Sum()");
    }

    #region Where

    [Benchmark]
    public object Where_Small()
    {
        _engine.SetVariable("items", _smallList);
        return _engine.Evaluate(_whereExpr)!;
    }

    [Benchmark]
    public object Where_Medium()
    {
        _engine.SetVariable("items", _mediumList);
        return _engine.Evaluate(_whereExpr)!;
    }

    [Benchmark]
    public object Where_Large()
    {
        _engine.SetVariable("items", _largeList);
        return _engine.Evaluate(_whereExpr)!;
    }

    #endregion

    #region Select

    [Benchmark]
    public object Select_Small()
    {
        _engine.SetVariable("items", _smallList);
        return _engine.Evaluate(_selectExpr)!;
    }

    [Benchmark]
    public object Select_Medium()
    {
        _engine.SetVariable("items", _mediumList);
        return _engine.Evaluate(_selectExpr)!;
    }

    [Benchmark]
    public object Select_Large()
    {
        _engine.SetVariable("items", _largeList);
        return _engine.Evaluate(_selectExpr)!;
    }

    #endregion

    #region Aggregate (Sum)

    [Benchmark]
    public object Sum_Small()
    {
        _engine.SetVariable("items", _smallList);
        return _engine.Evaluate(_aggregateExpr)!;
    }

    [Benchmark]
    public object Sum_Medium()
    {
        _engine.SetVariable("items", _mediumList);
        return _engine.Evaluate(_aggregateExpr)!;
    }

    [Benchmark]
    public object Sum_Large()
    {
        _engine.SetVariable("items", _largeList);
        return _engine.Evaluate(_aggregateExpr)!;
    }

    #endregion

    #region Chained Operations

    [Benchmark]
    public object Chained_Small()
    {
        _engine.SetVariable("items", _smallList);
        return _engine.Evaluate(_chainedExpr)!;
    }

    [Benchmark]
    public object Chained_Medium()
    {
        _engine.SetVariable("items", _mediumList);
        return _engine.Evaluate(_chainedExpr)!;
    }

    [Benchmark]
    public object Chained_Large()
    {
        _engine.SetVariable("items", _largeList);
        return _engine.Evaluate(_chainedExpr)!;
    }

    #endregion

    #region Native C# Comparison

    [Benchmark]
    public List<Dictionary<string, object?>> Where_Large_NativeCSharp()
    {
        return _largeList
            .Cast<Dictionary<string, object?>>()
            .Where(x => (bool)x["IsEven"]!)
            .ToList();
    }

    [Benchmark]
    public List<long> Select_Large_NativeCSharp()
    {
        return _largeList
            .Cast<Dictionary<string, object?>>()
            .Select(x => (long)x["Value"]! * 2)
            .ToList();
    }

    [Benchmark]
    public long Sum_Large_NativeCSharp()
    {
        return _largeList
            .Cast<Dictionary<string, object?>>()
            .Sum(x => (long)x["Value"]!);
    }

    [Benchmark]
    public long Chained_Large_NativeCSharp()
    {
        return _largeList
            .Cast<Dictionary<string, object?>>()
            .Where(x => (bool)x["IsEven"]!)
            .Select(x => (long)x["Value"]!)
            .Sum();
    }

    #endregion
}

/// <summary>
/// Benchmarks for property access on typed objects.
/// Tests the benefit of compiled property getters with warm caches.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class PropertyAccessBenchmarks
{
    private CsEvalEngine _engine = null!;

    // Typed test objects (not dictionaries - these use reflection/compiled getters)
    private List<TestPerson> _personList = null!;
    private TestPerson _singlePerson = null!;

    private CsEvalExpression _propertyAccess = null!;
    private CsEvalExpression _multiPropertyAccess = null!;
    private CsEvalExpression _objectMerge = null!;
    private CsEvalExpression _spreadOperator = null!;
    private CsEvalExpression _linqOnTypedObjects = null!;

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string City { get; set; } = "";
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();

        _singlePerson = new TestPerson
        {
            Name = "Alice",
            Age = 30,
            City = "NYC",
            IsActive = true,
            Salary = 75000m
        };

        _personList = Enumerable.Range(1, 100)
            .Select(i => new TestPerson
            {
                Name = $"Person{i}",
                Age = 20 + (i % 50),
                City = i % 2 == 0 ? "NYC" : "LA",
                IsActive = i % 3 != 0,
                Salary = 50000m + (i * 1000)
            })
            .ToList();

        _engine.SetVariable("person", _singlePerson);
        _engine.SetVariable("people", _personList);

        // Single property access (repeated in benchmark iterations = warm cache)
        _propertyAccess = _engine.Parse("person.Name");

        // Multiple property access on same object
        _multiPropertyAccess = _engine.Parse(@"
        {
            var n = person.Name;
            var a = person.Age;
            var c = person.City;
            var s = person.Salary;
            return n;
        }");

        // Object merge with typed object
        _objectMerge = _engine.Parse("person + new { FullInfo = person.Name + \" (\" + person.Age + \")\" }");

        // Spread typed object
        _spreadOperator = _engine.Parse("new { ...person, Extra = \"test\" }");

        // LINQ on typed objects (accesses properties in lambda)
        _linqOnTypedObjects = _engine.Parse("people.Where(p => p.IsActive).Select(p => p.Name)");
    }

    [Benchmark]
    public object PropertyAccess_Single()
    {
        return _engine.Evaluate(_propertyAccess)!;
    }

    [Benchmark]
    public object PropertyAccess_Multiple()
    {
        return _engine.Evaluate(_multiPropertyAccess)!;
    }

    [Benchmark]
    public object ObjectMerge_TypedObject()
    {
        return _engine.Evaluate(_objectMerge)!;
    }

    [Benchmark]
    public object Spread_TypedObject()
    {
        return _engine.Evaluate(_spreadOperator)!;
    }

    [Benchmark]
    public object Linq_OnTypedObjects()
    {
        return _engine.Evaluate(_linqOnTypedObjects)!;
    }

    // Repeated access benchmark - same expression evaluated many times
    // This is where compiled getters shine
    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    public object RepeatedPropertyAccess(int iterations)
    {
        object? result = null;
        for (int i = 0; i < iterations; i++)
        {
            result = _engine.Evaluate(_propertyAccess);
        }
        return result!;
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    public object RepeatedObjectMerge(int iterations)
    {
        object? result = null;
        for (int i = 0; i < iterations; i++)
        {
            result = _engine.Evaluate(_objectMerge);
        }
        return result!;
    }
}

/// <summary>
/// Benchmarks for block expressions with control flow.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BlockExpressionBenchmarks
{
    private CsEvalEngine _engine = null!;

    private CsEvalExpression _simpleBlock = null!;
    private CsEvalExpression _ifElseBlock = null!;
    private CsEvalExpression _forLoopBlock = null!;
    private CsEvalExpression _whileLoopBlock = null!;
    private CsEvalExpression _nestedLoopBlock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new CsEvalEngine();

        _simpleBlock = _engine.Parse(@"
        {
            var x = 10;
            var y = 20;
            return x + y;
        }");

        _ifElseBlock = _engine.Parse(@"
        {
            var result = 0;
            if (n > 50) {
                result = n * 2;
            } else if (n > 25) {
                result = n + 50;
            } else {
                result = n;
            }
            return result;
        }");

        _forLoopBlock = _engine.Parse(@"
        {
            var sum = 0;
            for (var i = 0; i < n; i++) {
                sum += i;
            }
            return sum;
        }");

        _whileLoopBlock = _engine.Parse(@"
        {
            var sum = 0;
            var i = 0;
            while (i < n) {
                sum += i;
                i++;
            }
            return sum;
        }");

        _nestedLoopBlock = _engine.Parse(@"
        {
            var sum = 0;
            for (var i = 0; i < n; i++) {
                for (var j = 0; j < n; j++) {
                    sum += 1;
                }
            }
            return sum;
        }");
    }

    [Benchmark]
    public object SimpleBlock() => _engine.Evaluate(_simpleBlock)!;

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public object IfElseBlock(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_ifElseBlock)!;
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public object ForLoopBlock(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_forLoopBlock)!;
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public object WhileLoopBlock(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_whileLoopBlock)!;
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(30)]
    public object NestedLoopBlock(int n)
    {
        _engine.SetVariable("n", (long)n);
        return _engine.Evaluate(_nestedLoopBlock)!;
    }
}
