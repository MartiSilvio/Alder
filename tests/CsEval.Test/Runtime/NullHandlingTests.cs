namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NullHandlingTests(CompilationMode mode)
{
    [TestCase("""
              {
                  int? x = null;
                  x ??= 42;
                  return x;
              }
              """,
        42,
        TestName = "NullCoalesceAssign_AssignsWhenNull")]
    [TestCase("""
              {
                  int? x = null;
                  return x ??= 42;
              }
              """,
        42,
        TestName = "NullCoalesceAssign_ReturnsAssignedValue")]
    [TestCase("""
              {
                  var x = "hello";
                  return x ??= "world";
              }
              """,
        "hello",
        TestName = "NullCoalesceAssign_ReturnsExistingValue")]
    [TestCase("""
              {
                  int? x = null;
                  x ??= 5 + 5;
                  return x;
              }
              """,
        10,
        TestName = "NullCoalesceAssign_WithExpression")]
    [TestCase("""
              {
                  int? x = null;
                  if (true) {
                      x ??= 100;
                  }
                  return x;
              }
              """,
        100,
        TestName = "NullCoalesceAssign_InIfStatement")]
    public async Task Eval_NullCoalesceAssign(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void Eval_NullCoalesceAssign_ThrowsOnNonNullableType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
                                                                      {
                                                                          var x = 10;
                                                                          x ??= 42;
                                                                          return x;
                                                                      }
                                                                      """));
        Assert.That(ex!.Message, Does.Contain("??=").And.Contain("Int32"));
    }

    // Tests with External Variables

    [Test]
    public void Eval_NullCoalesce()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        engine.SetVariable("y", "default");

        Assert.That(engine.Evaluate("x ?? y"), Is.EqualTo("default"));
        Assert.That(engine.Evaluate("y ?? \"other\""), Is.EqualTo("default"));
    }

    [TestCase("null ?? null ?? \"c\"", "c", TestName = "NullCoalesce_Chained_BothNull")]
    [TestCase("null ?? \"b\" ?? \"c\"", "b", TestName = "NullCoalesce_Chained_FirstNull")]
    [TestCase("\"a\" ?? \"b\" ?? \"c\"", "a", TestName = "NullCoalesce_Chained_NoneNull")]
    [TestCase("null ?? null ?? null ?? \"d\"", "d", TestName = "NullCoalesce_Chained_ThreeNulls")]
    public async Task Eval_NullCoalesce_RightAssociativity(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void Eval_NullSafeAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);

        Assert.That(engine.Evaluate("obj?.Name"), Is.Null);
    }

    #region ECMA-334 §12.4.8 - Lifted Operators (Nullable)

    [TestCase("{ int? a = 5; int? b = null; return a + b; }", null, TestName = "LiftedAdd_WithNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = 3; return a * b; }", null, TestName = "LiftedMultiply_WithNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a + b; }", 8, TestName = "LiftedAdd_BothNonNull_ReturnsSum")]
    [TestCase("{ int? a = null; int? b = null; return a + b; }", null, TestName = "LiftedAdd_BothNull_ReturnsNull")]
    public async Task LiftedOperators_Arithmetic(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = 5; int? b = null; return a > b; }", false, TestName = "LiftedComparison_WithNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = 3; return a < b; }", false, TestName = "LiftedLessThan_WithNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a >= b; }", false, TestName = "LiftedGreaterEqual_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 3; return a > b; }", true, TestName = "LiftedGreater_BothNonNull_ReturnsTrue")]
    public async Task LiftedOperators_Comparison(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = null; return a == b; }", true, TestName = "LiftedEquality_BothNull_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = null; return a == b; }", false, TestName = "LiftedEquality_OneNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 5; return a == b; }", true, TestName = "LiftedEquality_Equal_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = 3; return a != b; }", true, TestName = "LiftedInequality_NotEqual_ReturnsTrue")]
    public async Task LiftedOperators_Equality(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.2 - Precedence with Null-Coalescing and Ternary

    [TestCase("{ string? n = null; return true ? n ?? \"fallback\" : \"else\"; }", "fallback", TestName = "Precedence_TernaryWithNullCoalesce_InTrue")]
    [TestCase("{ string? n = null; return false ? \"then\" : n ?? \"fallback\"; }", "fallback", TestName = "Precedence_TernaryWithNullCoalesce_InFalse")]
    public async Task Precedence_TernaryAndNullCoalesce(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.8.12 - Null-Conditional Element Access

    [Test]
    public void NullConditionalIndex_NullArray_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.That(engine.Evaluate("arr?[0]"), Is.Null);
    }

    [Test]
    public void NullConditionalIndex_NonNullArray_ReturnsElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new[] { 1, 2, 3 });
        Assert.That(engine.Evaluate("arr?[1]"), Is.EqualTo(2));
    }

    [Test]
    public void NullConditionalIndex_WithNullCoalescing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.That(engine.Evaluate("arr?[0] ?? 42"), Is.EqualTo(42));
    }

    [Test]
    public void NullConditionalIndex_Dictionary()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("dict", new Dictionary<string, int> { ["key"] = 100 });
        Assert.That(engine.Evaluate("dict?[\"key\"]"), Is.EqualTo(100));
        engine.SetVariable("dict", null);
        Assert.That(engine.Evaluate("dict?[\"key\"]"), Is.Null);
    }

    [TestCase("{ string? s = null; return s?.Length ?? 0; }", 0, TestName = "NullConditional_WithNullCoalesce_Null")]
    [TestCase("{ string? s = \"hello\"; return s?.Length ?? 0; }", 5, TestName = "NullConditional_WithNullCoalesce_NonNull")]
    public async Task NullConditional_ChainedWithNullCoalesce(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void NullConditional_MethodCall()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");
        Assert.That(engine.Evaluate("s?.ToUpper()"), Is.EqualTo("HELLO"));
        engine.SetVariable("s", null);
        Assert.That(engine.Evaluate("s?.ToUpper()"), Is.Null);
    }

    #endregion
}