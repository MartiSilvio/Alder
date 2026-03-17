namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase02Plan02VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(CsEvalOptions.Default with { CompilationMode = mode });

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Boolean logical — negation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LogicalNot_True() => Assert.That(Eval("!true"), Is.EqualTo(false));

    [Test]
    public void LogicalNot_False() => Assert.That(Eval("!false"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Boolean logical — non-short-circuit &, |, ^
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BoolAnd_TrueTrue() => Assert.That(Eval("true & true"), Is.EqualTo(true));

    [Test]
    public void BoolAnd_TrueFalse() => Assert.That(Eval("true & false"), Is.EqualTo(false));

    [Test]
    public void BoolAnd_FalseTrue() => Assert.That(Eval("false & true"), Is.EqualTo(false));

    [Test]
    public void BoolOr_TrueFalse() => Assert.That(Eval("true | false"), Is.EqualTo(true));

    [Test]
    public void BoolOr_FalseFalse() => Assert.That(Eval("false | false"), Is.EqualTo(false));

    [Test]
    public void BoolXor_TrueFalse() => Assert.That(Eval("true ^ false"), Is.EqualTo(true));

    [Test]
    public void BoolXor_TrueTrue() => Assert.That(Eval("true ^ true"), Is.EqualTo(false));

    [Test]
    public void BoolXor_FalseFalse() => Assert.That(Eval("false ^ false"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Boolean logical — short-circuit &&, ||
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ConditionalAnd_TrueTrue() => Assert.That(Eval("true && true"), Is.EqualTo(true));

    [Test]
    public void ConditionalAnd_TrueFalse() => Assert.That(Eval("true && false"), Is.EqualTo(false));

    [Test]
    public void ConditionalAnd_FalseTrue() => Assert.That(Eval("false && true"), Is.EqualTo(false));

    [Test]
    public void ConditionalOr_TrueFalse() => Assert.That(Eval("true || false"), Is.EqualTo(true));

    [Test]
    public void ConditionalOr_FalseTrue() => Assert.That(Eval("false || true"), Is.EqualTo(true));

    [Test]
    public void ConditionalOr_FalseFalse() => Assert.That(Eval("false || false"), Is.EqualTo(false));

    [Test]
    public void ShortCircuit_AndFalse()
        => Assert.That(Eval(@"{ var s = (string)null; return s != null && s == ""hello""; }"), Is.EqualTo(false));

    [Test]
    public void ShortCircuit_OrTrue()
        => Assert.That(Eval(@"{ var s = (string)null; return s == null || s == ""hello""; }"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Three-value bool? logic — &&
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullableBool_And_FalseNull()
        => Assert.That(Eval("(bool?)false && (bool?)null"), Is.EqualTo(false));

    [Test]
    public void NullableBool_And_TrueNull()
        => Assert.That(Eval("(bool?)true && (bool?)null"), Is.Null);

    [Test]
    public void NullableBool_And_NullNull()
        => Assert.That(Eval("(bool?)null && (bool?)null"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Three-value bool? logic — ||
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullableBool_Or_TrueNull()
        => Assert.That(Eval("(bool?)true || (bool?)null"), Is.EqualTo(true));

    [Test]
    public void NullableBool_Or_FalseNull()
        => Assert.That(Eval("(bool?)false || (bool?)null"), Is.Null);

    [Test]
    public void NullableBool_Or_NullNull()
        => Assert.That(Eval("(bool?)null || (bool?)null"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Three-value bool? logic — &, |, ^, !
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullableBool_BitwiseAnd_FalseNull()
        => Assert.That(Eval("(bool?)false & (bool?)null"), Is.EqualTo(false));

    [Test]
    public void NullableBool_BitwiseAnd_TrueTrue()
        => Assert.That(Eval("(bool?)true & (bool?)true"), Is.EqualTo(true));

    [Test]
    public void NullableBool_BitwiseOr_TrueNull()
        => Assert.That(Eval("(bool?)true | (bool?)null"), Is.EqualTo(true));

    [Test]
    public void NullableBool_BitwiseOr_FalseNull()
        => Assert.That(Eval("(bool?)false | (bool?)null"), Is.Null);

    [Test]
    public void NullableBool_Xor_TrueNull()
        => Assert.That(Eval("(bool?)true ^ (bool?)null"), Is.Null);

    [Test]
    public void NullableBool_Xor_TrueFalse()
        => Assert.That(Eval("(bool?)true ^ (bool?)false"), Is.EqualTo(true));

    [Test]
    public void NullableBool_Not_True()
        => Assert.That(Eval("!(bool?)true"), Is.EqualTo(false));

    [Test]
    public void NullableBool_Not_Null()
        => Assert.That(Eval("!(bool?)null"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Bitwise — AND, OR, XOR, complement
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BitwiseAnd_HexValues() => Assert.That(Eval("0xFF & 0x0F"), Is.EqualTo(15));

    [Test]
    public void BitwiseAnd_Binary() => Assert.That(Eval("0b1100 & 0b1010"), Is.EqualTo(8));

    [Test]
    public void BitwiseOr_HexValues() => Assert.That(Eval("0xFF | 0x0F"), Is.EqualTo(255));

    [Test]
    public void BitwiseOr_Binary() => Assert.That(Eval("0b1100 | 0b1010"), Is.EqualTo(14));

    [Test]
    public void BitwiseXor_Binary() => Assert.That(Eval("0b1100 ^ 0b1010"), Is.EqualTo(6));

    [Test]
    public void BitwiseXor_Same() => Assert.That(Eval("0xFF ^ 0xFF"), Is.EqualTo(0));

    [Test]
    public void BitwiseNot_Zero() => Assert.That(Eval("~0"), Is.EqualTo(-1));

    [Test]
    public void BitwiseNot_NegOne() => Assert.That(Eval("~-1"), Is.EqualTo(0));

    // ═══════════════════════════════════════════════════════════════
    // Shift — left, right, unsigned right
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void LeftShift() => Assert.That(Eval("1 << 3"), Is.EqualTo(8));

    [Test]
    public void LeftShift_Hex() => Assert.That(Eval("0xFF << 4"), Is.EqualTo(4080));

    [Test]
    public void RightShift() => Assert.That(Eval("8 >> 2"), Is.EqualTo(2));

    [Test]
    public void RightShift_Negative() => Assert.That(Eval("-8 >> 2"), Is.EqualTo(-2));

    [Test]
    public void RightShift_Unsigned() => Assert.That(Eval("0xFFu >> 4"), Is.EqualTo(15u));

    [Test]
    public void UnsignedRightShift() => Assert.That(Eval("-1 >>> 31"), Is.EqualTo(1));

    [Test]
    public void UnsignedRightShift_NegEight() => Assert.That(Eval("-8 >>> 2"), Is.EqualTo(1073741822));

    // ═══════════════════════════════════════════════════════════════
    // Shift — count masking
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ShiftCountMasking_Int() => Assert.That(Eval("1 << 33"), Is.EqualTo(2));

    [Test]
    public void ShiftCountMasking_Long() => Assert.That(Eval("1L << 65"), Is.EqualTo(2L));

    // ═══════════════════════════════════════════════════════════════
    // Bitwise — numeric promotion
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BitwiseAnd_BytePromotion() => Assert.That(Eval("(byte)0xFF & (byte)0x0F"), Is.EqualTo(15));

    [Test]
    public void BitwiseOr_ByteAndInt() => Assert.That(Eval("(byte)0x0F | 0x100"), Is.EqualTo(271));

    // ═══════════════════════════════════════════════════════════════
    // Bitwise — enum
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void EnumBitwiseOr()
        => Assert.That(Eval("System.IO.FileAccess.Read | System.IO.FileAccess.Write"),
            Is.EqualTo(System.IO.FileAccess.ReadWrite));

    [Test]
    public void EnumBitwiseAnd()
        => Assert.That(Eval("System.IO.FileAccess.ReadWrite & System.IO.FileAccess.Read"),
            Is.EqualTo(System.IO.FileAccess.Read));

    // ═══════════════════════════════════════════════════════════════
    // Bitwise — lifted nullable
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullableBitwise_NullAnd() => Assert.That(Eval("(int?)null & 0xFF"), Is.Null);

    [Test]
    public void NullableBitwise_ValueOr() => Assert.That(Eval("(int?)5 | (int?)3"), Is.EqualTo(7));

    // ═══════════════════════════════════════════════════════════════
    // Assignment — simple
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SimpleAssignment() => Assert.That(Eval("{ var x = 5; x = 10; return x; }"), Is.EqualTo(10));

    // ═══════════════════════════════════════════════════════════════
    // Assignment — compound arithmetic
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CompoundAdd() => Assert.That(Eval("{ var x = 10; x += 5; return x; }"), Is.EqualTo(15));

    [Test]
    public void CompoundSubtract() => Assert.That(Eval("{ var x = 10; x -= 3; return x; }"), Is.EqualTo(7));

    [Test]
    public void CompoundMultiply() => Assert.That(Eval("{ var x = 4; x *= 3; return x; }"), Is.EqualTo(12));

    [Test]
    public void CompoundDivide() => Assert.That(Eval("{ var x = 15; x /= 4; return x; }"), Is.EqualTo(3));

    [Test]
    public void CompoundRemainder() => Assert.That(Eval("{ var x = 17; x %= 5; return x; }"), Is.EqualTo(2));

    // ═══════════════════════════════════════════════════════════════
    // Assignment — compound bitwise
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CompoundBitwiseAnd() => Assert.That(Eval("{ var x = 0xFF; x &= 0x0F; return x; }"), Is.EqualTo(15));

    [Test]
    public void CompoundBitwiseOr() => Assert.That(Eval("{ var x = 0x0F; x |= 0xF0; return x; }"), Is.EqualTo(255));

    [Test]
    public void CompoundBitwiseXor() => Assert.That(Eval("{ var x = 0xFF; x ^= 0x0F; return x; }"), Is.EqualTo(240));

    // ═══════════════════════════════════════════════════════════════
    // Assignment — compound shift
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CompoundLeftShift() => Assert.That(Eval("{ var x = 1; x <<= 3; return x; }"), Is.EqualTo(8));

    [Test]
    public void CompoundRightShift() => Assert.That(Eval("{ var x = 16; x >>= 2; return x; }"), Is.EqualTo(4));

    [Test]
    public void CompoundUnsignedRightShift() => Assert.That(Eval("{ var x = -1; x >>>= 31; return x; }"), Is.EqualTo(1));

    // ═══════════════════════════════════════════════════════════════
    // Null-coalescing — ??
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullCoalesce_NullReturnsRight() => Assert.That(Eval("(int?)null ?? 42"), Is.EqualTo(42));

    [Test]
    public void NullCoalesce_NonNullReturnsLeft() => Assert.That(Eval("(int?)5 ?? 42"), Is.EqualTo(5));

    [Test]
    public void NullCoalesce_StringNull() => Assert.That(Eval(@"(string)null ?? ""default"""), Is.EqualTo("default"));

    [Test]
    public void NullCoalesce_StringNonNull() => Assert.That(Eval(@"""hello"" ?? ""default"""), Is.EqualTo("hello"));

    [Test]
    public void NullCoalesce_Chained()
        => Assert.That(Eval(@"(string)null ?? (string)null ?? ""fallback"""), Is.EqualTo("fallback"));

    // ═══════════════════════════════════════════════════════════════
    // Null-coalescing assignment — ??=
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullCoalesceAssign_Null()
        => Assert.That(Eval("{ int? x = null; x ??= 42; return x; }"), Is.EqualTo(42));

    [Test]
    public void NullCoalesceAssign_NonNull()
        => Assert.That(Eval("{ int? x = 10; x ??= 42; return x; }"), Is.EqualTo(10));

    // ═══════════════════════════════════════════════════════════════
    // Null-conditional — ?.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullConditional_NonNull()
        => Assert.That(Eval(@"""hello""?.Length"), Is.EqualTo(5));

    [Test]
    public void NullConditional_Null()
        => Assert.That(Eval(@"((string)null)?.Length"), Is.Null);

    [Test]
    public void NullConditional_Chain()
        => Assert.That(Eval(@"""hello""?.ToUpper()?.Length"), Is.EqualTo(5));

    [Test]
    public void NullConditional_ChainNull()
        => Assert.That(Eval(@"((string)null)?.ToUpper()?.Length"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Null-conditional element access — ?[]
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NullConditionalIndex_NonNull()
        => Assert.That(Eval("{ var arr = new int[] { 10, 20, 30 }; return arr?[1]; }"), Is.EqualTo(20));

    [Test]
    public void NullConditionalIndex_Null()
        => Assert.That(Eval("{ var arr = (int[])null; return arr?[0]; }"), Is.Null);
}
