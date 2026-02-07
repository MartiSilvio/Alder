// ECMA-334 Spec Example Tests
// Executable test cases derived directly from ECMA-334 7th Edition (December 2023) examples
// and normative text. Each test references the specific ECMA-334 section from which it is derived.
// All tests validate Roslyn parity via TestHelpers.RunCSharpParityTestAsync.

using CsEval.TestData.Data;

namespace CsEval.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class EcmaSpecExampleTests(CompilationMode mode)
{
    #region ECMA-334 §12.4.7.3 - Binary Numeric Promotions

    // "For the operation b * s, where b is a byte and s is a short, overload resolution
    //  selects operator *(int, int) as the best operator. Thus, the effect is that b and s
    //  are converted to int, and the type of the result is int."
    [Test]
    public async Task S12_4_7_3_ByteTimesShort_ProducesInt()
        => await TestHelpers.RunCSharpParityTestAsync("(byte)3 * (short)4", 12, mode);

    // "for the operation i * d, where i is an int and d is a double, overload resolution
    //  selects operator *(double, double) as the best operator."
    [Test]
    public async Task S12_4_7_3_IntTimesDouble_ProducesDouble()
        => await TestHelpers.RunCSharpParityTestAsync("5 * 2.5", 12.5, mode);

    // Rule 1: "If either operand is of type decimal, the other operand is converted to type decimal"
    [Test]
    public async Task S12_4_7_3_Rule1_DecimalPlusInt_ProducesDecimal()
        => await TestHelpers.RunCSharpParityTestAsync("1.0m + 2", 3.0m, mode);

    // Rule 2: "if either operand is of type double, the other operand is converted to type double"
    [Test]
    public async Task S12_4_7_3_Rule2_DoublePlusFloat_ProducesDouble()
        => await TestHelpers.RunCSharpParityTestAsync("1.0 + 2.0f", 3.0, mode);

    // Rule 3: "if either operand is of type float, the other operand is converted to type float"
    [Test]
    public async Task S12_4_7_3_Rule3_FloatPlusInt_ProducesFloat()
        => await TestHelpers.RunCSharpParityTestAsync("1.0f + 2", 3.0f, mode);

    // Rule 5: "if either operand is of type long, the other operand is converted to type long"
    [Test]
    public async Task S12_4_7_3_Rule5_LongPlusInt_ProducesLong()
        => await TestHelpers.RunCSharpParityTestAsync("1L + 2", 3L, mode);

    // Rule 6: "if either operand is of type uint and the other operand is of type sbyte, short,
    //  or int, both operands are converted to type long"
    [Test]
    public async Task S12_4_7_3_Rule6_UIntPlusShort_ProducesLong()
        => await TestHelpers.RunCSharpParityTestAsync("5U + (short)3", 8L, mode);

    // Rule 8: "Otherwise, both operands are converted to type int"
    // char operands promote to int per binary numeric promotion
    [Test]
    public async Task S12_4_7_3_Rule8_CharPlusChar_ProducesInt()
        => await TestHelpers.RunCSharpParityTestAsync("'A' + 'B'", 131, mode);

    #endregion

    #region ECMA-334 §12.4.7.2 - Unary Numeric Promotions

    // "Unary numeric promotion simply consists of converting operands of type sbyte, byte,
    //  short, ushort, or char to type int."
    [Test]
    public async Task S12_4_7_2_UnaryPlus_BytePromotesToInt()
        => await TestHelpers.RunCSharpParityTestAsync("+(byte)42", 42, mode);

    // "for the unary - operator, unary numeric promotion converts operands of type uint to type long"
    [Test]
    public async Task S12_4_7_2_UnaryMinus_UIntPromotesToLong()
        => await TestHelpers.RunCSharpParityTestAsync("-(uint)1", -1L, mode);

    #endregion

    #region ECMA-334 §12.9.5 - Bitwise Complement Operator

    // "The predefined bitwise complement operators are: int operator ~(int x); ..."
    // Per 12.4.7.2, byte is promoted to int before ~ is applied.
    [Test]
    public async Task S12_9_5_BitwiseComplement_BytePromotedToInt()
        => await TestHelpers.RunCSharpParityTestAsync("~(byte)5", -6, mode);

