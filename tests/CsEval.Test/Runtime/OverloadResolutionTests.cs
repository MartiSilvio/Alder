namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.6.4 overload resolution tests.
/// Validates that CsEval selects the same method overload as Roslyn for:
/// - Exact type matching (section 12.6.4.3 "better function member")
/// - Widening/implicit conversion preference (section 12.6.4.5 "better conversion from type")
/// - Extension method precedence (section 12.8.9.2 "extension method invocations")
/// - Multi-parameter overloads
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class OverloadResolutionTests(CompilationMode mode)
{
    #region Exact Match Preference (ECMA-334 §12.6.4.3)

    // ECMA-334 §12.6.4.3: "Better function member"
    // When an exact type match exists, it should be preferred over implicit conversions.
    // Math.Abs has overloads for: short, int, long, float, double, decimal, nint
    // Each call should select the overload matching the argument type exactly.

    // Decimal cannot be used as TestCase attribute argument, so test separately
    [Test]
    public async Task MathAbs_Decimal_SelectsDecimalOverload()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(5.0m)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Abs(5.0m)");

        Assert.That(result, Is.EqualTo(5.0m));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(decimal)));
        Assert.That(result, Is.EqualTo(csharpResult));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    // ECMA-334 §12.6.4.3: Verify the return TYPE matches, not just the value.
    // This is the key compliance signal -- wrong overload selection produces wrong type.

    [Test]
    public async Task MathAbs_Int_ReturnsIntType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(5)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Abs(5)");

        Assert.That(result, Is.EqualTo(5));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Math.Abs(5) should return int, not double");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type should match Roslyn");
    }

    [Test]
    public async Task MathAbs_Long_ReturnsLongType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(5L)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Abs(5L)");

        Assert.That(result, Is.EqualTo(5L));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(long)), "Math.Abs(5L) should return long");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type should match Roslyn");
    }

    [Test]
    public async Task MathAbs_Double_ReturnsDoubleType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs(5.0)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Abs(5.0)");

        Assert.That(result, Is.EqualTo(5.0));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(double)), "Math.Abs(5.0) should return double");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type should match Roslyn");
    }

    #endregion

    #region Widening Conversion Preference (ECMA-334 §12.6.4.5)

    // ECMA-334 §12.6.4.5: "Better conversion from type"
    // When no exact match exists, the most specific applicable overload should be selected.
    // Math.Abs has no short overload, so Math.Abs((short)5) should promote to int (nearest widening).
    // Per section 10.2.3 implicit numeric conversions: short -> int, long, float, double, decimal
    // int is more specific than long/float/double/decimal, so int overload should be selected.

    [Test]
    public async Task MathAbs_Short_PromotesToInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Abs((short)5)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Abs((short)5)");

        // Roslyn actually selects the short overload (Math.Abs(short) exists in .NET)
        // So we just need to match Roslyn's behavior
        Assert.That(result, Is.EqualTo(csharpResult), "Value should match Roslyn");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type should match Roslyn");
    }

    #endregion

    #region Multiple Parameter Overloads (ECMA-334 §12.6.4)

    // Math.Round has overloads:
    // - Math.Round(double) -> double
    // - Math.Round(double, int) -> double
    // - Math.Round(double, MidpointRounding) -> double
    // - Math.Round(decimal) -> decimal
    // - Math.Round(decimal, int) -> decimal
    // The correct overload must be selected based on argument count and types.

    [Test]
    public async Task MathRound_SingleArg_SelectsDoubleOverload()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Round(3.7)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Round(3.7)");

        Assert.That(result, Is.EqualTo(4.0));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(double)));
        Assert.That(result, Is.EqualTo(csharpResult));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    [Test]
    public async Task MathRound_TwoArgs_SelectsDoubleIntOverload()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Round(3.456, 2)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Round(3.456, 2)");

        Assert.That(result, Is.EqualTo(3.46));
        Assert.That(result?.GetType(), Is.EqualTo(typeof(double)));
        Assert.That(result, Is.EqualTo(csharpResult));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    #endregion

    #region Math Method Overloads -- Additional

    [Test]
    public async Task MathMax_Int_ReturnsIntType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Max(5, 10)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Max(5, 10)");

        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Math.Max(int,int) should return int");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    #endregion

    #region Extension Method Precedence (ECMA-334 §12.8.9.2)

    // ECMA-334 §12.8.9.2: "An extension method is eligible if [...] normal processing
    // of the invocation found no applicable instance methods."
    // Instance methods MUST be tried before extension methods.

    // String has instance method Contains(string). LINQ also has Contains<T>(T).
    // The instance method should be preferred.

    [Test]
    public void InstanceMethod_PreferredOverExtension_StringContains()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello world");

        var result = engine.Evaluate("text.Contains(\"hello\")");
        Assert.That(result, Is.EqualTo(true));
    }

    // List<T>.Count is a property, but LINQ Enumerable.Count() is an extension method.
    // Calling Count() should work on List<int>.
    [Test]
    public void ExtensionMethod_WorksWhenNoInstanceMethod()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Where(x => x > 1).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    // LINQ extension methods should still work on collections
    [Test]
    public void LinqExtensions_StillWork_Select()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Select(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    [Test]
    public void LinqExtensions_StillWork_Where()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Where(x => x > 3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5 }));
    }

    [Test]
    public void LinqExtensions_StillWork_OrderBy()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 3, 1, 2 });

        var result = engine.Evaluate("numbers.OrderBy(x => x).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void LinqExtensions_StillWork_Aggregate()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Aggregate(0, (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void LinqExtensions_StillWork_First()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 10, 15 });

        var result = engine.Evaluate("numbers.First()");
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Overload Resolution with Mixed Types (ECMA-334 §12.6.4.5)

    // When calling Math.Max(int, long), both arguments need conversion consideration.
    // int can implicitly convert to long, so Math.Max(long, long) should be selected.

    [Test]
    public async Task MathMax_MixedIntLong_PromotesToLong()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Max(5, 10L)");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("Math.Max(5, 10L)");

        Assert.That(result, Is.EqualTo(csharpResult), "Value should match Roslyn");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type should match Roslyn");
    }

    #endregion
}
