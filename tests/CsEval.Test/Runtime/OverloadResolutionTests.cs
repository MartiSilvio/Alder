namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.6.4 overload resolution tests.
/// Engine-only tests for extension method precedence and SetVariable-based scenarios.
///
/// Migrated to .csx parity:
///   - MathAbs (int, long, double, short, decimal) -> TestData/OverloadResolution/ and TestData/Runtime/OverloadResolution/
///   - MathRound (single arg, two args) -> TestData/Runtime/OverloadResolution/
///   - MathMax (int, mixed int/long) -> TestData/OverloadResolution/ and TestData/Runtime/OverloadResolution/
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class OverloadResolutionTests(CompilationMode mode)
{
    #region Extension Method Precedence (ECMA-334 §12.8.9.2)

    // Engine-only: uses SetVariable with typed objects (not serializable to Roslyn .csx)
    [Test]
    public void InstanceMethod_PreferredOverExtension_StringContains()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello world");

        var result = engine.Evaluate("text.Contains(\"hello\")");
        Assert.That(result, Is.EqualTo(true));
    }

    // Engine-only: uses SetVariable with List<int> (not serializable to Roslyn .csx)
    [Test]
    public void ExtensionMethod_WorksWhenNoInstanceMethod()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Where(x => x > 1).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Select()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Select(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Where()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Where(x => x > 3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_OrderBy()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 3, 1, 2 });

        var result = engine.Evaluate("numbers.OrderBy(x => x).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Aggregate()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Aggregate(0, (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(10));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_First()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 10, 15 });

        var result = engine.Evaluate("numbers.First()");
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion
}
