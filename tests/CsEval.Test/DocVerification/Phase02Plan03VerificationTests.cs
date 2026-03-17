namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase02Plan03VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private object? Eval(string expr) => Engine().Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════
    // Member access — property read
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StringLength() => Assert.That(Eval(@"""hello"".Length"), Is.EqualTo(5));

    [Test]
    public void StringEmpty() => Assert.That(Eval("string.Empty"), Is.EqualTo(""));

    [Test]
    public void IntMaxValue() => Assert.That(Eval("int.MaxValue"), Is.EqualTo(int.MaxValue));

    [Test]
    public void DoubleNaN() => Assert.That(Eval("double.NaN"), Is.NaN);

    // ═══════════════════════════════════════════════════════════════
    // Member access — element access
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StringIndex_First() => Assert.That(Eval(@"""hello""[0]"), Is.EqualTo('h'));

    [Test]
    public void StringIndex_Last() => Assert.That(Eval(@"""hello""[4]"), Is.EqualTo('o'));

    [Test]
    public void ArrayIndex() => Assert.That(Eval("new int[] { 10, 20, 30 }[1]"), Is.EqualTo(20));

    // ═══════════════════════════════════════════════════════════════
    // Member access — method invocation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StringToUpper() => Assert.That(Eval(@"""hello"".ToUpper()"), Is.EqualTo("HELLO"));

    [Test]
    public void StringContains() => Assert.That(Eval(@"""hello world"".Contains(""world"")"), Is.EqualTo(true));

    [Test]
    public void StringTrim() => Assert.That(Eval(@"""  hello  "".Trim()"), Is.EqualTo("hello"));

    [Test]
    public void MethodChaining() => Assert.That(Eval(@"""Hello World"".ToLower().Trim()"), Is.EqualTo("hello world"));

    // ═══════════════════════════════════════════════════════════════
    // Type testing — is
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Is_IntIsInt() => Assert.That(Eval("42 is int"), Is.EqualTo(true));

    [Test]
    public void Is_StringIsString() => Assert.That(Eval(@"""hello"" is string"), Is.EqualTo(true));

    [Test]
    public void Is_IntIsDouble_False() => Assert.That(Eval("42 is double"), Is.EqualTo(false));

    [Test]
    public void Is_StringIsObject() => Assert.That(Eval(@"""hello"" is object"), Is.EqualTo(true));

    [Test]
    public void Is_NullIsString_False() => Assert.That(Eval("null is string"), Is.EqualTo(false));

    // ═══════════════════════════════════════════════════════════════
    // Type testing — as
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void As_StringAsString() => Assert.That(Eval(@"(object)""hello"" as string"), Is.EqualTo("hello"));

    [Test]
    public void As_IntAsString_Null() => Assert.That(Eval("(object)42 as string"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Type testing — explicit cast
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Cast_IntToDouble() => Assert.That(Eval("(double)42"), Is.EqualTo(42.0));

    [Test]
    public void Cast_DoubleToInt() => Assert.That(Eval("(int)3.14"), Is.EqualTo(3));

    [Test]
    public void Cast_IntToLong() => Assert.That(Eval("(long)42"), Is.EqualTo(42L));

    [Test]
    public void Cast_ByteOverflow() => Assert.That(Eval("(byte)256"), Is.EqualTo((byte)0));

    [Test]
    public void Cast_Unboxing() => Assert.That(Eval("(int)(object)42"), Is.EqualTo(42));

    [Test]
    public void Cast_CheckedOverflow_Throws()
        => Assert.Throws<OverflowException>(() => Eval("checked((byte)256)"));

    [Test]
    public void Cast_UncheckedOverflow() => Assert.That(Eval("unchecked((byte)256)"), Is.EqualTo((byte)0));

    // ═══════════════════════════════════════════════════════════════
    // Type testing — typeof (returns Type object, not blocked)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Typeof_ReturnsType()
        => Assert.That(Eval("typeof(int)"), Is.EqualTo(typeof(int)));

    [Test]
    public void Typeof_Equality()
        => Assert.That(Eval("typeof(int) == typeof(int)"), Is.EqualTo(true));

    [Test]
    public void Typeof_Inequality()
        => Assert.That(Eval("typeof(int) != typeof(string)"), Is.EqualTo(true));

    // ═══════════════════════════════════════════════════════════════
    // Type testing — default
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Default_Int() => Assert.That(Eval("default(int)"), Is.EqualTo(0));

    [Test]
    public void Default_Bool() => Assert.That(Eval("default(bool)"), Is.EqualTo(false));

    [Test]
    public void Default_Double() => Assert.That(Eval("default(double)"), Is.EqualTo(0.0));

    [Test]
    public void Default_String() => Assert.That(Eval("default(string)"), Is.Null);

    // ═══════════════════════════════════════════════════════════════
    // Type testing — nameof
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Nameof_Variable()
        => Assert.That(Eval(@"{ var myVariable = 42; return nameof(myVariable); }"), Is.EqualTo("myVariable"));

    // ═══════════════════════════════════════════════════════════════
    // Type testing — sizeof
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Sizeof_Int() => Assert.That(Eval("sizeof(int)"), Is.EqualTo(4));

    [Test]
    public void Sizeof_Double() => Assert.That(Eval("sizeof(double)"), Is.EqualTo(8));

    [Test]
    public void Sizeof_Char() => Assert.That(Eval("sizeof(char)"), Is.EqualTo(2));

    [Test]
    public void Sizeof_Bool() => Assert.That(Eval("sizeof(bool)"), Is.EqualTo(1));

    // ═══════════════════════════════════════════════════════════════
    // Range and index — index from end
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IndexFromEnd_Last() => Assert.That(Eval(@"""hello""[^1]"), Is.EqualTo('o'));

    [Test]
    public void IndexFromEnd_SecondToLast() => Assert.That(Eval(@"""hello""[^2]"), Is.EqualTo('l'));

    // ═══════════════════════════════════════════════════════════════
    // Range and index — range operator
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Range_Substring() => Assert.That(Eval(@"""hello""[1..4]"), Is.EqualTo("ell"));

    [Test]
    public void Range_FromStart() => Assert.That(Eval(@"""hello""[0..3]"), Is.EqualTo("hel"));

    [Test]
    public void Range_WithIndexFromEnd() => Assert.That(Eval(@"""hello""[1..^1]"), Is.EqualTo("ell"));

    // ═══════════════════════════════════════════════════════════════
    // Conditional — ternary
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Conditional_True() => Assert.That(Eval("true ? 1 : 2"), Is.EqualTo(1));

    [Test]
    public void Conditional_False() => Assert.That(Eval("false ? 1 : 2"), Is.EqualTo(2));

    [Test]
    public void Conditional_Expression() => Assert.That(Eval(@"5 > 3 ? ""yes"" : ""no"""), Is.EqualTo("yes"));

    [Test]
    public void Conditional_ShortCircuit()
        => Assert.That(Eval("true ? 42 : 1 / 0"), Is.EqualTo(42));

    [Test]
    public void Conditional_Nested()
        => Assert.That(Eval(@"{ var x = 15; return x > 20 ? ""large"" : x > 10 ? ""medium"" : ""small""; }"), Is.EqualTo("medium"));

    [Test]
    public void Conditional_NumericPromotion()
        => Assert.That(Eval("true ? 1 : 2.5"), Is.EqualTo(1.0));

    [Test]
    public void Conditional_NumericPromotion_False()
        => Assert.That(Eval("false ? 1 : 2.5"), Is.EqualTo(2.5));
}
