namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class VerbatimStringTests(CompilationMode mode) : TestBase
{
    #region Verbatim Strings (@"...")

    [Test]
    public void VerbatimString_BackslashesAreLiteral()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""path\to\file""");
        Assert.That(result, Is.EqualTo(@"path\to\file"));
    }

    [Test]
    public void VerbatimString_MultipleBackslashes()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""C:\Users\John\Documents""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John\Documents"));
    }

    [Test]
    public void VerbatimString_EscapedQuote()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""She said """"Hello"""".""");
        Assert.That(result, Is.EqualTo(@"She said ""Hello""."));
    }

    [Test]
    public void VerbatimString_EmptyString()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""""");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void VerbatimString_SingleQuoteDoesNotNeedEscape()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""It's fine""");
        Assert.That(result, Is.EqualTo("It's fine"));
    }

    [Test]
    public void VerbatimString_WithNewlines()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("@\"line1\nline2\"");
        Assert.That(result, Is.EqualTo("line1\nline2"));
    }

    [Test]
    public void VerbatimString_RegexPattern()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"@""^\d{3}-\d{4}$""");
        Assert.That(result, Is.EqualTo(@"^\d{3}-\d{4}$"));
    }

    #endregion

    #region Verbatim Interpolated Strings ($@"..." and @$"...")

    [Test]
    public void VerbatimInterpolated_DollarAt_BackslashesAreLiteral()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("name", "John");
        var result = engine.Evaluate(@"$@""C:\Users\{name}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_AtDollar_BackslashesAreLiteral()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("name", "John");
        var result = engine.Evaluate(@"@$""C:\Users\{name}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedQuote()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("name", "World");
        var result = engine.Evaluate(@"$@""Hello, """"dear"""" {name}""");
        Assert.That(result, Is.EqualTo(@"Hello, ""dear"" World"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedBraces()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 42);
        var result = engine.Evaluate(@"$@""Value: {x}, Literal: {{not interpolated}}""");
        Assert.That(result, Is.EqualTo("Value: 42, Literal: {not interpolated}"));
    }

    [Test]
    public void VerbatimInterpolated_MultipleExpressions()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("first", "C:");
        engine.SetVariable("second", "Users");
        engine.SetVariable("third", "John");
        var result = engine.Evaluate(@"$@""{first}\{second}\{third}""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_ExpressionWithMethod()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate(@"$@""Count: {items.Count()}""");
        Assert.That(result, Is.EqualTo("Count: 3"));
    }

    [Test]
    public void VerbatimInterpolated_NestedBraces()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("obj", new Dictionary<string, object?> { ["key"] = "value" });
        var result = engine.Evaluate(@"$@""Result: {obj[""key""]}""");
        Assert.That(result, Is.EqualTo("Result: value"));
    }

    #endregion

    #region Comparison with Regular Strings

    [Test]
    public void RegularString_BackslashNeedsEscape()
    {
        var engine = CreateEngine(mode);
        // Regular string needs escaped backslash
        var result = engine.Evaluate(@"""path\\to\\file""");
        Assert.That(result, Is.EqualTo(@"path\to\file"));
    }

    [Test]
    public void VerbatimVsRegular_SameResult()
    {
        var engine = CreateEngine(mode);
        // Both should produce the same result
        var verbatim = engine.Evaluate(@"@""C:\Windows\System32""");
        var regular = engine.Evaluate(@"""C:\\Windows\\System32""");
        Assert.That(verbatim, Is.EqualTo(regular));
    }

    #endregion
}
