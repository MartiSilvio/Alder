using CsEval.TestData.Data;

namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class VerbatimStringTests(CompilationMode mode)
{
    // Verbatim strings (@"...") and comparison with regular strings
    [TestCaseSource(typeof(VerbatimStringData), nameof(VerbatimStringData.ValueCases))]
    public void MatchesExpected(string expr, string expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [Test]
    public void VerbatimString_WithNewlines()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("@\"line1\nline2\"");
        Assert.That(result, Is.EqualTo("line1\nline2"));
    }

    [Test]
    public void VerbatimVsRegular_SameResult()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var verbatim = engine.Evaluate(@"@""C:\Windows\System32""");
        var regular = engine.Evaluate(@"""C:\\Windows\\System32""");
        Assert.That(verbatim, Is.EqualTo(regular));
    }

    // Verbatim interpolated strings ($@"..." and @$"...") - require SetVariable
    [Test]
    public void VerbatimInterpolated_DollarAt_BackslashesAreLiteral()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "John");
        var result = engine.Evaluate(@"$@""C:\Users\{name}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_AtDollar_BackslashesAreLiteral()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "John");
        var result = engine.Evaluate(@"@$""C:\Users\{name}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedQuote()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "World");
        var result = engine.Evaluate(@"$@""Hello, """"dear"""" {name}""");
        Assert.That(result, Is.EqualTo(@"Hello, ""dear"" World"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedBraces()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate(@"$@""Value: {x}, Literal: {{not interpolated}}""");
        Assert.That(result, Is.EqualTo("Value: 42, Literal: {not interpolated}"));
    }

    [Test]
    public void VerbatimInterpolated_MultipleExpressions()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", "C:");
        engine.SetVariable("second", "Users");
        engine.SetVariable("third", "John");
        var result = engine.Evaluate(@"$@""{first}\{second}\{third}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_ExpressionWithMethod()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate(@"$@""Count: {items.Count()}""");
        Assert.That(result, Is.EqualTo("Count: 3"));
    }

    [Test]
    public void VerbatimInterpolated_NestedBraces()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", new Dictionary<string, object?> { ["key"] = "value" });
        var result = engine.Evaluate(@"$@""Result: {obj[""key""]}""");
        Assert.That(result, Is.EqualTo("Result: value"));
    }
}
