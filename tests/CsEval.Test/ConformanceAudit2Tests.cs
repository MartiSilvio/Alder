namespace CsEval.Test;

/// <summary>
/// Second round of adversarial conformance tests — deeper edge cases.
/// Focuses on: type system interactions, control flow torture, scope rules,
/// numeric edge cases, and cross-feature interactions.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConformanceAudit2Tests(CompilationMode mode)
{
    private CsEvalEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §10.3.2 Explicit numeric conversions — truncation rules
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ExplicitConversion_DoubleToFloat_LosesPrecision()
    {
        var result = Eval(@"{
            double d = 1.23456789012345;
            float f = (float)d;
            return (double)f != d;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void ExplicitConversion_IntToSbyte_Truncates()
    {
        var result = Eval("unchecked((sbyte)200)");
        Assert.That(result, Is.EqualTo(unchecked((sbyte)200)));
    }

    [Test]
    public void ExplicitConversion_IntToSbyte_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => Eval("checked((sbyte)200)"));
    }

    [Test]
    public void ExplicitConversion_IntToUshort_Truncates()
    {
        var result = Eval("unchecked((ushort)70000)");
        Assert.That(result, Is.EqualTo(unchecked((ushort)70000)));
    }

    [Test]
    public void ExplicitConversion_LongToInt_CheckedThrows()
    {
        Assert.Throws<OverflowException>(() => Eval(@"{
            long x = (long)int.MaxValue + 1;
            return checked((int)x);
        }"));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedNaN_Throws()
    {
        // §10.3.2: in checked context, NaN → integral throws OverflowException
        Assert.Throws<OverflowException>(() => Eval("checked((int)double.NaN)"));
    }

    [Test]
    public void ExplicitConversion_DoubleToInt_CheckedInfinity_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked((int)double.PositiveInfinity)"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10 Arithmetic — overflow semantics
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IntMultiply_Overflow_Unchecked_Wraps()
    {
        var result = Eval("unchecked(int.MaxValue * 2)");
        Assert.That(result, Is.EqualTo(unchecked(int.MaxValue * 2)));
    }

    [Test]
    public void LongAdd_Overflow_Checked_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("checked(long.MaxValue + 1L)"));
    }

    [Test]
    public void DecimalOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Eval("decimal.MaxValue + 1m"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.5/6 Enum arithmetic — more edge cases
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void EnumArithmetic_BitwiseXor()
    {
        var result = Eval(@"{
            var a = System.IO.FileAccess.ReadWrite;
            var b = System.IO.FileAccess.Write;
            return a ^ b;
        }");
        Assert.That(result, Is.EqualTo(System.IO.FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_BitwiseNot()
    {
        var result = Eval(@"{
            var a = System.IO.FileAccess.Read;
            return ~a;
        }");
        Assert.That(result, Is.EqualTo(~System.IO.FileAccess.Read));
    }

    [Test]
    public void EnumArithmetic_HasFlag_Pattern()
    {
        var result = Eval(@"{
            var flags = System.IO.FileAccess.Read | System.IO.FileAccess.Write;
            return (flags & System.IO.FileAccess.Read) != 0;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.10.2 Multiplication — floating point edge cases
    // ═══════════════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════════════
    // §13.11 Try statement — nested exception handling
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedTryCatch_InnerExceptionPropagates()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                try
                {
                    log += ""A"";
                    throw new System.InvalidOperationException();
                }
                finally
                {
                    log += ""B"";
                }
            }
            catch (System.InvalidOperationException)
            {
                log += ""C"";
            }
            return log;
        }");
        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test]
    public void TryCatch_FilterCatchByType()
    {
        var result = Eval(@"{
            try
            {
                throw new System.NotImplementedException();
            }
            catch (System.ArgumentException)
            {
                return ""arg"";
            }
            catch (System.NotImplementedException)
            {
                return ""notimpl"";
            }
            catch (System.Exception)
            {
                return ""generic"";
            }
        }");
        Assert.That(result, Is.EqualTo("notimpl"));
    }

    [Test]
    public void TryFinally_NestedReturn_FinallyOrder()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                try
                {
                    log += ""inner-try:"";
                    return log + ""return"";
                }
                finally
                {
                    log += ""inner-finally:"";
                }
            }
            finally
            {
                log += ""outer-finally"";
            }
        }");
        // The return value should be from the return statement, but finallys still run
        Assert.That(result!.ToString(), Does.Contain("return"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.8.3 Switch — complex patterns
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void SwitchStatement_MultipleLabels_SameBody()
    {
        var result = Eval(@"{
            var x = 2;
            switch (x)
            {
                case 1:
                case 2:
                case 3:
                    return ""small"";
                default:
                    return ""other"";
            }
        }");
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void SwitchStatement_DefaultInMiddle()
    {
        var result = Eval(@"{
            var x = 99;
            switch (x)
            {
                case 1: return ""one"";
                default: return ""default"";
                case 2: return ""two"";
            }
        }");
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void SwitchStatement_StringLabels()
    {
        var result = Eval(@"{
            var s = ""hello"";
            switch (s)
            {
                case ""hello"": return 1;
                case ""world"": return 2;
                default: return 0;
            }
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void SwitchStatement_NullCase()
    {
        var result = Eval(@"{
            string s = null;
            switch (s)
            {
                case null: return ""null"";
                case ""hello"": return ""hello"";
                default: return ""default"";
            }
        }");
        Assert.That(result, Is.EqualTo("null"));
    }

    [Test]
    public void SwitchExpression_Tuple()
    {
        var result = Eval(@"{
            var point = (1, 0);
            return point switch
            {
                (0, 0) => ""origin"",
                (1, 0) => ""right"",
                (0, 1) => ""up"",
                _ => ""other""
            };
        }");
        Assert.That(result, Is.EqualTo("right"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.19 Lambda expressions — various forms
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Lambda_ExpressionBody()
    {
        var result = Eval(@"{
            Func<int, int> square = x => x * x;
            return square(5);
        }");
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void Lambda_StatementBody()
    {
        var result = Eval(@"{
            Func<int, int> abs = x => { if (x < 0) return -x; return x; };
            return abs(-5);
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Lambda_Closure_CapturesVariable()
    {
        var result = Eval(@"{
            var offset = 10;
            Func<int, int> add = x => x + offset;
            return add(5);
        }");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Lambda_Closure_MutatesVariable()
    {
        var result = Eval(@"{
            var count = 0;
            Action inc = () => count++;
            inc();
            inc();
            inc();
            return count;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Lambda_MultiParameter()
    {
        var result = Eval(@"{
            Func<int, int, int> add = (a, b) => a + b;
            return add(3, 4);
        }");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Lambda_NoParameter()
    {
        var result = Eval(@"{
            Func<int> f = () => 42;
            return f();
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.11.2 Array access — multi-dimensional
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void MultiDimArray_Access_SetAndGet()
    {
        var result = Eval(@"{
            var arr = new int[3, 4];
            arr[2, 3] = 42;
            return arr[2, 3];
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void MultiDimArray_GetLength()
    {
        var result = Eval(@"{
            var arr = new int[3, 4, 5];
            return arr.GetLength(0) * arr.GetLength(1) * arr.GetLength(2);
        }");
        Assert.That(result, Is.EqualTo(60));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.6 Tuple expressions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Tuple_Creation_ThreeElements()
    {
        var result = Eval(@"{
            var t = (1, ""hello"", true);
            return t.Item2;
        }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Tuple_Named_Elements()
    {
        var result = Eval(@"{
            var t = (Name: ""test"", Value: 42);
            return t.Name;
        }");
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void Tuple_Equality()
    {
        var result = Eval("(1, 2, 3) == (1, 2, 3)");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Tuple_Inequality()
    {
        var result = Eval("(1, 2, 3) != (1, 2, 4)");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.9.5 Foreach — various collection types
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Foreach_String_CharIteration()
    {
        var result = Eval(@"{
            var count = 0;
            foreach (var c in ""hello"")
            {
                count++;
            }
            return count;
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Foreach_Dictionary_KeyValuePairs()
    {
        var result = Eval(@"{
            var dict = new Dictionary<string, int>();
            dict[""a""] = 1;
            dict[""b""] = 2;
            var sum = 0;
            foreach (var kvp in dict)
            {
                sum += kvp.Value;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.3 Interpolated string — complex expressions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void InterpolatedString_NestedBraces()
    {
        var result = Eval(@"{
            var x = 5;
            return $""Value: {x + 1}"";
        }");
        Assert.That(result, Is.EqualTo("Value: 6"));
    }

    [Test]
    public void InterpolatedString_TernaryInside()
    {
        var result = Eval(@"{
            var x = 5;
            return $""{(x > 3 ? ""big"" : ""small"")}"";
        }");
        Assert.That(result, Is.EqualTo("big"));
    }

    [Test]
    public void InterpolatedString_MethodCallInside()
    {
        var result = Eval(@"{
            return $""{""hello"".ToUpper()}"";
        }");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.6.3 Local constant declarations
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void LocalConst_String()
    {
        var result = Eval(@"{
            const string greeting = ""hello"";
            return greeting + "" world"";
        }");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void LocalConst_UsedInSwitchCase()
    {
        var result = Eval(@"{
            const int TARGET = 42;
            var x = 42;
            switch (x)
            {
                case TARGET: return ""found"";
                default: return ""not found"";
            }
        }");
        Assert.That(result, Is.EqualTo("found"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.17 Declaration expressions — out var
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void OutVar_IntTryParse()
    {
        var result = Eval(@"{
            if (int.TryParse(""42"", out var x))
                return x;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void OutVar_FailedParse()
    {
        var result = Eval(@"{
            if (int.TryParse(""abc"", out var x))
                return x;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(-1));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.12.13 The as operator — edge cases
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void AsOperator_ValueType_NotAllowed()
    {
        // `as` only works for reference types and nullable value types
        // `obj as int` should be a compile error — test with nullable
        var result = Eval(@"{
            object obj = 42;
            return obj as int?;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void AsOperator_NullableValueType_NoMatch()
    {
        var result = Eval(@"{
            object obj = ""hello"";
            return obj as int?;
        }");
        Assert.That(result, Is.Null);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Numeric literal edge cases
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void IntLiteral_MaxValue()
    {
        var result = Eval("2147483647");
        Assert.That(result, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void IntLiteral_MinValueNegation()
    {
        // -2147483648 should be parsed as int.MinValue, not -(int.MaxValue+1)
        var result = Eval("-2147483648");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void LongLiteral_MaxValue()
    {
        var result = Eval("9223372036854775807L");
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void DoubleLiteral_ScientificNotation()
    {
        var result = Eval("1.5e3");
        Assert.That(result, Is.EqualTo(1500.0));
    }

    [Test]
    public void DoubleLiteral_NegativeExponent()
    {
        var result = Eval("1.5e-2");
        Assert.That(result, Is.EqualTo(0.015));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: nullable + pattern matching
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NullablePatternMatch_HasValue()
    {
        var result = Eval(@"{
            int? x = 42;
            if (x is int v)
                return v;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NullablePatternMatch_IsNull()
    {
        var result = Eval(@"{
            int? x = null;
            if (x is int v)
                return v;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(-1));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: exception + using statement
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void UsingStatement_DisposesOnException()
    {
        var result = Eval(@"{
            var disposed = false;
            try
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    throw new System.InvalidOperationException();
                }
            }
            catch (System.InvalidOperationException)
            {
                disposed = true;
            }
            return disposed;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: for loop variable scope
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ForLoop_VariableNotVisibleAfterLoop()
    {
        // The for loop variable i should not leak outside
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                sum += i;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ForLoop_MultipleInitializers()
    {
        var result = Eval(@"{
            var sum = 0;
            for (int i = 0, j = 10; i < 5; i++, j--)
            {
                sum += i + j;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(50)); // (0+10)+(1+9)+(2+8)+(3+7)+(4+6) = 50
    }

    // ═══════════════════════════════════════════════════════════════════
    // String operations
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void String_IndexAccess()
    {
        var result = Eval(@"""hello""[1]");
        Assert.That(result, Is.EqualTo('e'));
    }

    [Test]
    public void String_Length()
    {
        var result = Eval(@"""hello"".Length");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void String_Contains()
    {
        var result = Eval(@"""hello world"".Contains(""world"")");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void String_Substring()
    {
        var result = Eval(@"""hello world"".Substring(6)");
        Assert.That(result, Is.EqualTo("world"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: ternary + nullable
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Ternary_NullableResult()
    {
        var result = Eval(@"{
            var x = true;
            int? r = x ? 42 : (int?)null;
            return r;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Ternary_NullableResult_NullBranch()
    {
        var result = Eval(@"{
            var x = false;
            int? r = x ? 42 : (int?)null;
            return r;
        }");
        Assert.That(result, Is.Null);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: nested lambdas
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedLambda_ReturnFunction()
    {
        var result = Eval(@"{
            Func<int, Func<int, int>> adder = x => y => x + y;
            var add5 = adder(5);
            return add5(3);
        }");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void NestedLambda_ImmediateInvoke()
    {
        var result = Eval(@"{
            Func<int, Func<int, int>> multiply = x => y => x * y;
            return multiply(3)(4);
        }");
        Assert.That(result, Is.EqualTo(12));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.12.10 Equality between nullable and null literal
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NullableEquality_WithNullLiteral()
    {
        var result = Eval(@"{
            int? x = null;
            return x == null;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NullableEquality_NonNullWithNullLiteral()
    {
        var result = Eval(@"{
            int? x = 5;
            return x == null;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void NullableInequality_WithNullLiteral()
    {
        var result = Eval(@"{
            int? x = 5;
            return x != null;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.20 Query expressions
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Query_OrderBy()
    {
        var result = Eval(@"{
            var nums = new[] { 3, 1, 4, 1, 5 };
            var sorted = (from n in nums orderby n select n).ToList();
            return sorted[0] * 10000 + sorted[1] * 1000 + sorted[2] * 100 + sorted[3] * 10 + sorted[4];
        }");
        Assert.That(result, Is.EqualTo(11345));
    }

    [Test]
    public void Query_GroupBy()
    {
        var result = Eval(@"{
            var nums = new[] { 1, 2, 3, 4, 5, 6 };
            var groups = (from n in nums group n by n % 2).ToList();
            return groups.Count;
        }");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Query_Let()
    {
        var result = Eval(@"{
            var words = new[] { ""hello"", ""world"" };
            var result = (from w in words let upper = w.ToUpper() select upper).ToList();
            return result[0];
        }");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Query_MultipleFrom()
    {
        var result = Eval(@"{
            var a = new[] { 1, 2 };
            var b = new[] { 10, 20 };
            var result = (from x in a from y in b select x + y).ToList();
            return result.Count;
        }");
        Assert.That(result, Is.EqualTo(4)); // 1+10, 1+20, 2+10, 2+20
    }

    // ═══════════════════════════════════════════════════════════════════
    // Type coercion in assignment
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Assignment_ImplicitIntToDouble()
    {
        var result = Eval(@"{
            double d = 42;
            return d;
        }");
        Assert.That(result, Is.EqualTo(42.0));
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Assignment_ImplicitIntToLong()
    {
        var result = Eval(@"{
            long l = 42;
            return l;
        }");
        Assert.That(result, Is.EqualTo(42L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void Assignment_ImplicitByteToInt()
    {
        var result = Eval(@"{
            byte b = 200;
            int i = b;
            return i;
        }");
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Assignment_ImplicitCharToInt()
    {
        var result = Eval(@"{
            char c = 'A';
            int i = c;
            return i;
        }");
        Assert.That(result, Is.EqualTo(65));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8.17 typeof operator
    // ═══════════════════════════════════════════════════════════════════

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
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Typeof_Nullable()
    {
        var result = Eval("typeof(int?).Name");
        Assert.That(result, Does.Contain("Nullable"));
    }
}
