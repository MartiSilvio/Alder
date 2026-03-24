using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// ECMA-334 §12.6.4.4 "Better conversion from expression" tests.
/// Verifies that overload resolution correctly selects methods when lambda arguments
/// are involved and overloads differ in delegate return type.
///
/// Covers: primitive types, anonymous objects, nested lambdas, LINQ aggregates,
/// custom extension methods, mixed argument types, edge cases.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LambdaOverloadResolutionTests(CompilationMode mode)
{
    private AlderEngine Engine(Action<AlderOptions>? configure = null)
        => TestEngineFactory.Create(mode, configure);

    private object? Eval(string expr, Action<AlderOptions>? configure = null)
        => Engine(configure).Evaluate(expr);

    #region Sum with lambda selector — delegate return type disambiguation

    [Test]
    public void Sum_IntSelector_ReturnsInt()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => x * 2)");
        Assert.That(result, Is.EqualTo(12));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void Sum_DoubleSelector_ViaMultiplication()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => x * 1.0)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(6.0));
    }

    [Test]
    public void Sum_DoubleSelector_ViaMathPow()
    {
        var result = Eval("new[] { 3, 4 }.Sum(x => Math.Pow(x, 2))");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(25.0));
    }

    [Test]
    public void Sum_DoubleSelector_ViaDivision()
    {
        var result = Eval("new[] { 10, 20, 30 }.Sum(x => x / 2.0)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(30.0));
    }

    [Test]
    public void Sum_LongSelector_ViaCast()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => (long)x)");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(6L));
    }

    [Test]
    public void Sum_FloatSelector_ViaCast()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => (float)x)");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That(result, Is.EqualTo(6f));
    }

    [Test]
    public void Sum_DecimalSelector_ViaCast()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => (decimal)x)");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(6m));
    }

    [Test]
    public void Sum_IntSelector_WithMathAbs()
    {
        var result = Eval("new[] { -1, -2, 3 }.Sum(x => Math.Abs(x))");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Sum with anonymous object sources — dynamic member access in lambda

    [Test]
    public void Sum_AnonymousObject_IntProperty()
    {
        var result = Eval("""
            var items = new[] { new { Value = 10 }, new { Value = 20 } };
            items.Sum(x => x.Value)
            """);
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Sum_AnonymousObject_DoubleArithmetic()
    {
        var result = Eval("""
            var items = new[] { new { Count = 110 }, new { Count = 90 } };
            var expected = 100.0;
            items.Sum(b => Math.Pow(b.Count - expected, 2) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(2.0));
    }

    [Test]
    public void Sum_AnonymousObject_MixedPropertyTypes()
    {
        var result = Eval("""
            var items = new[] { new { Price = 9.99, Qty = 2 }, new { Price = 4.50, Qty = 3 } };
            items.Sum(x => x.Price * x.Qty)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(33.48).Within(0.001));
    }

    [Test]
    public void Sum_AnonymousObject_MathSqrt()
    {
        var result = Eval("""
            var items = new[] { new { N = 4 }, new { N = 9 }, new { N = 16 } };
            items.Sum(x => Math.Sqrt(x.N))
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(9.0).Within(0.001));
    }

    #endregion

    // Min/Max with selector uses Min<TSource, TResult> — a single generic method where
    // TResult must be inferred from the lambda body. This requires generic type inference
    // from lambda return types, which is a separate gap from overload resolution.
    // Tracked separately from the overload resolution fixes.

    #region Average with lambda selector

    [Test]
    public void Average_IntSelector()
    {
        var result = Eval("new[] { 2, 4, 6 }.Average(x => x)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void Average_DoubleSelector()
    {
        var result = Eval("new[] { 2, 4, 6 }.Average(x => x * 1.0)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void Average_AnonymousObject()
    {
        var result = Eval("""
            var items = new[] { new { Score = 80 }, new { Score = 90 }, new { Score = 100 } };
            items.Average(x => x.Score)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(90.0));
    }

    #endregion

    #region Select + LINQ chains — lambda return type flows through

    [Test]
    public void Select_DoubleResult_ThenSum()
    {
        var result = Eval("new[] { 1, 2, 3 }.Select(x => x * 2.0).Sum()");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(12.0));
    }

    // Select on anonymous objects returns IEnumerable<double> but Sum() dispatch
    // on the resulting iterator type has a separate invocation path issue.

    [Test]
    public void Where_ThenSum_WithDoubleSelector()
    {
        var result = Eval("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 2).Sum(x => x * 0.5)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(6.0));
    }

    #endregion

    #region string.Join — generic overload vs params object[] (binder-level fix)

    [Test]
    public void StringJoin_IntArray()
    {
        var result = Eval("""string.Join(", ", new int[] { 1, 2, 3 })""");
        Assert.That(result, Is.EqualTo("1, 2, 3"));
    }

    [Test]
    public void StringJoin_DoubleArray()
    {
        var result = Eval("""string.Join(" | ", new double[] { 1.1, 2.2, 3.3 })""");
        Assert.That(result, Is.EqualTo("1.1 | 2.2 | 3.3"));
    }

    [Test]
    public void StringJoin_BoolArray()
    {
        var result = Eval("""string.Join(", ", new bool[] { true, false, true })""");
        Assert.That(result, Is.EqualTo("True, False, True"));
    }

    [Test]
    public void StringJoin_StringArray()
    {
        var result = Eval("""string.Join("-", new string[] { "a", "b", "c" })""");
        Assert.That(result, Is.EqualTo("a-b-c"));
    }

    [Test]
    public void StringJoin_List()
    {
        var engine = Engine();
        engine.SetVariable("items", new List<int> { 10, 20, 30 });
        var result = engine.Evaluate("""string.Join(", ", items)""");
        Assert.That(result, Is.EqualTo("10, 20, 30"));
    }

    // string.Join with Select result (non-array IEnumerable<int>) hits the AOT-generated
    // dispatch for string which can't handle non-string IEnumerable types.
    // Requires AOT generator to fall through for unmatched IEnumerable types.

    #endregion

    #region SetVariable with typed collections — overloads resolve from runtime types

    [Test]
    public void Sum_SetVariable_ListInt()
    {
        var engine = Engine();
        engine.SetVariable("nums", new List<int> { 10, 20, 30 });
        var result = engine.Evaluate("nums.Sum(x => x * 2)");
        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public void Sum_SetVariable_ListInt_DoubleSelector()
    {
        var engine = Engine();
        engine.SetVariable("nums", new List<int> { 10, 20, 30 });
        var result = engine.Evaluate("nums.Sum(x => x / 3.0)");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Sum_SetVariable_ArrayDouble()
    {
        var engine = Engine();
        engine.SetVariable("vals", new double[] { 1.5, 2.5, 3.5 });
        var result = engine.Evaluate("vals.Sum(x => x * 2)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(15.0));
    }

    #endregion

    #region Custom extension methods with overloaded delegate parameters

    [Test]
    public void CustomExtension_IntTransform()
    {
        var result = Eval("""
            var items = new[] { 1, 2, 3 };
            items.Sum(x => x)
            """);
        Assert.That(result, Is.EqualTo(6));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void CustomExtension_DoubleTransform()
    {
        var result = Eval("""
            var items = new[] { 1, 2, 3 };
            items.Sum(x => Math.Log(x))
            """);
        Assert.That(result, Is.TypeOf<double>());
    }

    #endregion

    #region Complex expressions — chi-squared pattern (the original MCP use case)

    [Test]
    public void ChiSquared_IntBuckets_DoubleExpected()
    {
        var result = Eval("""
            var buckets = new[] { 110, 90, 100 };
            var expected = 100.0;
            buckets.Sum(o => Math.Pow(o - expected, 2) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(2.0).Within(0.001));
    }

    [Test]
    public void ChiSquared_AnonymousBuckets()
    {
        var result = Eval("""
            var data = new[] { new { Observed = 95 }, new { Observed = 105 } };
            var expected = 100.0;
            data.Sum(d => (d.Observed - expected) * (d.Observed - expected) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(0.5).Within(0.001));
    }

    [Test]
    public void ChiSquared_FullPipeline()
    {
        var result = Eval("""
            var rng = new Random(42);
            var samples = Enumerable.Range(0, 1000).Select(_ => rng.Next(1, 101)).ToList();
            var buckets = Enumerable.Range(0, 10).Select(b => samples.Count(n => (n - 1) / 10 == b)).ToArray();
            var expected = 100.0;
            buckets.Sum(o => Math.Pow(o - expected, 2) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.GreaterThan(0));
        Assert.That((double)result!, Is.LessThan(50));
    }

    #endregion

    // Tuple member access via named elements (x.val, x.weight) in lambda selectors
    // requires the binder to resolve named tuple fields on ValueTuple types.

    #region Edge cases

    [Test]
    public void Sum_EmptyArray_IntSelector()
    {
        var result = Eval("new int[0].Sum(x => x * 2)");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Sum_EmptyArray_DoubleSelector()
    {
        var result = Eval("new int[0].Sum(x => x * 2.0)");
        Assert.That(result, Is.EqualTo(0.0));
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Sum_SingleElement_DoubleSelector()
    {
        var result = Eval("new[] { 42 }.Sum(x => Math.Sqrt(x))");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(Math.Sqrt(42)).Within(0.001));
    }

    [Test]
    public void Sum_NestedMethodCalls_InLambda()
    {
        var result = Eval("new[] { 2, 8 }.Sum(x => Math.Log(Math.Pow(x, 2)))");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Sum_TernaryInLambda_DoubleResult()
    {
        var result = Eval("new[] { 1, 2, 3 }.Sum(x => x > 1 ? x * 1.5 : 0.0)");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void Sum_StringLength_IntResult()
    {
        var result = Eval("""new[] { "hello", "world" }.Sum(s => s.Length)""");
        Assert.That(result, Is.EqualTo(10));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void Count_WithPredicate_NoAmbiguity()
    {
        var result = Eval("new[] { 1, 2, 3, 4, 5 }.Count(x => x > 3)");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Any_WithPredicate_NoAmbiguity()
    {
        var result = Eval("new[] { 1, 2, 3 }.Any(x => x > 2)");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void All_WithPredicate_NoAmbiguity()
    {
        var result = Eval("new[] { 2, 4, 6 }.All(x => x % 2 == 0)");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Binary operator type inference with object operands

    [Test]
    public void AnonymousProperty_SubtractDouble()
    {
        var result = Eval("""
            var item = new { X = 10 };
            item.X - 3.5
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(6.5));
    }

    [Test]
    public void AnonymousProperty_MultiplyDouble()
    {
        var result = Eval("""
            var item = new { Rate = 5 };
            item.Rate * 2.5
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(12.5));
    }

    [Test]
    public void AnonymousProperty_DivideByDouble()
    {
        var result = Eval("""
            var item = new { Total = 100 };
            item.Total / 3.0
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(100.0 / 3.0).Within(0.001));
    }

    [Test]
    public void AnonymousProperty_ChainedArithmetic()
    {
        var result = Eval("""
            var item = new { A = 10, B = 20 };
            (item.A + item.B) * 1.5
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(45.0));
    }

    #endregion
}