    // ~ on int: bitwise complement
    [Test]
    public async Task S12_9_5_BitwiseComplement_Int()
        => await TestHelpers.RunCSharpParityTestAsync("~0", -1, mode);

    #endregion

    #region ECMA-334 §12.9.3 - Unary Minus Operator

    // "If the operand of the negation operator is of type uint, it is converted to type long,
    //  and the type of the result is long."
    [Test]
    public async Task S12_9_3_NegateUInt_ProducesLong()
        => await TestHelpers.RunCSharpParityTestAsync("-(uint)5", -5L, mode);

    // "The result is computed by subtracting X from zero."
    // Negating a negative int produces the positive value.
    [Test]
    public async Task S12_9_3_NegateNegativeInt()
        => await TestHelpers.RunCSharpParityTestAsync("-(-5)", 5, mode);

    #endregion

    #region ECMA-334 §12.9.4 - Logical Negation Operator

    // "If the operand is true, the result is false. If the operand is false, the result is true."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.LogicalNegationCases))]
    public async Task S12_9_4_LogicalNegation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Operators

    // "The lifted operator produces a null value if the operand is null."
    // Lifted unary ! on bool?
    [Test]
    public Task S12_4_8_LiftedLogicalNot_NullProducesNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ bool? x = null; return !x; }");
        Assert.That(result, Is.Null, "!(bool?)null should produce null per section 12.4.8");
        return Task.CompletedTask;
    }

    // "The lifted operator produces a null value if one or both operands are null"
    // (for binary operators other than bool? & and |)
    [Test]
    public Task S12_4_8_LiftedBinaryAdd_NullProducesNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ int? a = 5; int? b = null; return a + b; }");
        Assert.That(result, Is.Null, "int? + null should produce null per section 12.4.8");
        return Task.CompletedTask;
    }

    // Lifted equality: "The lifted operator considers two null values equal"
    [Test]
    public Task S12_4_8_LiftedEquality_NullEqualsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ int? a = null; int? b = null; return a == b; }");
        Assert.That(result, Is.EqualTo(true), "null == null should be true per section 12.4.8");
        return Task.CompletedTask;
    }

    // Lifted relational: "the lifted operator produces the value false if one or both operands are null"
    [Test]
    public Task S12_4_8_LiftedRelational_NullLessThan_False()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ int? a = null; int? b = 5; return a < b; }");
        Assert.That(result, Is.EqualTo(false), "null < 5 should be false per section 12.4.8");
        return Task.CompletedTask;
    }

    #endregion

    #region ECMA-334 §12.13.5 - Nullable Boolean & and | Operators

    // "The nullable Boolean type bool? can represent three values, true, false, and null."
    // Truth table from section 12.13.5:
    //   x       y       x & y   x | y
    //   true    true    true    true
    //   true    false   false   true
    //   true    null    null    true
    //   false   true    false   true
    //   false   false   false   false
    //   false   null    false   null
    //   null    true    null    true
    //   null    false   false   null
    //   null    null    null    null

    [Test]
    public Task S12_13_5_NullableBoolAnd_TruthTable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        // true & true = true
        engine.SetVariable("x", (bool?)true);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(true), "true & true should be true");

        // true & false = false
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(false), "true & false should be false");

        // true & null = null
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x & y"), Is.Null, "true & null should be null");

        // false & true = false
        engine.SetVariable("x", (bool?)false);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(false), "false & true should be false");

        // false & false = false
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(false), "false & false should be false");

        // false & null = false (KEY: not null, per section 12.13.5)
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(false), "false & null should be false per section 12.13.5");

        // null & true = null
        engine.SetVariable("x", (bool?)null);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x & y"), Is.Null, "null & true should be null");

        // null & false = false
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x & y"), Is.EqualTo(false), "null & false should be false per section 12.13.5");

        // null & null = null
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x & y"), Is.Null, "null & null should be null");

        return Task.CompletedTask;
    }

    [Test]
    public Task S12_13_5_NullableBoolOr_TruthTable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        // true | true = true
        engine.SetVariable("x", (bool?)true);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(true), "true | true should be true");

        // true | false = true
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(true), "true | false should be true");

        // true | null = true (KEY: not null, per section 12.13.5)
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(true), "true | null should be true per section 12.13.5");

        // false | true = true
        engine.SetVariable("x", (bool?)false);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(true), "false | true should be true");

        // false | false = false
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(false), "false | false should be false");

        // false | null = null
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x | y"), Is.Null, "false | null should be null");

        // null | true = true (KEY: not null, per section 12.13.5)
        engine.SetVariable("x", (bool?)null);
        engine.SetVariable("y", (bool?)true);
        Assert.That(engine.Evaluate("x | y"), Is.EqualTo(true), "null | true should be true per section 12.13.5");

        // null | false = null
        engine.SetVariable("y", (bool?)false);
        Assert.That(engine.Evaluate("x | y"), Is.Null, "null | false should be null");

        // null | null = null
        engine.SetVariable("y", (bool?)null);
        Assert.That(engine.Evaluate("x | y"), Is.Null, "null | null should be null");

        return Task.CompletedTask;
    }

    #endregion

    #region ECMA-334 §12.13.4 - Boolean Logical Operators

    // "The result of x & y is true if both x and y are true. Otherwise, the result is false."
    // "The result of x | y is true if either x or y is true. Otherwise, the result is false."
    // "The result of x ^ y is true if x is true and y is false, or x is false and y is true."
    // "When the operands are of type bool, the ^ operator computes the same result as the != operator."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.BooleanLogicalCases))]
    public async Task S12_13_4_BooleanLogicalOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.10.3 - Division Operator

    // "The division rounds the result towards zero."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.DivisionCases))]
    public async Task S12_10_3_IntegerDivision(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.10.4 - Remainder Operator

    // "The result of x % y is the value produced by x - (x / y) * y."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.RemainderCases))]
    public async Task S12_10_4_IntegerRemainder(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.10.5 - Addition Operator (String Concatenation)

    // "If an operand of string concatenation is null, an empty string is substituted."
    [Test]
    public Task S12_10_5_StringConcat_NullSubstitution()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", (string?)null);
        var result = engine.Evaluate("\"prefix\" + s + \"suffix\"");
        Assert.That(result, Is.EqualTo("prefixsuffix"), "null string operand should be substituted with empty string");
        return Task.CompletedTask;
    }

    // "any non-string operand is converted to its string representation"
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.StringConcatCases))]
    public async Task S12_10_5_StringConcatenation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.11 - Shift Operators

    // Shift count masking per section 12.11:
    // "When the type of x is int or uint, the shift count is given by the low-order five bits of count.
    //  In other words, the shift count is computed from count & 0x1F."
    // Arithmetic right shift: "the value of the most significant bit (the sign bit) of the operand
    //  is propagated to the high-order empty bit positions"
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.ShiftCases))]
    public async Task S12_11_ShiftOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Per section 12.4.7.2, small types are promoted to int for shift operators
    [Test]
    public async Task S12_11_ShortLeftShift_PromotedToInt()
        => await TestHelpers.RunCSharpParityTestAsync("(short)1 << 2", 4, mode);

    #endregion

    #region ECMA-334 §12.12.3 - Floating-Point Comparison

    // "If either operand is NaN, the result is false for all operators except !=,
    //  for which the result is true."
    // "Negative and positive zeros are considered equal."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.FloatingPointComparisonCases))]
    public async Task S12_12_3_FloatingPointComparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.12.5 - Boolean Equality Operators

    // "The result of == is true if both x and y are true or if both x and y are false."
    // "When the operands are of type bool, the != operator produces the same result as the ^ operator."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.BooleanEqualityCases))]
    public async Task S12_12_5_BooleanEquality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.14.2 - Boolean Conditional Logical Operators

    // "The operation x && y corresponds to the operation x & y, except that y is evaluated only if x is not false."
    // "The operation x || y corresponds to the operation x | y, except that y is evaluated only if x is not true."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.ConditionalLogicalCases))]
    public async Task S12_14_2_BooleanConditionalLogical(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.15 - Null Coalescing Operator

    // "if a is non-null, the result is a; otherwise, the result is b."
    // "The null coalescing operator is right-associative"
    // "An expression of the form a ?? b ?? c is evaluated as a ?? (b ?? c)"
    [Test]
    public Task S12_15_NullCoalescing_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", (string?)"hello");
        Assert.That(engine.Evaluate("a ?? \"default\""), Is.EqualTo("hello"));

        engine.SetVariable("a", (string?)null);
        Assert.That(engine.Evaluate("a ?? \"default\""), Is.EqualTo("default"));
        return Task.CompletedTask;
    }

    // Right-associativity: a ?? b ?? c == a ?? (b ?? c)
    [Test]
    public Task S12_15_NullCoalescing_RightAssociative()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", (string?)null);
        engine.SetVariable("b", (string?)null);
        engine.SetVariable("c", (string?)"found");
        Assert.That(engine.Evaluate("a ?? b ?? c"), Is.EqualTo("found"),
            "a ?? b ?? c should evaluate as a ?? (b ?? c)");
        return Task.CompletedTask;
    }

    #endregion

    #region ECMA-334 §12.18 - Conditional Operator

    // "A conditional expression of the form b ? x : y first evaluates the condition b.
    //  Then, if b is true, x is evaluated and becomes the result of the operation.
    //  Otherwise, y is evaluated and becomes the result of the operation."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.ConditionalOperatorCases))]
    public async Task S12_18_ConditionalOperator(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Right-associative: "a ? b : c ? d : e is evaluated as a ? b : (c ? d : e)"
    [Test]
    public async Task S12_18_Conditional_RightAssociative()
        => await TestHelpers.RunCSharpParityTestAsync("false ? 1 : true ? 2 : 3", 2, mode);

    #endregion

    #region ECMA-334 §10.2.3 - Implicit Numeric Conversions

    // "From char to ushort, int, uint, long, ulong, float, double, or decimal."
    // "There are no predefined implicit conversions to the char type, so values of the
    //  other integral types do not automatically convert to the char type."
    [Test]
    public async Task S10_2_3_CharToInt_ImplicitConversion()
        => await TestHelpers.RunCSharpParityTestAsync("'A' + 0", 65, mode);

    // "From int to long, float, double, or decimal."
    [Test]
    public async Task S10_2_3_IntToLong_ImplicitConversion()
        => await TestHelpers.RunCSharpParityTestAsync("42 + 0L", 42L, mode);

    // "From float to double."
    [Test]
    public async Task S10_2_3_FloatToDouble_ImplicitConversion()
        => await TestHelpers.RunCSharpParityTestAsync("1.0f + 0.0", 1.0, mode);

    #endregion

    #region ECMA-334 §10.3.2 / 12.9.7 - Explicit Numeric Conversions (Cast)

    // section 10.1: "The opposite conversion, from type long to type int, is explicit
    //  and so an explicit cast is required."
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.ExplicitConversionCases))]
    public async Task S10_3_2_ExplicitNumericConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Explicit cast: int to byte (narrowing)
    [Test]
    public async Task S10_3_2_IntToByte_ExplicitCast()
        => await TestHelpers.RunCSharpParityTestAsync("(byte)42", (byte)42, mode);

    #endregion

    #region ECMA-334 §10.3.7 - Unboxing Conversions

    // Unboxing requires exact type match: (long)(object)42 should fail because the boxed int
    // cannot be unboxed to long directly
    [Test]
    public void S10_3_7_Unboxing_RequiresExactType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("box", (object)42);
        Assert.Throws<InvalidCastException>(() => engine.Evaluate("(long)box"),
            "(long)(object)42 should throw InvalidCastException per section 10.3.7 unboxing rules");
    }

    #endregion

    #region ECMA-334 §12.8.2 - Literals

    // Verify literal types per section 12.8.2 and section 6.4.5.3
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.LiteralCases))]
    public async Task S12_8_2_Literals(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Decimal literal (separate test since TestCase attributes do not support decimal constants)
    [Test]
    public async Task S12_8_2_DecimalLiteral()
        => await TestHelpers.RunCSharpParityTestAsync("3.14m", 3.14m, mode);

    #endregion

    #region ECMA-334 §12.13.2 - Integer Logical Operators

    // "For the predefined integer logical operators: ... The & operator computes the
    //  bitwise logical AND of the two operands"
    [TestCaseSource(typeof(EcmaSpecExampleData), nameof(EcmaSpecExampleData.IntegerLogicalCases))]
    public async Task S12_13_2_IntegerLogicalOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
