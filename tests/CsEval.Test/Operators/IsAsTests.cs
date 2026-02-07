using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.12.12 — The is operator, §12.12.13 — The as operator,
/// §11.2 — Pattern matching (type patterns, var pattern, null patterns).
/// Tests is/is not type checking, as operator, pattern variable binding,
/// and var pattern (§11.2.4).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IsAsTests(CompilationMode mode)
{
    #region ECMA-334 §12.12.12 — Is Operator (Type Checking)

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsTypeCases))]
    public async Task Eval_IsType(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsVariableCases))]
    public async Task Is_Variable(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    [TestCase(42, "x is string", false, TestName = "Is_IntIsString_CrossType")]
    public void Is_Variable_CrossType(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsObjectCases))]
    public async Task Is_Object(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    #endregion

    #region ECMA-334 §11.2.1 — Null and Not-Null Patterns

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.NullCheckCases))]
    public async Task Is_NullCheck(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    [TestCase(42, "x is null", false, TestName = "Is_NonNullVariable_IsNull")]
    [TestCase(42, "x is not null", true, TestName = "Is_NonNullVariable_IsNotNull")]
    public void Is_NullCheck_ValueType(object? varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    #endregion

    #region ECMA-334 §11.2.3 — Is Not Type Pattern (C# 9+)

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsNotTypeParityCases))]
    public async Task IsNot_Type_Parity(object? varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

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

    #region ECMA-334 §12.12.12 — Is in Conditions and Combined with Logical Operators

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsInConditionCases))]
    public async Task Is_InCondition(object varValue, string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    [TestCase(42, "x is int && x is not null", true, TestName = "Is_WithLogicalAnd")]
    public void Is_WithLogicalOperators_ValueType(object varValue, string expr, bool expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.IsWithLogicalCases))]
    public async Task Is_WithLogicalOperators(object varValue, string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    #endregion

    #region ECMA-334 §12.12.13 — As Operator

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.AsOperatorCases))]
    public async Task As_Operator(object? varValue, string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

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

    #region ECMA-334 §12.12.13 — As Operator with Non-Keyword Types

    [Test]
    public void As_ClassType_Match()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new ArgumentException("test"));
        var result = engine.Evaluate("x as Exception");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public void As_ClassType_NoMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");
        var result = engine.Evaluate("x as Exception");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void As_ClassType_NullValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        var result = engine.Evaluate("x as Exception");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void As_ClassType_DerivedMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new InvalidOperationException("oops"));
        var result = engine.Evaluate("x as Exception");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<InvalidOperationException>());
    }

    #endregion

    #region ECMA-334 §11.2.2 — Type Pattern with Variable Binding

    [TestCaseSource(typeof(IsAsData), nameof(IsAsData.TypePatternInConditionalCases))]
    public async Task Is_TypePattern_InConditional(object varValue, string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, new() { ["x"] = varValue }, expected, mode);

    [TestCase(42, "x is string s ? s.ToUpper() : \"not a string\"", "not a string", TestName = "Is_TypePattern_InConditional_NoMatch")]
    public void Is_TypePattern_InConditional_CrossType(object varValue, string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", varValue);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(expected));
    }

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

    #region ECMA-334 §11.2.2 — Type Pattern with Non-Keyword Types

    [Test]
    public void Is_ClassType_Match()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new ArgumentException("test"));
        var result = engine.Evaluate("x is Exception ex");
        Assert.That(result, Is.True);
        Assert.That(engine.Evaluate("ex"), Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Is_ClassType_NoMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");
        var result = engine.Evaluate("x is Exception ex");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Is_ClassType_NullValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        var result = engine.Evaluate("x is Exception ex");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Is_ClassType_DerivedType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new ArgumentNullException("param"));
        var result = engine.Evaluate("x is ArgumentException argEx");
        Assert.That(result, Is.True);
        Assert.That(engine.Evaluate("argEx"), Is.InstanceOf<ArgumentNullException>());
    }

    [Test]
    public void Is_ClassType_InConditional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new InvalidOperationException("oops"));
        var result = engine.Evaluate("x is Exception ex ? ex.Message : \"no match\"");
        Assert.That(result, Is.EqualTo("oops"));
    }

    [Test]
    public void Is_ClassType_NoBinding()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", new Exception("test"));
        var result = engine.Evaluate("x is Exception ex");
        Assert.That(result, Is.True);
        Assert.That(engine.Evaluate("ex.Message"), Is.EqualTo("test"));
    }

    #endregion

    #region ECMA-334 §11.2 — Pattern Matching Edge Cases

    [Test]
    public void Is_TypePattern_VariableNotBoundOnFailure()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);

        var result = engine.Evaluate("x is string s");
        Assert.That(result, Is.False);

        Assert.Throws<CsEvalException>(() => engine.Evaluate("s"));
    }

    [Test]
    public async Task Is_TypePattern_VariableScopeInConditional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", "hello");

        var result = engine.Evaluate("x is string s ? s.Length : -1");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("var x = \"hello\"; x is string s ? s.Length : -1");

        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [Test]
    public void Is_NullCheck_OnBoxedValueType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object)42);

        Assert.That(engine.Evaluate("x is null"), Is.False);
        Assert.That(engine.Evaluate("x is not null"), Is.True);
    }

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

    #region ECMA-334 §11.2.4 — Var Pattern

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
