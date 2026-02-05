namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class BitwiseTests(CompilationMode mode)
{
    // Bitwise AND
    [TestCase("5 & 3", 1, TestName = "And_5And3")]
    [TestCase("12 & 10", 8, TestName = "And_12And10")]
    [TestCase("255 & 0", 0, TestName = "And_WithZero")]
    [TestCase("15 & 15", 15, TestName = "And_SameValue")]
    // Bitwise OR
    [TestCase("5 | 3", 7, TestName = "Or_5Or3")]
    [TestCase("12 | 10", 14, TestName = "Or_12Or10")]
    [TestCase("255 | 0", 255, TestName = "Or_WithZero")]
    [TestCase("1 | 2 | 4", 7, TestName = "Or_Chained")]
    // Bitwise XOR
    [TestCase("5 ^ 3", 6, TestName = "Xor_5Xor3")]
    [TestCase("12 ^ 10", 6, TestName = "Xor_12Xor10")]
    [TestCase("42 ^ 42", 0, TestName = "Xor_SameValueIsZero")]
    [TestCase("15 ^ 255", 240, TestName = "Xor_15Xor255")]
    // Bitwise NOT
    [TestCase("~0", -1, TestName = "Not_Zero")]
    [TestCase("~1", -2, TestName = "Not_One")]
    [TestCase("~(-1)", 0, TestName = "Not_NegativeOne")]
    [TestCase("~~42", 42, TestName = "Not_DoubleNot")]
    // Left Shift
    [TestCase("1 << 1", 2, TestName = "LeftShift_By1")]
    [TestCase("1 << 2", 4, TestName = "LeftShift_By2")]
    [TestCase("1 << 3", 8, TestName = "LeftShift_By3")]
    [TestCase("5 << 2", 20, TestName = "LeftShift_5By2")]
    [TestCase("3 << 4", 48, TestName = "LeftShift_3By4")]
    [TestCase("42 << 0", 42, TestName = "LeftShift_ByZero")]
    [TestCase("0 << 10", 0, TestName = "LeftShift_ZeroValue")]
    // Right Shift
    [TestCase("8 >> 1", 4, TestName = "RightShift_By1")]
    [TestCase("8 >> 2", 2, TestName = "RightShift_By2")]
    [TestCase("8 >> 3", 1, TestName = "RightShift_By3")]
    [TestCase("20 >> 2", 5, TestName = "RightShift_20By2")]
    [TestCase("48 >> 4", 3, TestName = "RightShift_48By4")]
    [TestCase("42 >> 0", 42, TestName = "RightShift_ByZero")]
    // Precedence
    [TestCase("1 | 2 & 3", 3, TestName = "Precedence_AndBeforeOr")]
    [TestCase("1 << 2 < 5", true, TestName = "Precedence_ShiftBeforeComparison")]
    [TestCase("7 ^ 3 & 5", 6, TestName = "Precedence_AndBeforeXor")]
    [TestCase("1 | 2 ^ 3", 1, TestName = "Precedence_XorBeforeOr")]
    // Combined with arithmetic
    [TestCase("(2 + 3) & 7", 5, TestName = "Combined_ArithmeticAndBitwise")]
    [TestCase("10 | (3 * 2)", 14, TestName = "Combined_BitwiseAndMultiply")]
    // Combined with logical
    [TestCase("(5 & 1) == 1 && true", true, TestName = "Combined_BitwiseAndLogical")]
    // In ternary
    [TestCase("true ? 5 & 3 : 0", 1, TestName = "InTernary")]
    // Boolean AND (non-short-circuit)
    [TestCase("true & true", true, TestName = "BoolAnd_TrueTrue")]
    [TestCase("true & false", false, TestName = "BoolAnd_TrueFalse")]
    [TestCase("false & false", false, TestName = "BoolAnd_FalseFalse")]
    // Boolean OR (non-short-circuit)
    [TestCase("true | false", true, TestName = "BoolOr_TrueFalse")]
    [TestCase("false | true", true, TestName = "BoolOr_FalseTrue")]
    [TestCase("false | false", false, TestName = "BoolOr_FalseFalse")]
    // Boolean XOR
    [TestCase("true ^ true", false, TestName = "BoolXor_TrueTrue")]
    [TestCase("true ^ false", true, TestName = "BoolXor_TrueFalse")]
    [TestCase("false ^ false", false, TestName = "BoolXor_FalseFalse")]
    public async Task Eval_Bitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // ECMA-334 §12.4.2 - Shift vs Addition Precedence (addition/subtraction has higher precedence than shift)
    [TestCase("1 << 2 + 3", 32, TestName = "Precedence_ShiftVsAdd_AddFirst")]
    [TestCase("16 >> 1 + 1", 4, TestName = "Precedence_ShiftVsAdd_RightShift")]
    [TestCase("1 << 1 << 1", 4, TestName = "Precedence_ShiftLeftAssociativity")]
    // ECMA-334 §12.11 - Shift with multiplication (multiply has higher precedence)
    [TestCase("1 << 2 * 2", 16, TestName = "Precedence_ShiftVsMul_MulFirst")]
    [TestCase("8 >> 4 / 2", 2, TestName = "Precedence_ShiftVsDiv_DivFirst")]
    // ECMA-334 §12.13 - Bitwise AND/XOR/OR precedence chain
    [TestCase("7 & 3 ^ 1", 2, TestName = "Precedence_AndBeforeXor")]
    [TestCase("7 ^ 3 | 1", 5, TestName = "Precedence_XorBeforeOr")]
    [TestCase("7 & 3 ^ 1 | 8", 10, TestName = "Precedence_FullChain_AndXorOr")]
    // ECMA-334 §12.11 - Large shift counts (masked to type width)
    [TestCase("1 << 32", 1, TestName = "LeftShift_By32_Wraps")]
    [TestCase("1 << 33", 2, TestName = "LeftShift_By33_Wraps")]
    // ECMA-334 §12.11 - Negative shift count behavior
    [TestCase("16 >> -1", 0, TestName = "RightShift_NegativeCount")]
    public async Task Precedence_ShiftAndArithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("3.14 & 1", TestName = "And_DoubleWithInt")]
    [TestCase("3.14 | 1", TestName = "Or_DoubleWithInt")]
    [TestCase("3.14 ^ 1", TestName = "Xor_DoubleWithInt")]
    [TestCase("3.14 << 1", TestName = "LeftShift_Double")]
    [TestCase("1 << 3.14", TestName = "LeftShift_DoubleCount")]
    [TestCase("8 >> 1.5", TestName = "RightShift_DoubleCount")]
    [TestCase("~3.14", TestName = "Not_Double")]
    // ECMA-334 §12.4.2 - == has higher precedence than &, so this parses as 1 & (1 == 1) = 1 & true
    [TestCase("1 & 1 == 1", TestName = "Precedence_CompareBeforeBitwiseAnd_TypeMismatch")]
    public async Task Eval_Bitwise_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }
}
