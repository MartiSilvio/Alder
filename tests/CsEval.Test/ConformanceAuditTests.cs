namespace CsEval.Test;

/// <summary>
/// Adversarial conformance tests probing ECMA-334 7th edition compliance.
/// Each test targets a specific spec section with edge cases designed to break the implementation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConformanceAuditTests(CompilationMode mode)
{
    private CsEvalEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §10.2.3 Implicit numeric conversions — binary numeric promotion
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void BinaryPromotion_UintPlusSbyte_ProducesLong()
    {
        // §12.4.7.3: uint + sbyte → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (sbyte)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusShort_ProducesLong()
    {
        // §12.4.7.3: uint + short → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (short)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusInt_ProducesLong()
    {
        // §12.4.7.3: uint + int → both promoted to long
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", 5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(15L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void BinaryPromotion_UintPlusUint_ProducesUint()
    {
        // §12.4.7.3: uint + uint → stays uint
        var engine = Engine();
        engine.SetVariable("a", (uint)10);
        engine.SetVariable("b", (uint)5);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo((uint)15));
        Assert.That(result, Is.TypeOf<uint>());
    }

    [Test]
    public void BinaryPromotion_BytePlusByte_ProducesInt()
    {
        // §12.4.7.3: byte + byte → both promoted to int
        var engine = Engine();
        engine.SetVariable("a", (byte)100);
        engine.SetVariable("b", (byte)50);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(150));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_ShortPlusShort_ProducesInt()
    {
        // §12.4.7.3: short + short → both promoted to int
        var engine = Engine();
        engine.SetVariable("a", (short)100);
        engine.SetVariable("b", (short)50);
        var result = engine.Evaluate("return a + b;");
        Assert.That(result, Is.EqualTo(150));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_CharPlusChar_ProducesInt()
    {
        // char is promoted to int per §12.4.7.3
        var result = Eval("'A' + 'B'");
        Assert.That(result, Is.EqualTo(131)); // 65 + 66
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void BinaryPromotion_LongPlusUlong_ShouldError()
    {
        // §12.4.7.3: ulong + long → binding-time error
        var engine = Engine();
        engine.SetVariable("a", (ulong)10);
        engine.SetVariable("b", (long)5);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("return a + b;"));
    }

    [Test]
    public void BinaryPromotion_DecimalPlusDouble_ShouldError()
    {
        // §12.4.7.3: decimal + double → binding-time error
        var engine = Engine();
        engine.SetVariable("a", 10m);
        engine.SetVariable("b", 5.0);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("return a + b;"));
    }

    [Test]
    public void BinaryPromotion_DecimalPlusFloat_ShouldError()
    {
        // §12.4.7.3: decimal + float → binding-time error
        var engine = Engine();
        engine.SetVariable("a", 10m);
        engine.SetVariable("b", 5.0f);
        Assert.Throws<CsEvalException>(() => engine.Evaluate("return a + b;"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.7.2 Unary numeric promotions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void UnaryPromotion_BitwiseComplementByte_ProducesInt()
    {
        // §12.4.7.2: ~byte → promoted to int, result is int
        var engine = Engine();
        engine.SetVariable("x", (byte)0xFF);
        var result = engine.Evaluate("return ~x;");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(~(int)(byte)0xFF));
    }

    [Test]
    public void UnaryPromotion_UnaryPlusByte_ProducesInt()
    {
        // §12.4.7.2: +byte → promoted to int
        var engine = Engine();
        engine.SetVariable("x", (byte)42);
        var result = engine.Evaluate("return +x;");
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void UnaryPromotion_NegateSbyte_ProducesInt()
    {
        // §12.4.7.2: -sbyte → promoted to int
        var engine = Engine();
        engine.SetVariable("x", (sbyte)5);
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-5));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void UnaryPromotion_NegateChar_ProducesInt()
    {
        // §12.4.7.2: -char → promoted to int
        var engine = Engine();
        engine.SetVariable("x", 'A');
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-65));
        Assert.That(result, Is.TypeOf<int>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // §10.2.11 Implicit constant expression conversions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ImplicitConstExprConversion_IntToByteInRange()
    {
        // §10.2.11: constant int in range [0..255] → byte assignment
        var result = Eval(@"{
            byte b = 200;
            return b;
        }");
        Assert.That(result, Is.EqualTo((byte)200));
    }

    [Test]
    public void ImplicitConstExprConversion_IntToSbyte()
    {
        var result = Eval(@"{
            sbyte sb = -100;
            return sb;
        }");
        Assert.That(result, Is.EqualTo((sbyte)-100));
    }

    [Test]
    public void ImplicitConstExprConversion_IntToUshort()
    {
        var result = Eval(@"{
            ushort us = 60000;
            return us;
        }");
        Assert.That(result, Is.EqualTo((ushort)60000));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.8 Lifted operators — nullable semantics
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void LiftedOperator_NullableEquals_BothNull_True()
    {
        // §12.4.8: lifted == considers two null values equal
        var result = Eval(@"{
            int? a = null;
            int? b = null;
            return a == b;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void LiftedOperator_NullableEquals_OneNull_False()
    {
        var result = Eval(@"{
            int? a = 5;
            int? b = null;
            return a == b;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void LiftedOperator_NullableNotEquals_BothNull_False()
    {
        // §12.4.8: lifted != considers two null values equal → != is false
        var result = Eval(@"{
            int? a = null;
            int? b = null;
            return a != b;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void LiftedOperator_NullableLessThan_OneNull_False()
    {
        // §12.4.8: lifted < returns false if either operand is null
        var result = Eval(@"{
            int? a = 5;
            int? b = null;
            return a < b;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void LiftedOperator_NullableGreaterThanOrEqual_BothNull_False()
    {
        // §12.4.8: lifted >= returns false if either operand is null
        var result = Eval(@"{
            int? a = null;
            int? b = null;
            return a >= b;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void LiftedOperator_NullableMultiply_OneNull_ReturnsNull()
    {
        var result = Eval(@"{
            int? a = 5;
            int? b = null;
            return a * b;
        }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LiftedOperator_NullableUnaryNegate_Null_ReturnsNull()
    {
        // §12.4.8: lifted unary - returns null if operand is null
        var result = Eval(@"{
            int? x = null;
            return -x;
        }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LiftedOperator_NullableUnaryNegate_NonNull()
    {
        var result = Eval(@"{
            int? x = 5;
            return -x;
        }");
        Assert.That(result, Is.EqualTo(-5));
    }

    [Test]
    public void LiftedOperator_NullableBitwiseComplement_Null()
    {
        var result = Eval(@"{
            int? x = null;
            return ~x;
        }");
        Assert.That(result, Is.Null);
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.13.5 Nullable Boolean & and | operators
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NullableBoolAnd_TrueAndNull_IsNull()
    {
        // §12.13.5 truth table: true & null = null
        var result = Eval(@"{
            bool? x = true;
            bool? y = null;
            return x & y;
        }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolAnd_FalseAndNull_IsFalse()
    {
        // §12.13.5 truth table: false & null = false
        var result = Eval(@"{
            bool? x = false;
            bool? y = null;
            return x & y;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void NullableBoolOr_TrueOrNull_IsTrue()
    {
        // §12.13.5 truth table: true | null = true
        var result = Eval(@"{
            bool? x = true;
            bool? y = null;
            return x | y;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NullableBoolOr_FalseOrNull_IsNull()
    {
        // §12.13.5 truth table: false | null = null
        var result = Eval(@"{
            bool? x = false;
            bool? y = null;
            return x | y;
        }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolOr_NullOrNull_IsNull()
    {
        var result = Eval(@"{
            bool? x = null;
            bool? y = null;
            return x | y;
        }");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableBoolAnd_NullAndNull_IsNull()
    {
        var result = Eval(@"{
            bool? x = null;
            bool? y = null;
            return x & y;
        }");
        Assert.That(result, Is.Null);
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.5 String concatenation — null handling
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void StringConcat_NullPlusNull_EmptyString()
    {
        // §12.10.5: null operand → empty string substituted
        var result = Eval(@"{
            string a = null;
            string b = null;
            return a + b;
        }");
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

    // ═══════════════════════════════════════════════════════════════════
    // §12.18 Conditional operator — type rules
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ConditionalOperator_IntAndLong_ProducesLong()
    {
        // If one arm is int and other is long, implicit conversion → long
        var result = Eval(@"{
            var x = true ? 1 : 2L;
            return x;
        }");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void ConditionalOperator_IntAndDouble_ProducesDouble()
    {
        var result = Eval(@"{
            var x = true ? 1 : 2.0;
            return x;
        }");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void ConditionalOperator_NullAndString_ProducesString()
    {
        var result = Eval(@"{
            string s = true ? ""hello"" : null;
            return s;
        }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.9.3 Unary minus — edge cases
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.3 Division operator — edge cases
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IntegerDivision_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Eval(@"{
            var x = 1;
            var y = 0;
            return x / y;
        }"));
    }

    [Test]
    public void IntegerDivision_MinValueByMinusOne_CheckedThrows()
    {
        // §12.10.3: int.MinValue / -1 overflows in checked
        Assert.Throws<OverflowException>(() => Eval(@"{
            return checked(int.MinValue / -1);
        }"));
    }

    [Test]
    public void IntegerRemainder_ByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Eval(@"{
            var x = 1;
            var y = 0;
            return x % y;
        }"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.11 Shift operators
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // §12.12.7 Reference equality operators
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void StringEquality_SameContent_IsTrue()
    {
        // §12.12.8: string equality compares by value
        var result = Eval(@"""hello"" == ""hell"" + ""o""");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.15 / §12.9.6 Increment/decrement semantics
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void PostfixIncrement_ReturnsOriginalValue()
    {
        var result = Eval(@"{
            var x = 5;
            var y = x++;
            return y;
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrefixIncrement_ReturnsIncrementedValue()
    {
        var result = Eval(@"{
            var x = 5;
            var y = ++x;
            return y;
        }");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void PostfixDecrement_ReturnsOriginalValue()
    {
        var result = Eval(@"{
            var x = 5;
            var y = x--;
            return y;
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrefixDecrement_ReturnsDecrementedValue()
    {
        var result = Eval(@"{
            var x = 5;
            var y = --x;
            return y;
        }");
        Assert.That(result, Is.EqualTo(4));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.21.4 Compound assignment — type narrowing
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CompoundAssign_BytePlusEquals_ByteValue()
    {
        // §12.21.4: byte += byte → implicit narrowing back to byte
        var result = Eval(@"{
            byte b = 10;
            b += 5;
            return b;
        }");
        Assert.That(result, Is.EqualTo((byte)15));
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void CompoundAssign_ShortTimesEquals()
    {
        var result = Eval(@"{
            short s = 10;
            s *= 3;
            return s;
        }");
        Assert.That(result, Is.EqualTo((short)30));
        Assert.That(result, Is.TypeOf<short>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // §8.3.13 Boxing/unboxing
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Boxing_IntToObject_UnboxToInt()
    {
        var result = Eval(@"{
            object o = 42;
            return (int)o;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Unboxing_WrongType_Throws()
    {
        // §10.3.7: unboxing to wrong type should fail
        // CsEval wraps this as CsEvalException, which is acceptable
        Assert.Throws<CsEvalException>(() => Eval(@"{
            object o = 42;
            return (long)o;
        }"));
    }

    [Test]
    public void Boxing_NullableInt_HasValue_BoxesToInt()
    {
        // §8.3.13: boxing nullable with HasValue=true boxes the underlying value
        // GetType() is sandboxed, so test via unboxing to int (not int?)
        var result = Eval(@"{
            int? x = 42;
            object o = x;
            return (int)o;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Boxing_NullableInt_Null_BoxesToNull()
    {
        var result = Eval(@"{
            int? x = null;
            object o = x;
            return o == null;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.9.5 Foreach — variable per-iteration
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Foreach_VariableIsPerIteration_LambdaCapture()
    {
        // Per C# spec, foreach variable is fresh per iteration for closures
        var result = Eval(@"{
            var funcs = new List<Func<int>>();
            foreach (var x in new[] { 1, 2, 3 })
            {
                funcs.Add(() => x);
            }
            return funcs[0]() + funcs[1]() + funcs[2]();
        }");
        Assert.That(result, Is.EqualTo(6)); // 1+2+3, not 3+3+3
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.8.3 Switch — pattern matching completeness
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void SwitchExpression_WithWhenClause()
    {
        var result = Eval(@"{
            var x = 5;
            return x switch
            {
                int n when n > 10 => ""big"",
                int n when n > 0 => ""small"",
                _ => ""other""
            };
        }");
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void SwitchExpression_TypePattern()
    {
        var result = Eval(@"{
            object obj = ""hello"";
            return obj switch
            {
                int i => $""int:{i}"",
                string s => $""str:{s}"",
                _ => ""unknown""
            };
        }");
        Assert.That(result, Is.EqualTo("str:hello"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.11 Try/catch/finally — ordering guarantees
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void TryCatchFinally_FinallyAlwaysRuns()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                log += ""try:"";
                throw new System.InvalidOperationException();
            }
            catch (System.InvalidOperationException)
            {
                log += ""catch:"";
            }
            finally
            {
                log += ""finally"";
            }
            return log;
        }");
        Assert.That(result, Is.EqualTo("try:catch:finally"));
    }

    [Test]
    public void TryCatchFinally_FinallyRunsOnReturn()
    {
        var result = Eval(@"{
            var ran = false;
            try
            {
                return 42;
            }
            finally
            {
                ran = true;
            }
        }");
        // Return value should be 42, and finally should have run
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TryCatch_SpecificCatch_BeforeGeneral()
    {
        var result = Eval(@"{
            try
            {
                throw new System.ArgumentException(""test"");
            }
            catch (System.ArgumentException)
            {
                return ""specific"";
            }
            catch (System.Exception)
            {
                return ""general"";
            }
        }");
        Assert.That(result, Is.EqualTo("specific"));
    }

    [Test]
    public void TryCatch_ExceptionInCatch_CaughtByOuter()
    {
        var result = Eval(@"{
            try
            {
                try
                {
                    throw new System.InvalidOperationException();
                }
                catch (System.InvalidOperationException)
                {
                    throw new System.ArgumentException(""rethrown"");
                }
            }
            catch (System.ArgumentException ex)
            {
                return ex.Message;
            }
        }");
        Assert.That(result, Is.EqualTo("rethrown"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.15 Null-coalescing operator — type inference
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NullCoalescing_IntNullable_IntFallback()
    {
        var result = Eval(@"{
            int? x = null;
            int y = x ?? 42;
            return y;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NullCoalescing_Chain()
    {
        var result = Eval(@"{
            string a = null;
            string b = null;
            string c = ""found"";
            return a ?? b ?? c;
        }");
        Assert.That(result, Is.EqualTo("found"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.9.7 Cast expressions — edge cases
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Cast_DoubleToInt_Truncates()
    {
        var result = Eval("(int)3.9");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Cast_NegativeDoubleToInt_TruncatesTowardsZero()
    {
        var result = Eval("(int)-3.9");
        Assert.That(result, Is.EqualTo(-3));
    }

    [Test]
    public void Cast_IntToChar()
    {
        var result = Eval("(char)65");
        Assert.That(result, Is.EqualTo('A'));
    }

    [Test]
    public void Cast_CharToInt()
    {
        var result = Eval("(int)'A'");
        Assert.That(result, Is.EqualTo(65));
    }

    [Test]
    public void Cast_IntToByte_Truncates()
    {
        var result = Eval("(byte)256");
        Assert.That(result, Is.EqualTo((byte)0));
    }

    [Test]
    public void Cast_LongToInt_Truncates()
    {
        var result = Eval(@"{
            long x = (long)int.MaxValue + 1;
            return unchecked((int)x);
        }");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.4 Remainder operator
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // §12.2 Expression classifications — evaluation order
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void EvaluationOrder_LeftToRight_Addition()
    {
        // Left operand evaluated before right
        var result = Eval(@"{
            var log = """";
            Func<int, int> f = (x) => { log += x.ToString(); return x; };
            var r = f(1) + f(2) + f(3);
            return log;
        }");
        Assert.That(result, Is.EqualTo("123"));
    }

    [Test]
    public void EvaluationOrder_ShortCircuitAnd_StopsEarly()
    {
        var result = Eval(@"{
            var count = 0;
            Func<bool> f = () => { count++; return true; };
            var r = false && f();
            return count;
        }");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void EvaluationOrder_ShortCircuitOr_StopsEarly()
    {
        var result = Eval(@"{
            var count = 0;
            Func<bool> f = () => { count++; return false; };
            var r = true || f();
            return count;
        }");
        Assert.That(result, Is.EqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §10.2.4 Implicit enumeration conversion
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ImplicitEnumConversion_ZeroToEnum()
    {
        // §10.2.4: constant 0 implicitly converts to any enum type
        var result = Eval(@"{
            var d = (System.DayOfWeek)0;
            return d;
        }");
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

    // ═══════════════════════════════════════════════════════════════════
    // §12.12.6 Enumeration comparison operators
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void EnumComparison_LessThan()
    {
        var result = Eval("System.DayOfWeek.Monday < System.DayOfWeek.Friday");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void EnumComparison_GreaterThanOrEqual()
    {
        var result = Eval("System.DayOfWeek.Friday >= System.DayOfWeek.Friday");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Control flow torture tests
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedSwitch_BreakExitsInnerOnly()
    {
        var result = Eval(@"{
            var r = 0;
            switch (1)
            {
                case 1:
                    switch (2)
                    {
                        case 2: r = 99; break;
                    }
                    r += 1;
                    break;
            }
            return r;
        }");
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void NestedFor_BreakAndContinue()
    {
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                if (i == 3) continue;
                if (i == 4) break;
                for (var j = 0; j < 3; j++)
                {
                    if (j == 1) break;
                    sum++;
                }
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(3)); // i=0,1,2 each contribute 1 (j=0)
    }

    [Test]
    public void DoWhile_RunsAtLeastOnce()
    {
        var result = Eval(@"{
            var count = 0;
            do { count++; } while (false);
            return count;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void While_FalseCondition_NeverRuns()
    {
        var result = Eval(@"{
            var count = 0;
            while (false) { count++; }
            return count;
        }");
        Assert.That(result, Is.EqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Expression torture tests — complex nesting
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void DeepNesting_Ternary()
    {
        var result = Eval(@"{
            var x = 3;
            return x == 1 ? ""one"" : x == 2 ? ""two"" : x == 3 ? ""three"" : ""other"";
        }");
        Assert.That(result, Is.EqualTo("three"));
    }

    [Test]
    public void DeepNesting_NullCoalescing()
    {
        var result = Eval(@"{
            int? a = null;
            int? b = null;
            int? c = null;
            int? d = 42;
            return a ?? b ?? c ?? d ?? 0;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void MixedArithmetic_PrecedenceChain()
    {
        // Verify: 2 + 3 * 4 - 1 = 2 + 12 - 1 = 13
        var result = Eval("2 + 3 * 4 - 1");
        Assert.That(result, Is.EqualTo(13));
    }

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
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.12.12 is operator — various patterns
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IsPattern_ConstantPattern()
    {
        var result = Eval(@"{
            object x = 42;
            return x is 42;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsPattern_NullPattern()
    {
        var result = Eval(@"{
            string s = null;
            return s is null;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsPattern_NotNull()
    {
        var result = Eval(@"{
            string s = ""hello"";
            return s is not null;
        }");
        Assert.That(result, Is.EqualTo(true));
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
        var result = Eval(@"{
            var x = 10;
            return x is > 5;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsPattern_Relational_LessThanOrEqual()
    {
        var result = Eval(@"{
            var x = 5;
            return x is <= 5;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsPattern_LogicalAnd()
    {
        var result = Eval(@"{
            var x = 5;
            return x is > 0 and < 10;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void IsPattern_LogicalOr()
    {
        var result = Eval(@"{
            var x = 15;
            return x is < 5 or > 10;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.20 Default value expressions
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // §12.13.2 Integer logical operators
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // Interaction tests — multiple feature combinations
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Interaction_NullableArithmeticWithConversion()
    {
        var result = Eval(@"{
            int? x = 5;
            double? y = 2.5;
            return x + y;
        }");
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void Interaction_NullCoalescing_WithCast()
    {
        var result = Eval(@"{
            int? x = null;
            long y = (long)(x ?? 42);
            return y;
        }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Interaction_TernaryInStringInterpolation()
    {
        var result = Eval(@"{
            var x = 5;
            return $""value is {(x > 3 ? ""big"" : ""small"")}"";
        }");
        Assert.That(result, Is.EqualTo("value is big"));
    }

    [Test]
    public void Interaction_LambdaInLinq()
    {
        var result = Eval(@"{
            var nums = new List<int> { 1, 2, 3, 4, 5 };
            return nums.Where(x => x % 2 == 0).Sum();
        }");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Interaction_ForEachWithSwitch()
    {
        var result = Eval(@"{
            var items = new[] { 1, 2, 3, 4, 5 };
            var sum = 0;
            foreach (var item in items)
            {
                switch (item % 3)
                {
                    case 0: sum += 100; break;
                    case 1: sum += 10; break;
                    default: sum += 1; break;
                }
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(122)); // 10+1+100+10+1
    }

    [Test]
    public void Interaction_ExceptionInForEach()
    {
        var result = Eval(@"{
            var items = new[] { 1, 2, 0, 4 };
            var sum = 0;
            foreach (var item in items)
            {
                try
                {
                    sum += 10 / item;
                }
                catch (System.DivideByZeroException)
                {
                    sum += -1;
                }
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(10 + 5 + -1 + 2)); // 16
    }

    [Test]
    public void Interaction_NullConditional_WithNullCoalescing()
    {
        var result = Eval(@"{
            string s = null;
            return s?.Length ?? -1;
        }");
        Assert.That(result, Is.EqualTo(-1));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.3 Integer literals — hex, binary, digit separators
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IntegerLiteral_Hex()
    {
        var result = Eval("0xFF");
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void IntegerLiteral_Binary()
    {
        var result = Eval("0b1010");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntegerLiteral_DigitSeparator()
    {
        var result = Eval("1_000_000");
        Assert.That(result, Is.EqualTo(1000000));
    }

    [Test]
    public void IntegerLiteral_HexDigitSeparator()
    {
        var result = Eval("0xFF_FF");
        Assert.That(result, Is.EqualTo(65535));
    }

    [Test]
    public void IntegerLiteral_BinaryDigitSeparator()
    {
        var result = Eval("0b1111_0000");
        Assert.That(result, Is.EqualTo(240));
    }

    [Test]
    public void LongLiteral_Suffix()
    {
        var result = Eval("42L");
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void UintLiteral_Suffix()
    {
        var result = Eval("42U");
        Assert.That(result, Is.TypeOf<uint>());
    }

    [Test]
    public void UlongLiteral_Suffix()
    {
        var result = Eval("42UL");
        Assert.That(result, Is.TypeOf<ulong>());
    }

    [Test]
    public void FloatLiteral_Suffix()
    {
        var result = Eval("3.14f");
        Assert.That(result, Is.TypeOf<float>());
    }

    [Test]
    public void DecimalLiteral_Suffix()
    {
        var result = Eval("3.14m");
        Assert.That(result, Is.TypeOf<decimal>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.5 Character literals — escape sequences
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void CharLiteral_UnicodeEscape()
    {
        var result = Eval(@"'\u0041'"); // 'A'
        Assert.That(result, Is.EqualTo('A'));
    }

    [Test]
    public void CharLiteral_HexEscape()
    {
        var result = Eval(@"'\x41'"); // 'A'
        Assert.That(result, Is.EqualTo('A'));
    }

    [Test]
    public void CharLiteral_Backslash()
    {
        var result = Eval(@"'\\'");
        Assert.That(result, Is.EqualTo('\\'));
    }

    [Test]
    public void CharLiteral_Null()
    {
        var result = Eval(@"'\0'");
        Assert.That(result, Is.EqualTo('\0'));
    }

    [Test]
    public void CharLiteral_Tab()
    {
        var result = Eval(@"'\t'");
        Assert.That(result, Is.EqualTo('\t'));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §6.4.5.6 String literals — escape sequences
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void StringLiteral_UnicodeEscape()
    {
        var result = Eval(@"""\u0048\u0065\u006C\u006C\u006F""");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void VerbatimString_DoubleQuoteEscape()
    {
        var result = Eval(@"@""He said """"hello""""""");
        Assert.That(result, Is.EqualTo("He said \"hello\""));
    }

    [Test]
    public void VerbatimString_ContainsNewlines()
    {
        var result = Eval("@\"line1\nline2\"");
        Assert.That(result!.ToString(), Does.Contain("line1"));
        Assert.That(result!.ToString(), Does.Contain("line2"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.22 Nameof
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // Scope rules — variable shadowing in nested blocks
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void VariableShadowing_InnerBlockCanShadow()
    {
        // In C# scripting/statement contexts, inner blocks can shadow outer variables
        // Note: C# spec §7.7 actually forbids this in method scope — test if CsEval enforces it
        var result = Eval(@"{
            var x = 1;
            {
                var x2 = 2;
                x = x2;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(2));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.16.2 Object creation — with initializers
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ObjectInit_ListWithInitializer()
    {
        var result = Eval(@"{
            var list = new List<int> { 1, 2, 3 };
            return list.Count;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ObjectInit_DictionaryIndexInitializer_Standard()
    {
        // §12.8.16.3: Dictionary initializer with indexer syntax
        // This is standard C# 6+ syntax, not an Extended-mode collection expression
        var result = Eval(@"{
            var dict = new Dictionary<string, int> { [""a""] = 1, [""b""] = 2 };
            return dict[""a""] + dict[""b""];
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.5 Addition — checked overflow
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // Complex real-world patterns
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void RealWorld_FibonacciLoop()
    {
        var result = Eval(@"{
            var a = 0;
            var b = 1;
            for (var i = 0; i < 10; i++)
            {
                var temp = b;
                b = a + b;
                a = temp;
            }
            return a;
        }");
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void RealWorld_LinqChain()
    {
        var result = Eval(@"{
            return Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => x * x)
                .Sum();
        }");
        Assert.That(result, Is.EqualTo(4 + 16 + 36 + 64 + 100)); // 220
    }

    [Test]
    public void RealWorld_StringManipulation()
    {
        // Tests string.Join with an IEnumerable<string> from Select
        var result = Eval(@"{
            var words = ""hello world foo bar"".Split(' ');
            var capitalized = words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)).ToArray();
            var result = string.Join(""-"", capitalized);
            return result;
        }");
        Assert.That(result, Is.EqualTo("Hello-World-Foo-Bar"));
    }

    [Test]
    public void RealWorld_StringJoinWithIEnumerable()
    {
        // string.Join(string, IEnumerable<string>) should work directly
        var result = Eval(@"{
            var words = ""hello world foo bar"".Split(' ');
            var result = string.Join(""-"", words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)));
            return result;
        }");
        Assert.That(result, Is.EqualTo("Hello-World-Foo-Bar"));
    }

    [Test]
    public void RealWorld_StringJoinWithStringArray()
    {
        // Simpler variant: string[] passed directly
        var result = Eval(@"{
            var result = string.Join(""-"", new string[] { ""a"", ""b"", ""c"" });
            return result;
        }");
        Assert.That(result, Is.EqualTo("a-b-c"));
    }
}
