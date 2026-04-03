using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

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
public class OverloadResolutionTests(CompilationMode mode)
{
    #region Extension Method Precedence (ECMA-334 §12.8.9.2)

    // Engine-only: uses SetVariable with typed objects (not serializable to Roslyn .csx)
    [Test]
    public void InstanceMethod_PreferredOverExtension_StringContains()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("text", "hello world");

        var result = engine.Evaluate("""text.Contains("hello") """);
        Assert.That(result, Is.True);
    }

    // Engine-only: uses SetVariable with List<int> (not serializable to Roslyn .csx)
    [Test]
    public void ExtensionMethod_WorksWhenNoInstanceMethod()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Where(x => x > 1).Count()");
        Assert.That(result, Is.EqualTo(2));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Select()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Select(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Where()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Where(x => x > 3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_OrderBy()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 3, 1, 2 });

        var result = engine.Evaluate("numbers.OrderBy(x => x).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_Aggregate()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Aggregate(0, (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(10));
    }

    // Engine-only: uses SetVariable with List<int>
    [Test]
    public void LinqExtensions_StillWork_First()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 5, 10, 15 });

        var result = engine.Evaluate("numbers.First()");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void AmbiguousOverloads_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("ambig", new AmbiguousOverloads());
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("ambig.M(1, 1)"));
        Assert.That(ex!.Message, Does.Contain("ambiguous"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0121));
    }

    [Test]
    public void NamedArgument_CaseSensitive_MismatchThrows()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("named", new NamedCaseTarget());
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("named.M(VALUE: 1)"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0304));
    }

    #endregion

    private sealed class AmbiguousOverloads
    {
        public string M(int a, long b) => "int,long";
        public string M(long a, int b) => "long,int";
    }

    private sealed class NamedCaseTarget
    {
        public int M(int value) => value;
    }

    [Test]
    public void MultiTypeParamGenericInference_Zip_Works()
    {
        var engine = new AlderEngine(o =>
        {
            if (mode == CompilationMode.Compiled) o.UseCompiler();
            o.Types.AddAssembly(typeof(Enumerable).Assembly);
            o.Types.AddNamespace("System.Linq");
        });
        var a = new[] { 1, 2, 3 };
        var b = new[] { "a", "b", "c" };
        engine.SetVariable("a", a);
        engine.SetVariable("b", b);

        var result = engine.Evaluate("a.Zip(b, (x, y) => x + y)");
        Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
        Assert.That(((IEnumerable<string>)result!).ToArray(), Is.EqualTo(new[] { "1a", "2b", "3c" }));
    }

    [Test]
    public void MultiTypeParamGenericInference_ToDictionary_Works()
    {
        var engine = new AlderEngine(o =>
        {
            if (mode == CompilationMode.Compiled) o.UseCompiler();
            o.Types.AddAssembly(typeof(Enumerable).Assembly);
            o.Types.AddNamespace("System.Linq");
        });
        var items = new[] { "apple", "banana", "cherry" };
        engine.SetVariable("items", items);

        var result = engine.Evaluate("items.ToDictionary(x => x, x => x.Length)");
        Assert.That(result, Is.InstanceOf<Dictionary<string, int>>());
        var dict = (Dictionary<string, int>)result!;
        Assert.That(dict["apple"], Is.EqualTo(5));
        Assert.That(dict["banana"], Is.EqualTo(6));
    }
}
