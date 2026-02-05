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
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase(42, "x is int", true, TestName = "Is_Variable_Int")]
    [TestCase("hello", "x is string", true, TestName = "Is_Variable_String")]
    public async Task Is_Variable(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // Cross-type pattern matching - CsEval allows runtime checks on boxed values
    // In C#, int x = 42; x is string would be a compile error, but CsEval uses object boxing
    [TestCase(42, "x is string", false, TestName = "Is_IntIsString_CrossType")]
    public void Is_Variable_CrossType(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [TestCase(42, "x is object", true, TestName = "Is_IntIsObject")]
    [TestCase("hello", "x is object", true, TestName = "Is_StringIsObject")]
    [TestCase(null, "x is object", false, TestName = "Is_NullIsObject")]
    public async Task Is_Object(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    #endregion

    #region Is Null / Is Not Null

    [TestCase(null, "x is null", true, TestName = "Is_NullVariable_IsNull")]
    [TestCase(null, "x is not null", false, TestName = "Is_NullVariable_IsNotNull")]
    [TestCase("hello", "x is not null", true, TestName = "Is_StringVariable_IsNotNull")]
    public async Task Is_NullCheck(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // Value types with null checks - CsEval uses boxed values (object) for this
    // In C#, int x = 42; x is null is a compile error, but CsEval allows runtime check
    [TestCase(42, "x is null", false, TestName = "Is_NonNullVariable_IsNull")]
    [TestCase(42, "x is not null", true, TestName = "Is_NonNullVariable_IsNotNull")]
    public void Is_NullCheck_ValueType(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    #endregion

    #region Is Not Type

    // is not <type> pattern (C# 9+)
    // Can use parity tests when value is null (gets typed as object in Roslyn)
    [TestCase(null, "x is not object", true, TestName = "IsNot_NullIsNotObject_True")]
    [TestCase(null, "x is not string", true, TestName = "IsNot_NullIsNotString_True")]
    public async Task IsNot_Type_Parity(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // Cross-type pattern matching - CsEval allows runtime checks on boxed values
    // In C#, int x = 42; x is not string would be a compile error, but CsEval uses object boxing
    [TestCase("hello", "x is not int", true, TestName = "IsNot_StringIsNotInt_True")]
    [TestCase(42, "x is not string", true, TestName = "IsNot_IntIsNotString_True")]
    [TestCase("hello", "x is not string", false, TestName = "IsNot_StringIsNotString_False")]
    [TestCase(42, "x is not int", false, TestName = "IsNot_IntIsNotInt_False")]
    public void IsNot_Type_CrossType(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [TestCase("hello", "x is not int ? \"not int\" : \"is int\"", "not int", TestName = "IsNot_InConditional")]
    [TestCase(42, "x is not string ? \"not string\" : \"is string\"", "not string", TestName = "IsNot_InConditional_Match")]
    public void IsNot_Type_InConditional(object varValue, string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    #endregion

    #region Is in Conditions

    [TestCase(42, "x is int ? \"yes\" : \"no\"", "yes", TestName = "Is_InConditional")]
    [TestCase("hello", "x is int ? \"yes\" : \"no\"", "no", TestName = "Is_InConditional_False")]
    [TestCase(42, "(x is int) == true", true, TestName = "Is_Precedence_WithEquality")]
    [TestCase("hello", "!(x is int)", true, TestName = "Is_Precedence_WithNegation")]
    public async Task Is_InCondition(object varValue, string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // Logical operators with null checks on value types - CsEval specific behavior
    [TestCase(42, "x is int && x is not null", true, TestName = "Is_WithLogicalAnd")]
    public void Is_WithLogicalOperators_ValueType(object varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [TestCase("hello", "x is int || x is string", true, TestName = "Is_WithLogicalOr")]
    public async Task Is_WithLogicalOperators(object varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    #endregion

    #region As Operator

    [TestCase("hello", "x as string", "hello", TestName = "As_StringToString")]
    [TestCase(null, "x as string", null, TestName = "As_NullToString")]
    [TestCase("hello", "(x as string) is not null ? \"found\" : \"not found\"", "found", TestName = "As_InConditional")]
    [TestCase("hello", "(x as string) ?? \"default\"", "hello", TestName = "As_ChainedWithNullCoalesce_WhenNotNull")]
    public async Task As_Operator(object? varValue, string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // As operator on value types - CsEval allows runtime checks on boxed values
    // In C#, int x = 42; x as string is a compile error
    [TestCase(42, "x as string", null, TestName = "As_IntAsString")]
    [TestCase(42, "(x as string) ?? \"default\"", "default", TestName = "As_IntAsString_WithCoalesce")]
    [TestCase(42, "(x as string) is null ? \"not string\" : \"is string\"", "not string", TestName = "As_IntAsString_InConditional")]
    public void As_Operator_ValueType(object varValue, string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
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
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = \"hello\"; x is string s");

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
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = 42; x is int i");

        Assert.That(result, Is.True);
        Assert.That(iValue, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [TestCase("hello", "x is string s ? s.ToUpper() : \"not a string\"", "HELLO", TestName = "Is_TypePattern_InConditional")]
    public async Task Is_TypePattern_InConditional(object varValue, string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    // Cross-type pattern matching - CsEval specific behavior
    [TestCase(42, "x is string s ? s.ToUpper() : \"not a string\"", "not a string", TestName = "Is_TypePattern_InConditional_NoMatch")]
    public void Is_TypePattern_InConditional_CrossType(object varValue, string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [Test]
    public async Task Is_TypePattern_Object()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");
        var result = engine.Evaluate("x is object o");
        var oValue = engine.Evaluate("o");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = \"hello\"; x is object o");

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
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = (object?)null; x is string s");

        Assert.That(result, Is.False);
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    #endregion

    #region ECMA-334 Edge Cases - Pattern Matching

    // Pattern variable should NOT be bound when match fails
    [Test]
    public void Is_TypePattern_VariableNotBoundOnFailure()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);

        // The pattern doesn't match (42 is not a string)
        var result = engine.Evaluate("x is string s");
        Assert.That(result, Is.False);

        // s should NOT be defined in the context
        Assert.Throws<CsEvalException>(() => engine.Evaluate("s"));
    }

    // Pattern variable scope in conditional
    [Test]
    public async Task Is_TypePattern_VariableScopeInConditional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");

        // s should be usable in the then branch
        var result = engine.Evaluate("x is string s ? s.Length : -1");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = \"hello\"; x is string s ? s.Length : -1");

        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    // Null check on value type (via boxed object)
    [Test]
    public void Is_NullCheck_OnBoxedValueType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object)42); // Boxed int

        // Boxed value types are never null
        Assert.That(engine.Evaluate("x is null"), Is.False);
        Assert.That(engine.Evaluate("x is not null"), Is.True);
    }

    // Type pattern with nullable value type
    [TestCase("x is int", true, TestName = "Is_NullableInt_IsInt_True")]
    public void Is_NullableValueType(string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        int? x = 42;
        engine.SetVariable("x", x);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [Test]
    public void Is_NullableWithNullValue_IsType_False()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        int? x = null;
        engine.SetVariable("x", x);
        Assert.That(engine.Evaluate("x is int"), Is.False);
    }

    #endregion

    #region ECMA-334 §11.2.4 - Var Pattern

    [Test]
    public void VarPattern_AlwaysMatches()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        Assert.That(engine.Evaluate("x is var y"), Is.True);
        Assert.That(engine.Evaluate("y"), Is.EqualTo(42));
    }

    [Test]
    public void VarPattern_MatchesNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        Assert.That(engine.Evaluate("x is var y"), Is.True);
        Assert.That(engine.Evaluate("y"), Is.Null);
    }

    [Test]
    public void VarPattern_InBlock()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 42; if (x is var y) { return y * 2; } return 0; }");
        Assert.That(result, Is.EqualTo(84));
    }

    #endregion
}
