namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase02Plan01VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — addition
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Add_Integers() => Assert.That(Eval("3 + 4"), Is.EqualTo(7));

    [Test]
    public void Add_Doubles() => Assert.That(Eval("1.5 + 2.5"), Is.EqualTo(4.0));

    [Test]
    public void Add_IntAndLong() => Assert.That(Eval("1 + 2L"), Is.EqualTo(3L));

    [Test]
    public void StringConcat() => Assert.That(Eval(@"""hello"" + "" world"""), Is.EqualTo("hello world"));

    [Test]
    public void StringConcat_WithInt() => Assert.That(Eval(@"""value: "" + 42"), Is.EqualTo("value: 42"));

    [Test]
    public void StringConcat_NullRight() => Assert.That(Eval(@"""hello"" + null"), Is.EqualTo("hello"));

    [Test]
    public void StringConcat_NullLeft() => Assert.That(Eval(@"null + ""world"""), Is.EqualTo("world"));

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — subtraction, multiplication
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Subtract() => Assert.That(Eval("10 - 3"), Is.EqualTo(7));

    [Test]
    public void Subtract_Doubles() => Assert.That(Eval("5.5 - 2.0"), Is.EqualTo(3.5));

    [Test]
    public void Multiply() => Assert.That(Eval("4 * 5"), Is.EqualTo(20));

    [Test]
    public void Multiply_Doubles() => Assert.That(Eval("2.5 * 4.0"), Is.EqualTo(10.0));

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — division
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IntegerDivision_Truncates() => Assert.That(Eval("10 / 3"), Is.EqualTo(3));

    [Test]
    public void IntegerDivision_NegativeTruncates() => Assert.That(Eval("-7 / 2"), Is.EqualTo(-3));

    [Test]
    public void IntegerDivision_ByZero_Throws()
        => Assert.Throws<DivideByZeroException>(() => Eval("1 / 0"));

    [Test]
    public void FloatingDivision()
        => Assert.That(Eval("10.0 / 3.0"), Is.EqualTo(10.0 / 3.0));

    [Test]
    public void FloatingDivision_ByZero_Infinity()
        => Assert.That(Eval("1.0 / 0.0"), Is.EqualTo(double.PositiveInfinity));

    [Test]
    public void FloatingDivision_NegByZero_NegInfinity()
        => Assert.That(Eval("-1.0 / 0.0"), Is.EqualTo(double.NegativeInfinity));

    [Test]
    public void FloatingDivision_ZeroByZero_NaN()
        => Assert.That(Eval("0.0 / 0.0"), Is.NaN);

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — remainder
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Remainder() => Assert.That(Eval("10 % 3"), Is.EqualTo(1));

    [Test]
    public void Remainder_Negative() => Assert.That(Eval("-10 % 3"), Is.EqualTo(-1));

    [Test]
    public void Remainder_Doubles() => Assert.That(Eval("10.5 % 3.0"), Is.EqualTo(1.5));

    [Test]
    public void Remainder_ByZero_Throws()
        => Assert.Throws<DivideByZeroException>(() => Eval("1 % 0"));

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — increment/decrement
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void PrefixIncrement()
        => Assert.That(Eval("{ var x = 5; return ++x; }"), Is.EqualTo(6));

    [Test]
    public void PrefixDecrement()
        => Assert.That(Eval("{ var x = 5; return --x; }"), Is.EqualTo(4));

    [Test]
    public void PostfixIncrement_ReturnsOriginal()
        => Assert.That(Eval("{ var x = 5; return x++; }"), Is.EqualTo(5));

    [Test]
    public void PostfixIncrement_MutatesVariable()
        => Assert.That(Eval("{ var x = 5; x++; return x; }"), Is.EqualTo(6));

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — numeric promotion
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ByteAddition_PromotesToInt()
        => Assert.That(Eval("(byte)100 + (byte)200"), Is.EqualTo(300));

    [Test]
    public void IntPlusLong_PromotesToLong()
        => Assert.That(Eval("1 + 2L"), Is.EqualTo(3L));

    [Test]
    public void IntPlusDouble_PromotesToDouble()
        => Assert.That(Eval("1 + 1.5"), Is.EqualTo(2.5));

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic — checked/unchecked overflow
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Unchecked_Overflow_Wraps()
        => Assert.That(Eval("unchecked(int.MaxValue + 1)"), Is.EqualTo(int.MinValue));

    [Test]
    public void Checked_Overflow_Throws()
        => Assert.Throws<OverflowException>(() => Eval("checked(int.MaxValue + 1)"));

    // ═══════════════════════════════════════════════════════════════
    // Comparison — basic
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LessThan() => Assert.That(Eval("3 < 5"), Is.EqualTo(true));

    [Test]
    public void GreaterThan() => Assert.That(Eval("5 > 3"), Is.EqualTo(true));

    [Test]
    public void LessThanOrEqual() => Assert.That(Eval("3 <= 3"), Is.EqualTo(true));

    [Test]
    public void GreaterThanOrEqual_False() => Assert.That(Eval("5 >= 10"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Comparison — numeric promotion
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Comparison_IntAndLong() => Assert.That(Eval("1 < 2L"), Is.EqualTo(true));

    [Test]
    public void Comparison_IntAndDouble() => Assert.That(Eval("1 < 1.5"), Is.EqualTo(true));

    [Test]
    public void Comparison_LongAndDouble() => Assert.That(Eval("100L > 50.0"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Comparison — char
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Char_LessThan() => Assert.That(Eval("'a' < 'z'"), Is.EqualTo(true));

    [Test]
    public void Char_UpperLessThanLower() => Assert.That(Eval("'A' < 'a'"), Is.EqualTo(true));

    [Test]
    public void Char_GreaterThan() => Assert.That(Eval("'B' > 'A'"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Comparison — nullable lifted
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Nullable_LessThan_ReturnsFalse()
        => Assert.That(Eval("(int?)null < 5"), Is.EqualTo(false));

    [Test]
    public void Nullable_GreaterThan_ReturnsFalse()
        => Assert.That(Eval("(int?)null > 5"), Is.EqualTo(false));

    [Test]
    public void Nullable_LessOrEqual_ReturnsFalse()
        => Assert.That(Eval("(int?)null <= 5"), Is.EqualTo(false));

    [Test]
    public void Nullable_GreaterOrEqual_ReturnsFalse()
        => Assert.That(Eval("(int?)null >= 5"), Is.EqualTo(false));

    [Test]
    public void Nullable_NullLessThanNull_ReturnsFalse()
        => Assert.That(Eval("(int?)null < (int?)null"), Is.EqualTo(false));

    [Test]
    public void Nullable_NullLessOrEqualNull_ReturnsFalse()
        => Assert.That(Eval("(int?)null <= (int?)null"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Comparison — NaN
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NaN_LessThan_False()
        => Assert.That(Eval("double.NaN < 0.0"), Is.EqualTo(false));

    [Test]
    public void NaN_GreaterThan_False()
        => Assert.That(Eval("double.NaN > 0.0"), Is.EqualTo(false));

    [Test]
    public void NaN_LessOrEqual_False()
        => Assert.That(Eval("double.NaN <= 0.0"), Is.EqualTo(false));

    [Test]
    public void NaN_GreaterOrEqual_False()
        => Assert.That(Eval("double.NaN >= 0.0"), Is.EqualTo(false));

    [Test]
    public void NaN_LessThanNaN_False()
        => Assert.That(Eval("double.NaN < double.NaN"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Equality — value equality
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IntEquality() => Assert.That(Eval("1 == 1"), Is.EqualTo(true));

    [Test]
    public void IntInequality() => Assert.That(Eval("1 != 2"), Is.EqualTo(true));

    [Test]
    public void DoubleEquality() => Assert.That(Eval("3.14 == 3.14"), Is.EqualTo(true));

    [Test]
    public void BoolEquality() => Assert.That(Eval("true == true"), Is.EqualTo(true));

    [Test]
    public void BoolInequality() => Assert.That(Eval("true != false"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Equality — string
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StringEquality()
        => Assert.That(Eval(@"""abc"" == ""abc"""), Is.EqualTo(true));

    [Test]
    public void StringInequality()
        => Assert.That(Eval(@"""abc"" != ""def"""), Is.EqualTo(true));

    [Test]
    public void StringEquality_CaseSensitive()
        => Assert.That(Eval(@"""hello"" == ""HELLO"""), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Equality — char
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CharEquality() => Assert.That(Eval("'a' == 'a'"), Is.EqualTo(true));

    [Test]
    public void CharInequality() => Assert.That(Eval("'A' != 'a'"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Equality — nullable
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Nullable_NullEqualsNull()
        => Assert.That(Eval("(int?)null == null"), Is.EqualTo(true));

    [Test]
    public void Nullable_NullNotEqualsNull_False()
        => Assert.That(Eval("(int?)null != null"), Is.EqualTo(false));

    [Test]
    public void Nullable_NullEqualsValue_False()
        => Assert.That(Eval("(int?)null == 5"), Is.EqualTo(false));

    [Test]
    public void Nullable_ValueEqualsValue()
        => Assert.That(Eval("(int?)1 == 1"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Equality — NaN
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NaN_EqualsNaN_False()
        => Assert.That(Eval("double.NaN == double.NaN"), Is.EqualTo(false));

    [Test]
    public void NaN_NotEqualsNaN_True()
        => Assert.That(Eval("double.NaN != double.NaN"), Is.EqualTo(true));

    [Test]
    public void NaN_EqualsZero_False()
        => Assert.That(Eval("double.NaN == 0.0"), Is.EqualTo(false));

    [Test]
    public void NaN_NotEqualsZero_True()
        => Assert.That(Eval("double.NaN != 0.0"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Equality — tuple
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TupleEquality()
        => Assert.That(Eval("(1, 2) == (1, 2)"), Is.EqualTo(true));

    [Test]
    public void TupleInequality()
        => Assert.That(Eval("(1, 2) != (1, 3)"), Is.EqualTo(true));

    [Test]
    public void TupleEquality_MixedTypes()
        => Assert.That(Eval(@"(""a"", 1) == (""a"", 1)"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Equality — enum
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void EnumEquality()
        => Assert.That(Eval("System.DayOfWeek.Monday == System.DayOfWeek.Monday"), Is.EqualTo(true));

    [Test]
    public void EnumInequality()
        => Assert.That(Eval("System.DayOfWeek.Monday != System.DayOfWeek.Tuesday"), Is.EqualTo(true));
}
