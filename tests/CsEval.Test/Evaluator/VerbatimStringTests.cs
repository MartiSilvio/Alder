using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class VerbatimStringTests : EvaluatorTestBase
{
    #region Verbatim Strings (@"...")

    [Test]
    public void VerbatimString_BackslashesAreLiteral()
    {
        var result = Eval(@"@""path\to\file""");
        Assert.That(result, Is.EqualTo(@"path\to\file"));
    }

    [Test]
    public void VerbatimString_MultipleBackslashes()
    {
        var result = Eval(@"@""C:\Users\John\Documents""");
        Assert.That(result, Is.EqualTo(@"C:\Users\John\Documents"));
    }

    [Test]
    public void VerbatimString_EscapedQuote()
    {
        var result = Eval(@"@""She said """"Hello"""".""");
        Assert.That(result, Is.EqualTo(@"She said ""Hello""."));
    }

    [Test]
    public void VerbatimString_EmptyString()
    {
        var result = Eval(@"@""""");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void VerbatimString_SingleQuoteDoesNotNeedEscape()
    {
        var result = Eval(@"@""It's fine""");
        Assert.That(result, Is.EqualTo("It's fine"));
    }

    [Test]
    public void VerbatimString_WithNewlines()
    {
        var result = Eval("@\"line1\nline2\"");
        Assert.That(result, Is.EqualTo("line1\nline2"));
    }

    [Test]
    public void VerbatimString_RegexPattern()
    {
        var result = Eval(@"@""^\d{3}-\d{4}$""");
        Assert.That(result, Is.EqualTo(@"^\d{3}-\d{4}$"));
    }

    #endregion

    #region Verbatim Interpolated Strings ($@"..." and @$"...")

    [Test]
    public void VerbatimInterpolated_DollarAt_BackslashesAreLiteral()
    {
        var context = new EvalContext();
        context.Define("name", "John");
        var result = Eval(@"$@""C:\Users\{name}""", context);
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_AtDollar_BackslashesAreLiteral()
    {
        var context = new EvalContext();
        context.Define("name", "John");
        var result = Eval(@"@$""C:\Users\{name}""", context);
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedQuote()
    {
        var context = new EvalContext();
        context.Define("name", "World");
        var result = Eval(@"$@""Hello, """"dear"""" {name}""", context);
        Assert.That(result, Is.EqualTo(@"Hello, ""dear"" World"));
    }

    [Test]
    public void VerbatimInterpolated_EscapedBraces()
    {
        var context = new EvalContext();
        context.Define("x", 42);
        var result = Eval(@"$@""Value: {x}, Literal: {{not interpolated}}""", context);
        Assert.That(result, Is.EqualTo("Value: 42, Literal: {not interpolated}"));
    }

    [Test]
    public void VerbatimInterpolated_MultipleExpressions()
    {
        var context = new EvalContext();
        context.Define("first", "C:");
        context.Define("second", "Users");
        context.Define("third", "John");
        var result = Eval(@"$@""{first}\{second}\{third}""", context);
        Assert.That(result, Is.EqualTo(@"C:\Users\John"));
    }

    [Test]
    public void VerbatimInterpolated_ExpressionWithMethod()
    {
        var context = new EvalContext();
        context.Define("items", new List<int> { 1, 2, 3 });
        var result = Eval(@"$@""Count: {items.Count()}""", context);
        Assert.That(result, Is.EqualTo("Count: 3"));
    }

    [Test]
    public void VerbatimInterpolated_NestedBraces()
    {
        var context = new EvalContext();
        context.Define("obj", new Dictionary<string, object?> { ["key"] = "value" });
        var result = Eval(@"$@""Result: {obj[""key""]}""", context);
        Assert.That(result, Is.EqualTo("Result: value"));
    }

    #endregion

    #region Comparison with Regular Strings

    [Test]
    public void RegularString_BackslashNeedsEscape()
    {
        // Regular string needs escaped backslash
        var result = Eval(@"""path\\to\\file""");
        Assert.That(result, Is.EqualTo(@"path\to\file"));
    }

    [Test]
    public void VerbatimVsRegular_SameResult()
    {
        // Both should produce the same result
        var verbatim = Eval(@"@""C:\Windows\System32""");
        var regular = Eval(@"""C:\\Windows\\System32""");
        Assert.That(verbatim, Is.EqualTo(regular));
    }

    #endregion
}
