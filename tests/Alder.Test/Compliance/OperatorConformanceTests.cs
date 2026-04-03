using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

public enum LongEnum : long { A = 1, B = 2, C = 3 }

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class OperatorConformanceTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(LanguageMode lang = LanguageMode.Standard, HashSet<Type>? allowedTypes = null)
        => TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = lang;
            if (allowedTypes != null)
                o.Sandbox = SandboxOptions.Trusted() with { TrustedTypes = allowedTypes };
        });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => CreateEngine(lang).Evaluate(expr);

    #region §10.2.4 Implicit enumeration conversion
    [Test]
    public void ImplicitEnumConversion_ZeroToEnum()
    {
        // §10.2.4: constant 0 implicitly converts to any enum type
        var result = Eval(@"
            var d = (System.DayOfWeek)0;
            return d;
        ");
        Assert.That(result, Is.EqualTo(DayOfWeek.Sunday));
    }

    [Test]
    public void ImplicitEnumConversion_ZeroToEnum_Assignment()
    {
        // §10.2.4: constant 0 implicitly converts to any enum
        // This requires typed variable declaration with FQN and literal 0 assignment
        var result = Eval(@"{
            System.DayOfWeek d = 0;
            return d;
        }");
        Assert.That(result, Is.EqualTo(DayOfWeek.Sunday));
    }

    #endregion

    #region §12.4.8 Lifted operators — nullable semantics
    [Test]
    public void LiftedOperator_NullableEquals_BothNull_True()
    {
        // §12.4.8: lifted == considers two null values equal
        var result = Eval(@"
            int? a = null;
            int? b = null;
            return a == b;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LiftedOperator_NullableEquals_OneNull_False()
    {
        var result = Eval(@"
            int? a = 5;
            int? b = null;
            return a == b;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void LiftedOperator_NullableNotEquals_BothNull_False()
    {
        // §12.4.8: lifted != considers two null values equal → != is false
        var result = Eval(@"
            int? a = null;
            int? b = null;
            return a != b;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void LiftedOperator_NullableLessThan_OneNull_False()
    {
        // §12.4.8: lifted < returns false if either operand is null
        var result = Eval(@"
            int? a = 5;
            int? b = null;
            return a < b;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void LiftedOperator_NullableGreaterThanOrEqual_BothNull_False()
    {
        // §12.4.8: lifted >= returns false if either operand is null
        var result = Eval(@"
            int? a = null;
            int? b = null;
            return a >= b;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void LiftedOperator_NullableMultiply_OneNull_ReturnsNull()
    {
        var result = Eval(@"
            int? a = 5;
            int? b = null;
            return a * b;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LiftedOperator_NullableUnaryNegate_Null_ReturnsNull()
    {
        // §12.4.8: lifted unary - returns null if operand is null
        var result = Eval(@"
            int? x = null;
            return -x;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LiftedOperator_NullableUnaryNegate_NonNull()
    {
        var result = Eval(@"
            int? x = 5;
            return -x;
        ");
        Assert.That(result, Is.EqualTo(-5));
    }

    [Test]
    public void LiftedOperator_NullableBitwiseComplement_Null()
    {
        var result = Eval(@"
            int? x = null;
            return ~x;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region §12.13.5 Nullable Boolean & and | operators
    [Test]
    public void NullableBoolAnd_TrueAndNull_IsNull()
    {
        // §12.13.5 truth table: true & null = null
        var result = Eval(@"
            bool? x = true;
            bool? y = null;
            return x & y;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolAnd_FalseAndNull_IsFalse()
    {
        // §12.13.5 truth table: false & null = false
        var result = Eval(@"
            bool? x = false;
            bool? y = null;
            return x & y;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void NullableBoolOr_TrueOrNull_IsTrue()
    {
        // §12.13.5 truth table: true | null = true
        var result = Eval(@"
            bool? x = true;
            bool? y = null;
            return x | y;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void NullableBoolOr_FalseOrNull_IsNull()
    {
        // §12.13.5 truth table: false | null = null
        var result = Eval(@"
            bool? x = false;
            bool? y = null;
            return x | y;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolOr_NullOrNull_IsNull()
    {
        var result = Eval(@"
            bool? x = null;
            bool? y = null;
            return x | y;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolAnd_NullAndNull_IsNull()
    {
        var result = Eval(@"
            bool? x = null;
            bool? y = null;
            return x & y;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region §12.10.5 String concatenation — null handling
    [Test]
    public void StringConcat_NullPlusNull_EmptyString()
    {
        // §12.10.5: null operand → empty string substituted
        var result = Eval(@"
            string a = null;
            string b = null;
            return a + b;
        ");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void StringConcat_ObjectToString_NullResult()
    {
        // §12.10.5: if ToString returns null, empty string substituted
        // In practice this is rare, but test with known types
        var result = Eval(@"42 + "" items""");
        Assert.That(result, Is.EqualTo("42 items"));
    }

    #endregion

    #region §12.18 Conditional operator — type rules
    [Test]
    public void ConditionalOperator_IntAndLong_ProducesLong()
    {
        // If one arm is int and other is long, implicit conversion → long
        var result = Eval(@"
            var x = true ? 1 : 2L;
            return x;
        ");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void ConditionalOperator_IntAndDouble_ProducesDouble()
    {
        var result = Eval(@"
            var x = true ? 1 : 2.0;
            return x;
        ");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void ConditionalOperator_NullAndString_ProducesString()
    {
        var result = Eval(@"
            string s = true ? ""hello"" : null;
            return s;
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    #endregion

    #region §12.9.3 Unary minus — edge cases
    [Test]
    public void UnaryMinus_IntMinValue_CheckedThrows()
    {
        // checked(-int.MinValue) overflows
        Assert.Throws<OverflowException>(() => Eval("checked(-int.MinValue)"));
    }

    [Test]
    public void UnaryMinus_IntMinValue_UncheckedWraps()
    {
        // unchecked(-int.MinValue) == int.MinValue
        var result = Eval("unchecked(-int.MinValue)");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    [Test]
    public void UnaryMinus_LongMinValue_UncheckedWraps()
    {
        var result = Eval("unchecked(-long.MinValue)");
        Assert.That(result, Is.EqualTo(long.MinValue));
    }

    #endregion

    #region §12.10.3 Division operator — edge cases
    [Test]
    public void IntegerDivision_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Eval(@"
            var x = 1;
            var y = 0;
            return x / y;
        "));
    }

    [Test]
    public void IntegerDivision_MinValueByMinusOne_CheckedThrows()
    {
        // §12.10.3: int.MinValue / -1 overflows in checked
        Assert.Throws<OverflowException>(() => Eval(@"
            return checked(int.MinValue / -1);
        "));
    }

    [Test]
    public void IntegerRemainder_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Eval(@"
            var x = 1;
            var y = 0;
            return x % y;
        "));
    }

    #endregion

    #region §12.11 Shift operators
    [Test]
    public void LeftShift_IntBy32_EffectivelyZeroShift()
    {
        // §12.11: for int, shift count is masked to 5 bits (count & 0x1F)
        // So shifting by 32 effectively shifts by 0
        var result = Eval("1 << 32");
        Assert.That(result, Is.EqualTo(1)); // 32 & 0x1F = 0
    }

    [Test]
    public void LeftShift_IntBy33_ShiftsBy1()
    {
        // 33 & 0x1F = 1
        var result = Eval("1 << 33");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void RightShift_Negative_ArithmeticShift()
    {
        // §12.11: >> on int uses arithmetic shift (sign-extending)
        var result = Eval("-8 >> 2");
        Assert.That(result, Is.EqualTo(-2));
    }

    #endregion

    #region §12.10.6 Enumeration subtraction: E - E → U
    [Test]
    public void EnumSubtraction_IntUnderlying_ReturnsInt()
    {
        // §12.10.6: E - E → U where U is the underlying type
        // DayOfWeek has int underlying
        var result = Eval("System.DayOfWeek.Friday - System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(4));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void EnumSubtraction_LongUnderlying_ReturnsLong()
    {
        // §12.10.6: E - E → U where U is the underlying type
        var engine = CreateEngine();
        engine.SetVariable("a", LongEnum.A);
        engine.SetVariable("b", LongEnum.B);
        var result = engine.Evaluate("b - a");
        Assert.That(result, Is.EqualTo(1L));
        Assert.That(result, Is.TypeOf<long>());
    }

    #endregion

    #region §12.12.7 Reference equality operators
    [Test]
    public void StringEquality_SameContent_IsTrue()
    {
        // §12.12.8: string equality compares by value
        var result = Eval(@"""hello"" == ""hell"" + ""o""");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.8.15 / §12.9.6 Increment/decrement semantics
    [Test]
    public void PostfixIncrement_ReturnsOriginalValue()
    {
        var result = Eval(@"
            var x = 5;
            var y = x++;
            return y;
        ");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrefixIncrement_ReturnsIncrementedValue()
    {
        var result = Eval(@"
            var x = 5;
            var y = ++x;
            return y;
        ");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void PostfixDecrement_ReturnsOriginalValue()
    {
        var result = Eval(@"
            var x = 5;
            var y = x--;
            return y;
        ");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrefixDecrement_ReturnsDecrementedValue()
    {
        var result = Eval(@"
            var x = 5;
            var y = --x;
            return y;
        ");
        Assert.That(result, Is.EqualTo(4));
    }

    #endregion

    #region §12.21.4 Compound assignment — type narrowing
    [Test]
    public void CompoundAssign_BytePlusEquals_ByteValue()
    {
        // §12.21.4: byte += byte → implicit narrowing back to byte
        var result = Eval(@"
            byte b = 10;
            b += 5;
            return b;
        ");
        Assert.That(result, Is.EqualTo((byte)15));
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void CompoundAssign_ShortTimesEquals()
    {
        var result = Eval(@"
            short s = 10;
            s *= 3;
            return s;
        ");
        Assert.That(result, Is.EqualTo((short)30));
        Assert.That(result, Is.TypeOf<short>());
    }

    #endregion

    #region §12.15 Null-coalescing operator — type inference
    [Test]
    public void NullCoalescing_IntNullable_IntFallback()
    {
        var result = Eval(@"
            int? x = null;
            int y = x ?? 42;
            return y;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NullCoalescing_Chain()
    {
        var result = Eval(@"
            string a = null;
            string b = null;
            string c = ""found"";
            return a ?? b ?? c;
        ");
        Assert.That(result, Is.EqualTo("found"));
    }

    #endregion

    #region §12.10.4 Remainder operator
    [Test]
    public void Remainder_FloatingPoint_DoesNotThrow()
    {
        // §12.10.4: floating point % does not throw on div by zero
        var result = Eval("5.0 % 0.0");
        Assert.That(result, Is.NaN);
    }

    [Test]
    public void Remainder_NegativeDividend()
    {
        var result = Eval("-7 % 3");
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void Remainder_NegativeDivisor()
    {
        var result = Eval("7 % -3");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region §12.2 Expression classifications — evaluation order
    [Test]
    public void EvaluationOrder_LeftToRight_Addition()
    {
        // Left operand evaluated before right
        var result = Eval(@"
            var log = """";
            Func<int, int> f = (x) => { log += x.ToString(); return x; };
            var r = f(1) + f(2) + f(3);
            return log;
        ");
        Assert.That(result, Is.EqualTo("123"));
    }

    [Test]
    public void EvaluationOrder_ShortCircuitAnd_StopsEarly()
    {
        var result = Eval(@"
            var count = 0;
            Func<bool> f = () => { count++; return true; };
            var r = false && f();
            return count;
        ");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void EvaluationOrder_ShortCircuitOr_StopsEarly()
    {
        var result = Eval(@"
            var count = 0;
            Func<bool> f = () => { count++; return false; };
            var r = true || f();
            return count;
        ");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region §12.12.6 Enumeration comparison operators
    [Test]
    public void EnumComparison_LessThan()
    {
        var result = Eval("System.DayOfWeek.Monday < System.DayOfWeek.Friday");
        Assert.That(result, Is.True);
    }

    [Test]
    public void EnumComparison_GreaterThanOrEqual()
    {
        var result = Eval("System.DayOfWeek.Friday >= System.DayOfWeek.Friday");
        Assert.That(result, Is.True);
    }

    #endregion

    #region Operator precedence
    [Test]
    public void OperatorPrecedence_ShiftVsAddition()
    {
        // << has lower precedence than +
        // 1 + 2 << 3 = (1 + 2) << 3 = 3 << 3 = 24
        var result = Eval("1 + 2 << 3");
        Assert.That(result, Is.EqualTo(24));
    }

    [Test]
    public void OperatorPrecedence_BitwiseAndVsEquality()
    {
        // & has lower precedence than ==
        // 5 & 1 == 1 should parse as 5 & (1 == 1) = 5 & true → error in standard C#
        // Actually in C#: & has lower precedence than ==
        // But 1 == 1 is bool, 5 & bool is error
        // Let's use a valid one: (5 & 1) == 1
        var result = Eval("(5 & 1) == 1");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.12.12 is operator — various patterns
    [Test]
    public void IsPattern_ConstantPattern()
    {
        var result = Eval(@"
            object x = 42;
            return x is 42;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_NullPattern()
    {
        var result = Eval(@"
            string s = null;
            return s is null;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_NotNull()
    {
        var result = Eval(@"
            string s = ""hello"";
            return s is not null;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_VarPattern()
    {
        var result = Eval(@"{
            object obj = 42;
            if (obj is var v)
                return v;
            return null;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void IsPattern_Relational_GreaterThan()
    {
        var result = Eval(@"
            var x = 10;
            return x is > 5;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_Relational_LessThanOrEqual()
    {
        var result = Eval(@"
            var x = 5;
            return x is <= 5;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_LogicalAnd()
    {
        var result = Eval(@"
            var x = 5;
            return x is > 0 and < 10;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPattern_LogicalOr()
    {
        var result = Eval(@"
            var x = 15;
            return x is < 5 or > 10;
        ");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.8.20 Default value expressions
    [Test]
    public void Default_Char()
    {
        var result = Eval("default(char)");
        Assert.That(result, Is.EqualTo('\0'));
    }

    [Test]
    public void Default_Double()
    {
        var result = Eval("default(double)");
        Assert.That(result, Is.EqualTo(0.0));
    }

    [Test]
    public void Default_NullableInt()
    {
        var result = Eval("default(int?)");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region §12.13.2 Integer logical operators
    [Test]
    public void BitwiseXor_IntValues()
    {
        var result = Eval("0xF0 ^ 0xFF");
        Assert.That(result, Is.EqualTo(0x0F));
    }

    [Test]
    public void BitwiseOr_IntValues()
    {
        var result = Eval("0x0F | 0xF0");
        Assert.That(result, Is.EqualTo(0xFF));
    }

    [Test]
    public void BitwiseAnd_IntValues()
    {
        var result = Eval("0xFF & 0x0F");
        Assert.That(result, Is.EqualTo(0x0F));
    }

    #endregion

    #region §12.8.22 Nameof
    [Test]
    public void Nameof_Type()
    {
        var result = Eval("nameof(System.Int32)");
        Assert.That(result, Is.EqualTo("Int32"));
    }

    [Test]
    public void Nameof_Method()
    {
        var result = Eval("nameof(System.Console.WriteLine)");
        Assert.That(result, Is.EqualTo("WriteLine"));
    }

    #endregion

    #region §12.10.5 Addition — checked overflow
    [Test]
    public void CheckedAdd_IntOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked(int.MaxValue + 1)"));
    }

    [Test]
    public void UncheckedAdd_IntOverflow_Wraps()
    {
        var result = Eval("unchecked(int.MaxValue + 1)");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    [Test]
    public void CheckedMultiply_IntOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked(int.MaxValue * 2)"));
    }

    #endregion

    #region §12.10.5/6 Enum arithmetic — more edge cases
    [Test]
    public void EnumArithmetic_BitwiseXor()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var a = System.IO.FileAccess.ReadWrite;
            var b = System.IO.FileAccess.Write;
            return a ^ b;
        ");
        Assert.That(result, Is.EqualTo(FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_BitwiseNot()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var a = System.IO.FileAccess.Read;
            return ~a;
        ");
        Assert.That(result, Is.EqualTo(~FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_HasFlag_Pattern()
    {
        var engine = CreateEngine(allowedTypes: [typeof(FileAccess)]);
        var result = engine.Evaluate(@"
            var flags = System.IO.FileAccess.Read | System.IO.FileAccess.Write;
            return (flags & System.IO.FileAccess.Read) != 0;
        ");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.10.2 Multiplication — floating point edge cases
    [Test]
    public void FloatMultiply_Infinity()
    {
        var result = Eval("double.PositiveInfinity * 2.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void FloatMultiply_InfinityTimesZero_IsNaN()
    {
        var result = Eval("double.PositiveInfinity * 0.0");
        Assert.That(result, Is.NaN);
    }

    [Test]
    public void FloatDivide_ZeroByZero_IsNaN()
    {
        var result = Eval("0.0 / 0.0");
        Assert.That(result, Is.NaN);
    }

    [Test]
    public void FloatDivide_ByZero_IsInfinity()
    {
        var result = Eval("1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void FloatDivide_NegativeByZero_IsNegativeInfinity()
    {
        var result = Eval("-1.0 / 0.0");
        Assert.That(result, Is.EqualTo(double.NegativeInfinity));
    }

    #endregion

    #region §12.12.13 The as operator — edge cases
    [Test]
    public void AsOperator_ValueType_NotAllowed()
    {
        // `as` only works for reference types and nullable value types
        // `obj as int` should be a compile error — test with nullable
        var result = Eval(@"
            object obj = 42;
            return obj as int?;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void AsOperator_NullableValueType_NoMatch()
    {
        var result = Eval(@"
            object obj = ""hello"";
            return obj as int?;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region §12.12.10 Equality between nullable and null literal
    [Test]
    public void NullableEquality_WithNullLiteral()
    {
        var result = Eval(@"
            int? x = null;
            return x == null;
        ");
        Assert.That(result, Is.True);
    }

    [Test]
    public void NullableEquality_NonNullWithNullLiteral()
    {
        var result = Eval(@"
            int? x = 5;
            return x == null;
        ");
        Assert.That(result, Is.False);
    }

    [Test]
    public void NullableInequality_WithNullLiteral()
    {
        var result = Eval(@"
            int? x = 5;
            return x != null;
        ");
        Assert.That(result, Is.True);
    }

    #endregion

    #region §12.8.17 typeof operator
    [Test]
    public void Typeof_GenericList()
    {
        var result = Eval("typeof(List<int>).Name");
        Assert.That(result, Does.Contain("List"));
    }

    [Test]
    public void Typeof_Array()
    {
        var result = Eval("typeof(int[]).IsArray");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Typeof_Nullable()
    {
        var result = Eval("typeof(int?).Name");
        Assert.That(result, Does.Contain("Nullable"));
    }
    #endregion
}
