namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IsAsTests(CompilationMode mode)
{
    #region Is Operator - Type Checking

    [TestCase("42 is int", true, TestName = "Is_IntIsInt")]
    [TestCase("42L is long", true, TestName = "Is_LongIsLong")]
    [TestCase("3.14 is double", true, TestName = "Is_DoubleIsDouble")]
    [TestCase("3.14f is float", true, TestName = "Is_FloatIsFloat")]
    [TestCase("3.14m is decimal", true, TestName = "Is_DecimalIsDecimal")]
    [TestCase("true is bool", true, TestName = "Is_BoolIsBool")]
    [TestCase("'a' is char", true, TestName = "Is_CharIsChar")]
    [TestCase("\"hello\" is string", true, TestName = "Is_StringIsString")]
    [TestCase("42 is long", false, TestName = "Is_IntIsLong_False")]
    [TestCase("42 is double", false, TestName = "Is_IntIsDouble_False")]
    [TestCase("42 is string", false, TestName = "Is_IntIsString_False")]
    [TestCase("\"hello\" is int", false, TestName = "Is_StringIsInt_False")]
    [TestCase("true is int", false, TestName = "Is_BoolIsInt_False")]
    public async Task Eval_IsType(string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase(42, "x is int", "42 is int", true, TestName = "Is_Variable_Int")]
    [TestCase("hello", "x is string", "\"hello\" is string", true, TestName = "Is_Variable_String")]
    [TestCase(42, "x is string", "42 is string", false, TestName = "Is_Variable_WrongType")]
    public async Task Is_Variable(object? varValue, string expr, string csharpExpr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(csharpExpr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    // Is object tests need special handling due to boxing semantics
    [TestCase(42, "x is object", true, TestName = "Is_IntIsObject")]
    [TestCase("hello", "x is object", true, TestName = "Is_StringIsObject")]
    [TestCase(null, "x is object", false, TestName = "Is_NullIsObject")]
    public void Is_Object(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region Is Null / Is Not Null

    [TestCase(null, "x is null", true, TestName = "Is_NullVariable_IsNull")]
    [TestCase(null, "x is not null", false, TestName = "Is_NullVariable_IsNotNull")]
    [TestCase(42, "x is null", false, TestName = "Is_NonNullVariable_IsNull")]
    [TestCase(42, "x is not null", true, TestName = "Is_NonNullVariable_IsNotNull")]
    [TestCase("hello", "x is not null", true, TestName = "Is_StringVariable_IsNotNull")]
    public void Is_NullCheck(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region Is in Conditions

    [TestCase(42, "x is int ? \"yes\" : \"no\"", "42 is int ? \"yes\" : \"no\"", "yes", TestName = "Is_InConditional")]
    [TestCase("hello", "x is int ? \"yes\" : \"no\"", "\"hello\" is int ? \"yes\" : \"no\"", "no", TestName = "Is_InConditional_False")]
    [TestCase(42, "(x is int) == true", "(42 is int) == true", true, TestName = "Is_Precedence_WithEquality")]
    [TestCase("hello", "!(x is int)", "!(\"hello\" is int)", true, TestName = "Is_Precedence_WithNegation")]
    public async Task Is_InCondition(object varValue, string expr, string csharpExpr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(csharpExpr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase(42, "x is int && x is not null", true, TestName = "Is_WithLogicalAnd")]
    [TestCase("hello", "x is int || x is string", true, TestName = "Is_WithLogicalOr")]
    public void Is_WithLogicalOperators(object varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region As Operator

    [TestCase("hello", "x as string", "\"hello\" as string", "hello", TestName = "As_StringToString")]
    [TestCase(42, "x as string", "((object)42) as string", null, TestName = "As_ObjectToString_WhenInt")]
    [TestCase(null, "x as string", "((object?)null) as string", null, TestName = "As_NullToString")]
    [TestCase("hello", "(x as string) is not null ? \"found\" : \"not found\"", "(\"hello\" as string) is not null ? \"found\" : \"not found\"", "found", TestName = "As_InConditional")]
    [TestCase(42, "(x as string) ?? \"default\"", "(((object)42) as string) ?? \"default\"", "default", TestName = "As_ChainedWithNullCoalesce")]
    [TestCase("hello", "(x as string) ?? \"default\"", "(\"hello\" as string) ?? \"default\"", "hello", TestName = "As_ChainedWithNullCoalesce_WhenNotNull")]
    public async Task As_Operator(object? varValue, string expr, string csharpExpr, object? expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(csharpExpr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [Test]
    public async Task As_InConditional_WhenNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate("(x as string) is null ? \"not string\" : \"is string\"");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(((object)42) as string) is null ? \"not string\" : \"is string\"");

        Assert.That(result, Is.EqualTo("not string"));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    #endregion

    #region Pattern Matching with Variable

    [Test]
    public async Task Is_TypePattern_WithVariable_Match()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");
        var result = engine.Evaluate("x is string s");
        var sValue = engine.Evaluate("s");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("\"hello\" is string s");

        Assert.That(result, Is.True);
        Assert.That(sValue, Is.EqualTo("hello"));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [Test]
    public void Is_TypePattern_WithVariable_NoMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate("x is string s");

        Assert.That(result, Is.False);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("s"));
    }

    [Test]
    public async Task Is_TypePattern_WithVariable_Int()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate("x is int i");
        var iValue = engine.Evaluate("i");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("42 is int i");

        Assert.That(result, Is.True);
        Assert.That(iValue, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [TestCase("hello", "x is string s ? s.ToUpper() : \"not a string\"", "\"hello\" is string s ? s.ToUpper() : \"not a string\"", "HELLO", TestName = "Is_TypePattern_InConditional")]
    [TestCase(42, "x is string s ? s.ToUpper() : \"not a string\"", "((object)42) is string s ? s.ToUpper() : \"not a string\"", "not a string", TestName = "Is_TypePattern_InConditional_NoMatch")]
    public async Task Is_TypePattern_InConditional(object varValue, string expr, string csharpExpr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(csharpExpr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [Test]
    public async Task Is_TypePattern_Object()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");
        var result = engine.Evaluate("x is object o");
        var oValue = engine.Evaluate("o");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("\"hello\" is object o");

        Assert.That(result, Is.True);
        Assert.That(oValue, Is.EqualTo("hello"));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [Test]
    public async Task Is_TypePattern_NullValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        var result = engine.Evaluate("x is string s");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("((string?)null) is string s");

        Assert.That(result, Is.False);
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    #endregion
}
