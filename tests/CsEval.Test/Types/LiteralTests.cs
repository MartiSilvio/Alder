namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiteralTests(CompilationMode mode)
{
    [TestCase("42", 42, TestName = "Integer")]
    [TestCase("3.14", 3.14, TestName = "Double")]
    [TestCase("\"hello\"", "hello", TestName = "String")]
    [TestCase("true", true, TestName = "True")]
    [TestCase("false", false, TestName = "False")]
    [TestCase("0xFF", 255, TestName = "HexLiteral")]
    [TestCase("0x1A", 26, TestName = "HexLiteral2")]
    [TestCase("0b1010", 10, TestName = "BinaryLiteral")]
    [TestCase("0b11111111", 255, TestName = "BinaryLiteral2")]
    public async Task Eval_Literal(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [Test]
    public async Task Eval_Null_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "null";
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.Null);
        Assert.That(result, Is.EqualTo(csharpResult));
    }

    [TestCase(@"""\0""", "\0", TestName = "EscapeNull")]
    [TestCase(@"""\a""", "\a", TestName = "EscapeAlert")]
    [TestCase(@"""\b""", "\b", TestName = "EscapeBackspace")]
    [TestCase(@"""\f""", "\f", TestName = "EscapeFormFeed")]
    [TestCase(@"""\v""", "\v", TestName = "EscapeVerticalTab")]
    public async Task Eval_EscapeSequence_MatchesCSharp(string expr, string expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("0xFF + 1", 256, TestName = "HexArithmetic")]
    [TestCase("0b1010 * 2", 20, TestName = "BinaryArithmetic")]
    [TestCase("0xFF == 255", true, TestName = "HexComparison")]
    [TestCase("0b1010 == 10", true, TestName = "BinaryComparison")]
    public async Task Eval_HexBinaryArithmetic_MatchesCSharp(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("'a'", 'a', TestName = "CharLiteralA")]
    [TestCase("'Z'", 'Z', TestName = "CharLiteralZ")]
    [TestCase("'0'", '0', TestName = "CharDigit")]
    [TestCase(@"'\n'", '\n', TestName = "CharEscapeNewline")]
    [TestCase(@"'\t'", '\t', TestName = "CharEscapeTab")]
    public async Task Eval_CharLiteral_MatchesCSharp(string expr, char expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(char)), $"Type should be char for: {expr}");
    }

    [TestCase("'a' == 'a'", true, TestName = "CharComparison")]
    [TestCase("'a' < 'b'", true, TestName = "CharLessThan")]
    [TestCase("'a' != 'b'", true, TestName = "CharNotEqual")]
    public async Task Eval_CharOperations_MatchesCSharp(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    // Digit Separator Tests (C# 7.0+)

    [TestCase("1_000_000", 1000000, TestName = "DigitSeparator_Millions")]
    [TestCase("1_2_3", 123, TestName = "DigitSeparator_Arbitrary")]
    [TestCase("100_000L", 100000L, TestName = "DigitSeparator_Long")]
    [TestCase("1_000u", 1000u, TestName = "DigitSeparator_UInt")]
    [TestCase("1_000_000_000_000UL", 1000000000000UL, TestName = "DigitSeparator_ULong")]
    [TestCase("3.14_159", 3.14159, TestName = "DigitSeparator_Double")]
    [TestCase("1_000.5", 1000.5, TestName = "DigitSeparator_DoubleBeforeDecimal")]
    [TestCase("1_000.5_5", 1000.55, TestName = "DigitSeparator_DoubleBothSides")]
    [TestCase("3.14_159f", 3.14159f, TestName = "DigitSeparator_Float")]
    [TestCase("1_000.50m", 1000.50, TestName = "DigitSeparator_Decimal")]
    public async Task Eval_DigitSeparator_Decimal(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [TestCase("0xFF_FF", 0xFFFF, TestName = "DigitSeparator_Hex")]
    [TestCase("0x00_FF_00_FF", 0x00FF00FF, TestName = "DigitSeparator_HexBytes")]
    [TestCase("0xAB_CD_EF", 0xABCDEF, TestName = "DigitSeparator_HexMixed")]
    [TestCase("0xFF_FF_FF_FF_FF_FF_FF_FFUL", 0xFFFFFFFFFFFFFFFFUL, TestName = "DigitSeparator_HexULong")]
    public async Task Eval_DigitSeparator_Hex(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [TestCase("0b1111_0000", 0b11110000, TestName = "DigitSeparator_Binary")]
    [TestCase("0b1010_1010_1010_1010", 0b1010101010101010, TestName = "DigitSeparator_BinaryPattern")]
    [TestCase("0b1111_1111L", 0b11111111L, TestName = "DigitSeparator_BinaryLong")]
    public async Task Eval_DigitSeparator_Binary(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    // Exponent Notation Tests

    [TestCase("1e10", 1e10, TestName = "Exponent_Positive")]
    [TestCase("1E10", 1E10, TestName = "Exponent_UpperCase")]
    [TestCase("1.5e3", 1.5e3, TestName = "Exponent_WithDecimal")]
    [TestCase("1.5E-3", 1.5E-3, TestName = "Exponent_Negative")]
    [TestCase("1e+5", 1e+5, TestName = "Exponent_ExplicitPositive")]
    [TestCase("2.5e0", 2.5e0, TestName = "Exponent_Zero")]
    [TestCase("6.022e23", 6.022e23, TestName = "Exponent_Avogadro")]
    [TestCase("1.6e-19", 1.6e-19, TestName = "Exponent_ElementaryCharge")]
    [TestCase("3.14159e2", 3.14159e2, TestName = "Exponent_Pi100")]
    public async Task Eval_Exponent_Double(string expr, double expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(double)), $"Type should be double for: {expr}");
    }

    [TestCase("1e5f", 1e5f, TestName = "Exponent_Float")]
    [TestCase("1.5e-2f", 1.5e-2f, TestName = "Exponent_FloatNegative")]
    public async Task Eval_Exponent_Float(string expr, float expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(float)), $"Type should be float for: {expr}");
    }

    [TestCase("1e5m", TestName = "Exponent_Decimal")]
    [TestCase("1.5e-2m", TestName = "Exponent_DecimalNegative")]
    public async Task Eval_Exponent_Decimal(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(csharpResult), $"Value mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(decimal)), $"Type should be decimal for: {expr}");
    }

    // Combined Digit Separators and Exponents

    [TestCase("1_000e3", 1000e3, TestName = "DigitSeparator_WithExponent")]
    [TestCase("1.234_567e10", 1.234567e10, TestName = "DigitSeparator_DecimalWithExponent")]
    [TestCase("6.022_140_76e23", 6.02214076e23, TestName = "DigitSeparator_Avogadro")]
    public async Task Eval_DigitSeparator_WithExponent(string expr, double expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }
}
