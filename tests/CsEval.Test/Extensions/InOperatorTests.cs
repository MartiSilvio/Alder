namespace CsEval.Test.Extensions;

/// <summary>
/// Tests for 'in' operator (CsEval extension, not standard C#).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class InOperatorTests(CompilationMode mode)
{
    // Basic 'in' operator with arrays
    [TestCase("2 in [1, 2, 3]", true, TestName = "NumberInArray_True")]
    [TestCase("5 in [1, 2, 3]", false, TestName = "NumberNotInArray_False")]
    [TestCase("\"b\" in [\"a\", \"b\", \"c\"]", true, TestName = "StringInArray_True")]
    [TestCase("\"x\" in [\"a\", \"b\", \"c\"]", false, TestName = "StringNotInArray_False")]
    // 'in' with strings (substring check)
    [TestCase("\"bc\" in \"abcd\"", true, TestName = "SubstringInString_True")]
    [TestCase("\"xy\" in \"abcd\"", false, TestName = "SubstringNotInString_False")]
    // Combined with logical operators
    [TestCase("(2 in [1, 2, 3]) && (5 in [4, 5, 6])", true, TestName = "CombinedWithAnd")]
    [TestCase("(10 in [1, 2, 3]) || (5 in [4, 5, 6])", true, TestName = "CombinedWithOr")]
    [TestCase("!(5 in [1, 2, 3])", true, TestName = "NegatedWithNot")]
    // In ternary
    [TestCase("2 in [1, 2, 3] ? \"yes\" : \"no\"", "yes", TestName = "InTernary")]
    // Null handling
    [TestCase("null in [1, null, 3]", true, TestName = "NullInArray_True")]
    [TestCase("null in [1, 2, 3]", false, TestName = "NullNotInArray_False")]
    // Edge cases
    [TestCase("1 in []", false, TestName = "EmptyArray_False")]
    [TestCase("\"hello\" in [1, \"hello\", true]", true, TestName = "MixedTypeArray")]
    public void MatchesExpected(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    // Tests requiring variables
    [Test]
    public void WithVariable_Collection()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new List<int> { 1, 2, 3, 4, 5 });
        Assert.That(engine.Evaluate("3 in arr"), Is.True);
    }

    [Test]
    public void WithVariable_Value()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 3);
        Assert.That(engine.Evaluate("x in [1, 2, 3, 4, 5]"), Is.True);
    }

    [Test]
    public void WithNullCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("1 in arr"));
    }
}
