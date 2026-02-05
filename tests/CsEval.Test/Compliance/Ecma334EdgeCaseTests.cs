namespace CsEval.Test.Compliance;

/// <summary>
/// ECMA-334 edge case tests derived from the compliance audit.
/// Tests operator precedence, numeric promotion, conversions, and null semantics
/// per the C# language specification (ECMA-334 7th Edition, December 2023).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class Ecma334EdgeCaseTests(CompilationMode mode)
{
    #region §12.4.7.3 - Binary Numeric Promotion

    [TestCase("1 + 1L", typeof(long), TestName = "NumericPromotion_IntPlusLong_IsLong")]
    [TestCase("1L + 1", typeof(long), TestName = "NumericPromotion_LongPlusInt_IsLong")]
    [TestCase("1 + 1.0", typeof(double), TestName = "NumericPromotion_IntPlusDouble_IsDouble")]
    [TestCase("1.0f + 1.0", typeof(double), TestName = "NumericPromotion_FloatPlusDouble_IsDouble")]
    [TestCase("1.0f + 1", typeof(float), TestName = "NumericPromotion_FloatPlusInt_IsFloat")]
    [TestCase("1L + 1.0f", typeof(float), TestName = "NumericPromotion_LongPlusFloat_IsFloat")]
    [TestCase("(byte)1 + (byte)2", typeof(int), TestName = "NumericPromotion_BytePlusByte_IsInt")]
    [TestCase("(short)1 + (short)2", typeof(int), TestName = "NumericPromotion_ShortPlusShort_IsInt")]
    public async Task NumericPromotion_ResultType(string expr, Type expectedType)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result?.GetType(), Is.EqualTo(expectedType), $"CsEval type mismatch for: {expr}");
        Assert.That(csharpResult?.GetType(), Is.EqualTo(expectedType), $"C# type mismatch for: {expr}");
    }

    #endregion

    #region §12.4.8 - Lifted Operators (Nullable)

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

    #region §12.15 - Null-Coalescing Operator (Right-Associativity)

    [TestCase("{ string? a = null; string? b = null; return a ?? b ?? \"c\"; }", "c", TestName = "NullCoalesce_NestedNulls")]
    [TestCase("{ string? a = null; return a ?? \"b\" ?? \"c\"; }", "b", TestName = "NullCoalesce_FirstNull")]
    [TestCase("{ string? a = \"a\"; return a ?? \"b\" ?? \"c\"; }", "a", TestName = "NullCoalesce_NoNulls")]
    [TestCase("{ string? a = null; string? b = null; string? c = null; return a ?? b ?? c ?? \"d\"; }", "d", TestName = "NullCoalesce_ThreeNulls")]
    public async Task NullCoalesce_RightAssociativity(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region §12.4.2 - Operator Precedence Edge Cases

    // Precedence between ?? and ?: (ternary has lower precedence than ??)
    [TestCase("{ string? n = null; return true ? n ?? \"fallback\" : \"else\"; }", "fallback", TestName = "Precedence_TernaryWithNullCoalesce_InTrue")]
    [TestCase("{ string? n = null; return false ? \"then\" : n ?? \"fallback\"; }", "fallback", TestName = "Precedence_TernaryWithNullCoalesce_InFalse")]
    public async Task Precedence_TernaryAndNullCoalesce(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Shift vs addition (addition/subtraction has higher precedence than shift)
    [TestCase("1 << 2 + 3", 32, TestName = "Precedence_ShiftVsAdd_AddFirst")]
    [TestCase("16 >> 1 + 1", 4, TestName = "Precedence_ShiftVsAdd_RightShift")]
    [TestCase("1 << 1 << 1", 4, TestName = "Precedence_ShiftLeftAssociativity")]
    public async Task Precedence_ShiftAndArithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Note: "5 & 3 == 3" and "5 | 2 == 2" are compile errors in C# - bool cannot be
    // implicitly converted to int. The bitwise operator applies to the comparison result (bool).
    // These tests are removed as they're not valid C# expressions.

    // Logical vs bitwise (& is higher precedence than &&)
    [TestCase("true & true && false", false, TestName = "Precedence_BitwiseVsLogicalAnd")]
    [TestCase("false | false || true", true, TestName = "Precedence_BitwiseVsLogicalOr")]
    public async Task Precedence_BitwiseVsLogical(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region §10.2.3, §10.3.2 - Conversion Edge Cases

    // Char to numeric conversions (explicit cast)
    [TestCase("(int)'A'", 65, TestName = "CharToInt_Conversion")]
    [TestCase("(long)'A'", 65L, TestName = "CharToLong_Conversion")]
    [TestCase("(double)'A'", 65.0, TestName = "CharToDouble_Conversion")]
    public async Task Conversion_CharExplicit(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Float/double truncation
    [TestCase("(int)3.9", 3, TestName = "DoubleToInt_Truncates")]
    [TestCase("(int)-3.9", -3, TestName = "NegativeDoubleToInt_Truncates")]
    [TestCase("(long)3.9", 3L, TestName = "DoubleToLong_Truncates")]
    [TestCase("(int)3.5f", 3, TestName = "FloatToInt_Truncates")]
    public async Task Conversion_FloatTruncation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Narrowing conversions (unchecked overflow wraps)
    [TestCase("unchecked((byte)256)", (byte)0, TestName = "IntToByte_Overflow")]
    [TestCase("unchecked((sbyte)128)", (sbyte)-128, TestName = "IntToSbyte_Overflow")]
    [TestCase("unchecked((short)32768)", (short)-32768, TestName = "IntToShort_Overflow")]
    public async Task Conversion_NarrowingOverflow(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region §10.3.7 - Unboxing Edge Cases

    [Test]
    public async Task Unboxing_ExactTypeMatch_Required()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        // Unboxing to same type works
        Assert.That(engine.Evaluate("(int)boxed"), Is.EqualTo(42));

        // Unboxing to wrong type should throw InvalidCastException
        Assert.Catch<InvalidCastException>(() => engine.Evaluate("(long)boxed"));

        // Verify C# has same behavior
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int)(object)42");
        Assert.That(csharpResult, Is.EqualTo(42));
    }

    [Test]
    public void Unboxing_NullableFromBoxed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        // Unboxing to nullable of correct type should work
        var result = engine.Evaluate("(int?)boxed");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region IEEE 754 - NaN Handling

    [Test]
    public async Task IEEE754_NaN_ZeroDivZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("0.0 / 0.0");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("0.0 / 0.0");

        Assert.That(double.IsNaN((double)result!));
        Assert.That(double.IsNaN((double)csharpResult!));
    }

    [Test]
    public async Task IEEE754_NaN_Equality()
    {
        // NaN == NaN should be false, NaN != NaN should be true
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nan", double.NaN);

        Assert.That(engine.Evaluate("nan == nan"), Is.False);
        Assert.That(engine.Evaluate("nan != nan"), Is.True);

        // Verify C# has same behavior
        var eqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN == double.NaN");
        var neqResult = await TestHelpers.EvaluateCSharpAsync("double.NaN != double.NaN");
        Assert.That(eqResult, Is.False);
        Assert.That(neqResult, Is.True);
    }

    [TestCase("1.0 / 0.0", double.PositiveInfinity, TestName = "Double_PositiveInfinity")]
    [TestCase("-1.0 / 0.0", double.NegativeInfinity, TestName = "Double_NegativeInfinity")]
    public async Task IEEE754_Infinity(string expr, double expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(csharpResult, Is.EqualTo(expected));
    }

    #endregion

    #region Null-Conditional and Null-Coalescing Interaction

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

    #region §12.9.2, §12.9.3 - Unary Operators

    [TestCase("+5", 5, TestName = "UnaryPlus_Int")]
    [TestCase("+5L", 5L, TestName = "UnaryPlus_Long")]
    [TestCase("+5.0", 5.0, TestName = "UnaryPlus_Double")]
    [TestCase("+5.0f", 5.0f, TestName = "UnaryPlus_Float")]
    [TestCase("+-5", -5, TestName = "UnaryPlus_NegativeValue")]
    [TestCase("-+5", -5, TestName = "UnaryMinus_AfterPlus")]
    [TestCase("-(-5)", 5, TestName = "DoubleNegation_Parenthesized")]
    [TestCase("+(+5)", 5, TestName = "DoublePlus_Parenthesized")]
    public async Task UnaryOperators_Arithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Method Overload Resolution Edge Cases

    [TestCase("Math.Max(1, 2)", 2, TestName = "Overload_IntInt")]
    [TestCase("Math.Max(1L, 2L)", 2L, TestName = "Overload_LongLong")]
    [TestCase("Math.Max(1.0, 2.0)", 2.0, TestName = "Overload_DoubleDouble")]
    [TestCase("Math.Abs(-5)", 5, TestName = "Overload_AbsInt")]
    [TestCase("Math.Abs(-5L)", 5L, TestName = "Overload_AbsLong")]
    [TestCase("Math.Abs(-5.0)", 5.0, TestName = "Overload_AbsDouble")]
    public async Task MethodOverload_NumericTypes(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("\"hello\".Substring(1)", "ello", TestName = "Overload_Substring_OneArg")]
    [TestCase("\"hello\".Substring(1, 2)", "el", TestName = "Overload_Substring_TwoArgs")]
    [TestCase("\"hello\".IndexOf('l')", 2, TestName = "Overload_IndexOf_Char")]
    [TestCase("\"hello\".IndexOf(\"ll\")", 2, TestName = "Overload_IndexOf_String")]
    public async Task MethodOverload_StringMethods(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region String Comparison and Null Handling

    [TestCase("\"\" == \"\"", true, TestName = "StringEquality_EmptyStrings")]
    [TestCase("\"a\" == \"a\"", true, TestName = "StringEquality_Same")]
    [TestCase("\"a\" == \"b\"", false, TestName = "StringEquality_Different")]
    public async Task StringEquality(string expr, bool expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public void StringEquality_WithNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", null);

        Assert.That(engine.Evaluate("s == null"), Is.True);
        Assert.That(engine.Evaluate("s != null"), Is.False);
        Assert.That(engine.Evaluate("\"hello\" == null"), Is.False);
    }

    #endregion
}
