using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

/// <summary>
/// Regression tests for bugs discovered through MCP/LLM usage patterns.
/// These cover composition edge cases where individually correct features
/// fail when combined in ways typical of LLM-generated expressions.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class McpRegressionTests(CompilationMode mode)
{
    private AlderEngine Engine() => TestEngineFactory.Create(mode);

    private object? Eval(string expr) => Engine().Evaluate(expr);

    #region new Func<T>(lambda) — delegate constructor from lambda

    [Test]
    public void NewFunc_FromLambda_SingleParam()
    {
        var result = Eval("new Func<int, int>(x => x * 2).Invoke(21)");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NewFunc_FromLambda_TwoParams()
    {
        var result = Eval("new Func<int, int, int>((a, b) => a + b).Invoke(10, 20)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void NewFunc_FromLambda_ReturningBool()
    {
        var result = Eval("new Func<int, bool>(x => x > 5).Invoke(10)");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NewFunc_FromLambda_NoParams()
    {
        var result = Eval("new Func<string>(() => \"hello\").Invoke()");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void NewAction_FromLambda()
    {
        var result = Eval("""
            var called = false;
            var a = new Action(() => { called = true; });
            a.Invoke();
            called
        """);
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Func<T> reassignment — lambda-to-delegate in assignment context

    [Test]
    public void FuncVariable_Reassignment_FromLambda()
    {
        var result = Eval("""
            Func<int, int> f = null;
            f = x => x * 3;
            f(7)
        """);
        Assert.That(result, Is.EqualTo(21));
    }

    [Test]
    public void FuncVariable_RecursiveLambda()
    {
        var result = Eval("""
            Func<int, int> factorial = null;
            factorial = n => n <= 1 ? 1 : n * factorial(n - 1);
            factorial(6)
        """);
        Assert.That(result, Is.EqualTo(720));
    }

    [Test]
    public void FuncVariable_Reassignment_MultipleTypes()
    {
        var result = Eval("""
            Func<string, int> len = null;
            len = s => s.Length;
            len("hello")
        """);
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region LINQ let clause with 3+ from clauses — transparent identifier nesting

    [Test]
    public void Query_ThreeFroms_WithLet()
    {
        var result = Eval("""
            var a = new[] { 1, 2 };
            var b = new[] { 10, 20 };
            var c = new[] { 100, 200 };
            var q = (from x in a
                     from y in b
                     from z in c
                     let sum = x + y + z
                     where sum == 111
                     select sum).ToList();
            q.Count
        """);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Query_ThreeFroms_WithLet_AccessAllRangeVars()
    {
        var result = Eval("""
            var a = new[] { 1 };
            var b = new[] { 2 };
            var c = new[] { 3 };
            var q = (from x in a
                     from y in b
                     from z in c
                     let sum = x + y + z
                     select $"{x},{y},{z}={sum}").First();
            q
        """);
        Assert.That(result, Is.EqualTo("1,2,3=6"));
    }

    [Test]
    public void Query_FourFroms_WithLet()
    {
        var result = Eval("""
            var p = new[] { 1, 2 };
            var q = (from a in p
                     from b in p
                     from c in p
                     from d in p
                     let sum = a + b + c + d
                     where sum == 5
                     select sum).Count();
            q
        """);
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Query_TwoFroms_WithLet_Baseline()
    {
        var result = Eval("""
            var nums = new[] { 1, 2, 3 };
            var q = (from x in nums
                     from y in nums
                     let product = x * y
                     where product > 4
                     select product).ToList();
            q.Count
        """);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Query_ThreeFroms_MultipleLets()
    {
        var result = Eval("""
            var a = new[] { 2 };
            var b = new[] { 3 };
            var c = new[] { 5 };
            var q = (from x in a
                     from y in b
                     let xy = x * y
                     from z in c
                     let xyz = xy * z
                     select xyz).First();
            q
        """);
        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region Generic method overload resolution — generic should beat object

    [Test]
    public void StringJoin_IntArray_UsesGenericOverload()
    {
        var result = Eval("""string.Join(", ", new int[] { 1, 2, 3 })""");
        Assert.That(result, Is.EqualTo("1, 2, 3"));
    }

    [Test]
    public void StringJoin_StringArray_UsesGenericOverload()
    {
        var result = Eval("""string.Join(", ", new string[] { "a", "b", "c" })""");
        Assert.That(result, Is.EqualTo("a, b, c"));
    }

    [Test]
    public void DotNet_StringJoin_Sanity()
    {
        // Verify that .NET itself resolves this correctly
        Assert.That(string.Join(", ", new int[] { 1, 2, 3 }), Is.EqualTo("1, 2, 3"));
    }

    #endregion

    #region Sum with double-returning lambda selector

    [Test]
    public void Sum_WithDoubleSelector_OnIntArray()
    {
        var result = Eval("""new int[] { 100, 110, 90 }.Sum(x => Math.Pow(x - 100, 2) / 100.0)""");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Sum_WithIntSelector_OnIntArray()
    {
        var result = Eval("""new int[] { 1, 2, 3 }.Sum(x => x * 2)""");
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Sum_WithDoubleSelector_OnAnonymousObjects()
    {
        var result = Eval("""
            var items = new[] { new { Count = 110 }, new { Count = 90 } };
            var expected = 100.0;
            items.Sum(b => Math.Pow(b.Count - expected, 2) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(2.0));
    }

    #endregion

    #region Anonymous object as return value

    [Test]
    public void AnonymousObject_SimpleReturn()
    {
        var result = Eval("""new { x = 1, y = "hello" }""");
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Huffman tree failures — Sum on char sequence, explicit cast to double

    [Test]
    public void Sum_OnCharSequence_IntSelector()
    {
        var engine = Engine();
        engine.SetVariable("codes", new Dictionary<string, string> { ["a"] = "0", ["b"] = "110" });
        var result = engine.Evaluate("""
            var input = "ab";
            input.Sum(c => codes[c.ToString()].Length)
            """);
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void ExplicitCast_IntToDouble()
    {
        var result = Eval("var x = 10; (double)x / 3");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Ratio_IntDividedByInt_AsDouble()
    {
        var result = Eval("var totalBits = 23; var asciiBits = 88; (1.0 - (double)totalBits / asciiBits) * 100");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(73.86).Within(0.1));
    }

    [Test]
    public void Huffman_SecondAttempt_RatioCalculation()
    {
        var result = Eval("""
            var input = "abracadabra";
            var freq = input.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
            var codes = new Dictionary<string, string> { ["a"] = "0", ["b"] = "110", ["r"] = "10", ["c"] = "1110", ["d"] = "1111" };
            var totalBits = input.Sum(c => codes[c.ToString()].Length);
            var asciiBits = input.Length * 8;
            var ratio = (1.0 - (double)totalBits / asciiBits) * 100;
            ratio
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.GreaterThan(70));
    }

    #endregion
}
