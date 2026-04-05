using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

/// <summary>
/// Regression tests for bugs discovered through MCP/LLM usage patterns.
/// Tests here require SetVariable, type assertions, or complex validation
/// that can't be expressed as .csx parity files.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class McpRegressionTests(CompilationMode mode)
{
    [Test]
    public void Sum_WithDoubleSelector_OnIntArray()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("new int[] { 100, 110, 90 }.Sum(x => Math.Pow(x - 100, 2) / 100.0)");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Sum_WithDoubleSelector_OnAnonymousObjects()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("""
            var items = new[] { new { Count = 110 }, new { Count = 90 } };
            var expected = 100.0;
            items.Sum(b => Math.Pow(b.Count - expected, 2) / expected)
            """);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(2.0));
    }

    [Test]
    public void AnonymousObject_SimpleReturn()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("""new { x = 1, y = "hello" }""");
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Sum_OnCharSequence_IntSelector()
    {
        var engine = TestEngineFactory.Create(mode);
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
        var result = TestEngineFactory.Create(mode).Evaluate("var x = 10; (double)x / 3");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Ratio_IntDividedByInt_AsDouble()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("var totalBits = 23; var asciiBits = 88; (1.0 - (double)totalBits / asciiBits) * 100");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(73.86).Within(0.1));
    }

    [Test]
    public void Huffman_SecondAttempt_RatioCalculation()
    {
        var result = TestEngineFactory.Create(mode).Evaluate("""
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

    [Test]
    public void DotNet_StringJoin_Sanity()
    {
        Assert.That(string.Join(", ", new int[] { 1, 2, 3 }), Is.EqualTo("1, 2, 3"));
    }
}
