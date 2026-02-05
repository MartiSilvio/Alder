namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CastTests(CompilationMode mode)
{
    [TestCase("(int)3.7", 3, TestName = "Cast_DoubleToInt")]
    [TestCase("(int)3.9", 3, TestName = "Cast_DoubleToInt_Truncates")]
    [TestCase("(double)5", 5.0, TestName = "Cast_IntToDouble")]
    [TestCase("(long)42", 42L, TestName = "Cast_IntToLong")]
    [TestCase("(int)100L", 100, TestName = "Cast_LongToInt")]
    [TestCase("(float)3.14", 3.14f, TestName = "Cast_DoubleToFloat")]
    [TestCase("(double)3.14f", (double)3.14f, TestName = "Cast_FloatToDouble")]
    [TestCase("(byte)255", (byte)255, TestName = "Cast_IntToByte")]
    [TestCase("(sbyte)127", (sbyte)127, TestName = "Cast_IntToSbyte")]
    [TestCase("(short)1000", (short)1000, TestName = "Cast_IntToShort")]
    [TestCase("(ushort)50000", (ushort)50000, TestName = "Cast_IntToUshort")]
    [TestCase("(uint)100", 100u, TestName = "Cast_IntToUint")]
    [TestCase("(ulong)100", 100ul, TestName = "Cast_IntToUlong")]
    [TestCase("(int)'A'", 65, TestName = "Cast_CharToInt")]
    [TestCase("(char)65", 'A', TestName = "Cast_IntToChar")]
    [TestCase("(char)90", 'Z', TestName = "Cast_IntToChar_Z")]
    [TestCase("(int)'9'", 57, TestName = "Cast_DigitCharToInt")]
    [TestCase("(int)(2.5 + 3.7)", 6, TestName = "Cast_ExpressionResult")]
    [TestCase("(int)(-3.9)", -3, TestName = "Cast_NegativeDouble")]
    [TestCase("(double)(10 / 3)", 3.0, TestName = "Cast_IntDivisionResult")]
    [TestCase("(int)(double)42L", 42, TestName = "Cast_ChainedCasts")]
    [TestCase("(int)3.7 + (int)4.2", 7, TestName = "Cast_InArithmetic")]
    [TestCase("(int)3.7m", 3, TestName = "Cast_DecimalToInt")]
    [TestCase("(int?)42", 42, TestName = "Cast_IntToNullableInt")]
    [TestCase("(decimal)42", 42, TestName = "Cast_IntToDecimal")]
    [TestCase("{ var x = 3.7; return (int)x; }", 3, TestName = "Cast_Variable")]
    [TestCase("{ var x = 3.7; return (int)x > 3 ? \"yes\" : \"no\"; }", "no", TestName = "Cast_InConditional")]
    public async Task Eval_Cast(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("(int?)null", null, TestName = "Cast_NullToNullableInt")]
    [TestCase("(long?)null", null, TestName = "Cast_NullToNullableLong")]
    [TestCase("(double?)null", null, TestName = "Cast_NullToNullableDouble")]
    public async Task Eval_Cast_ToNull(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ object x = null; return (int)x; }", TestName = "CastNullObjectToInt")]
    [TestCase("{ object x = \"hello\"; return (int)x; }", TestName = "CastStringObjectToInt")]
    public async Task Eval_Cast_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #region Unboxing Requires Exact Type Match

    // C# unboxing rule: when source static type is 'object', you can only unbox to the exact boxed type
    // (long)(object)42 fails because 42 is boxed as int, not long
    // CsEval tracks compile-time types to enforce this correctly

    [TestCase("{ object x = 42; return (long)x; }", TestName = "Unboxing_IntToLong")]
    [TestCase("{ object x = 42; return (double)x; }", TestName = "Unboxing_IntToDouble")]
    [TestCase("{ object x = true; return (int)x; }", TestName = "Unboxing_BoolToInt")]
    [TestCase("{ object x = 3.14; return (int)x; }", TestName = "Unboxing_DoubleToInt")]
    [TestCase("{ object x = 42L; return (int)x; }", TestName = "Unboxing_LongToInt")]
    [TestCase("{ object x = 3.14f; return (double)x; }", TestName = "Unboxing_FloatToDouble")]
    public async Task Eval_Cast_InvalidUnboxing_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<InvalidCastException>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    [TestCase("{ object x = 42; return (int)x; }", 42, TestName = "Unboxing_IntToInt")]
    [TestCase("{ object x = 42L; return (long)x; }", 42L, TestName = "Unboxing_LongToLong")]
    [TestCase("{ object x = 3.14; return (double)x; }", 3.14, TestName = "Unboxing_DoubleToDouble")]
    [TestCase("{ object x = true; return (bool)x; }", true, TestName = "Unboxing_BoolToBool")]
    [TestCase("{ object x = 'A'; return (char)x; }", 'A', TestName = "Unboxing_CharToChar")]
    public async Task Eval_Cast_ValidUnboxing(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int x = 42; return (long)x; }", 42L, TestName = "Conversion_IntToLong")]
    [TestCase("{ int x = 42; return (double)x; }", 42.0, TestName = "Conversion_IntToDouble")]
    [TestCase("{ long x = 42L; return (int)x; }", 42, TestName = "Conversion_LongToInt")]
    [TestCase("{ double x = 3.7; return (int)x; }", 3, TestName = "Conversion_DoubleToInt")]
    public async Task Eval_Cast_NumericConversion_NotUnboxing(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
