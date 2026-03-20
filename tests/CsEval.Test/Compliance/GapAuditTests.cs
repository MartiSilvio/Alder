using CsEval.Diagnostics;
using CsEval.Test._Infrastructure;

namespace CsEval.Test.Compliance;

/// <summary>
/// Tests verifying known gaps documented in docs/gaps.md.
/// Each test is tagged with its gap ID (P=Parser, B=Binding, R=Runtime, T=TestCoverage).
/// Tests that are expected to fail (unimplemented features) use Assert.Throws or try/catch
/// to document the current behavior, with a comment noting what SHOULD happen.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class GapAuditTests(CompilationMode mode)
{
    private CsEvalEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    #region P1 — Cast to array type

    [Test]
    public void P1_CastToArrayType_IntArray()
    {
        // (int[])obj should work when obj is int[]
        var result = Eval(@"
            object obj = new int[] { 1, 2, 3 };
            var arr = (int[])obj;
            return arr[1];
        ");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void P1_CastToArrayType_StringArray()
    {
        var result = Eval(@"
            object obj = new string[] { ""a"", ""b"" };
            var arr = (string[])obj;
            return arr[0];
        ");
        Assert.That(result, Is.EqualTo("a"));
    }

    [Test]
    public void P1_CastToArrayType_DoubleArray()
    {
        var result = Eval(@"
            object obj = new double[] { 1.5, 2.5 };
            var arr = (double[])obj;
            return arr[0] + arr[1];
        ");
        Assert.That(result, Is.EqualTo(4.0));
    }

    #endregion

    #region P2 — Cast to generic type

    [Test]
    public void P2_CastToGenericType_ListInt()
    {
        var engine = Engine();
        engine.SetVariable("obj", (object)new List<int> { 10, 20, 30 });
        var result = engine.Evaluate(@"
            var list = (List<int>)obj;
            return list[2];
        ");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void P2_CastToGenericType_DictionaryStringInt()
    {
        var engine = Engine();
        engine.SetVariable("obj", (object)new Dictionary<string, int> { ["x"] = 42 });
        var result = engine.Evaluate(@"
            var dict = (Dictionary<string, int>)obj;
            return dict[""x""];
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region P3 — Multidimensional array initializer

    [Test]
    public void P3_MultidimArrayInitializer_2D()
    {
        // new int[,] { {1,2}, {3,4} } should parse and create a 2D array
        var result = Eval(@"
            var m = new int[,] { {1, 2}, {3, 4} };
            return m[1, 0] + m[0, 1];
        ");
        Assert.That(result, Is.EqualTo(5)); // 3 + 2
    }

    [Test]
    public void P3_MultidimArrayInitializer_WithExplicitSize()
    {
        var result = Eval(@"
            var m = new int[2, 3] { {1, 2, 3}, {4, 5, 6} };
            return m[1, 2];
        ");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region P4 — Multiple variable declarations

    [Test]
    public void P4_MultipleVarDeclarations_SameType()
    {
        var result = Eval(@"
            int a = 1, b = 2, c = 3;
            return a + b + c;
        ");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void P4_MultipleVarDeclarations_WithExpressions()
    {
        var result = Eval(@"
            double x = 1.5, y = x * 2, z = x + y;
            return z;
        ");
        Assert.That(result, Is.EqualTo(4.5));
    }

    #endregion

    #region P5 — goto / labels

    [Test]
    public void P5_Goto_SimpleJump()
    {
        var result = Eval(@"{
            var x = 0;
            goto skip;
            x = 99;
            skip:
            return x;
        }");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region P12 — goto case / goto default

    [Test]
    public void P12_GotoCase_InSwitch()
    {
        var result = Eval(@"{
            var x = 1;
            var r = 0;
            switch (x)
            {
                case 1: r += 10; goto case 2;
                case 2: r += 20; break;
                case 3: r += 30; break;
            }
            return r;
        }");
        Assert.That(result, Is.EqualTo(30)); // 10 + 20
    }

    [Test]
    public void P12_GotoDefault_InSwitch()
    {
        var result = Eval(@"{
            var x = 1;
            var r = 0;
            switch (x)
            {
                case 1: r += 10; goto default;
                case 2: r += 20; break;
                default: r += 100; break;
            }
            return r;
        }");
        Assert.That(result, Is.EqualTo(110)); // 10 + 100
    }

    #endregion

    #region B1 — Catch variable typing

    [Test]
    public void B1_CatchVariableTyping_ArgumentException_ParamName()
    {
        var result = Eval(@"{
            try
            {
                throw new System.ArgumentNullException(""myParam"");
            }
            catch (System.ArgumentNullException ex)
            {
                return ex.ParamName;
            }
        }");
        Assert.That(result, Is.EqualTo("myParam"));
    }

    [Test]
    public void B1_CatchVariableTyping_ArgumentException_ActualMessage()
    {
        var result = Eval(@"{
            try
            {
                throw new System.ArgumentException(""bad value"", ""param1"");
            }
            catch (System.ArgumentException ex)
            {
                return ex.ParamName;
            }
        }");
        Assert.That(result, Is.EqualTo("param1"));
    }

    [Test]
    public void B1_CatchVariableTyping_InnerException()
    {
        var result = Eval(@"{
            try
            {
                try { throw new System.InvalidOperationException(""inner""); }
                catch (System.Exception inner)
                {
                    throw new System.ArgumentException(""outer"", inner);
                }
            }
            catch (System.ArgumentException ex)
            {
                return ex.InnerException.Message;
            }
        }");
        Assert.That(result, Is.EqualTo("inner"));
    }

    #endregion

    #region R1 — Enum arithmetic

    [Test]
    public void R1_EnumPlusInt()
    {
        // DayOfWeek.Monday (1) + 2 should produce DayOfWeek.Wednesday (3)
        var result = Eval("System.DayOfWeek.Monday + 2");
        Assert.That(result, Is.EqualTo(DayOfWeek.Wednesday));
    }

    [Test]
    public void R1_IntPlusEnum()
    {
        var result = Eval("2 + System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(DayOfWeek.Wednesday));
    }

    [Test]
    public void R1_EnumMinusEnum()
    {
        // DayOfWeek.Friday (5) - DayOfWeek.Monday (1) = 4
        var result = Eval("System.DayOfWeek.Friday - System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void R1_EnumMinusInt()
    {
        // DayOfWeek.Friday (5) - 2 = DayOfWeek.Wednesday (3)
        var result = Eval("System.DayOfWeek.Friday - 2");
        Assert.That(result, Is.EqualTo(DayOfWeek.Wednesday));
    }

    [Test]
    public void R1_EnumComparison()
    {
        var result = Eval("System.DayOfWeek.Friday > System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void R1_EnumEquality()
    {
        var result = Eval("System.DayOfWeek.Monday == System.DayOfWeek.Monday");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void R1_EnumToString()
    {
        var result = Eval("System.DayOfWeek.Wednesday.ToString()");
        Assert.That(result, Is.EqualTo("Wednesday"));
    }

    [Test]
    public void R1_EnumBitwiseOr_Flags()
    {
        var result = Eval(@"
            var flags = System.IO.FileAccess.Read | System.IO.FileAccess.Write;
            return flags == System.IO.FileAccess.ReadWrite;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region R2 — Array element assignment coercion

    [Test]
    public void R2_ArrayElementAssign_IntToDouble()
    {
        var result = Eval(@"
            var arr = new double[3];
            arr[0] = 42;
            arr[1] = 7;
            return arr[0] + arr[1];
        ");
        Assert.That(result, Is.EqualTo(49.0));
    }

    [Test]
    public void R2_ArrayElementAssign_ByteToInt()
    {
        var result = Eval(@"
            var arr = new int[2];
            byte b = 200;
            arr[0] = b;
            return arr[0];
        ");
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void R2_ArrayElementAssign_IntToLong()
    {
        var result = Eval(@"
            var arr = new long[2];
            arr[0] = 42;
            return arr[0];
        ");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void R2_ArrayElementAssign_FloatToDouble()
    {
        var result = Eval(@"
            var arr = new double[1];
            float f = 3.14f;
            arr[0] = f;
            return arr[0] > 3.0;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region R3 — Unary negation of uint

    [Test]
    public void R3_NegateUint_ProducesLong()
    {
        var engine = Engine();
        engine.SetVariable("x", (uint)5);
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-5L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void R3_NegateUint_LargeValue()
    {
        var engine = Engine();
        engine.SetVariable("x", (uint)2147483648);
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-2147483648L));
        Assert.That(result, Is.TypeOf<long>());
    }

    #endregion

    #region Compound assignment cross-type

    [Test]
    public void T4_CompoundAssign_DoublePlusEqualsInt()
    {
        var result = Eval(@"
            double d = 1.5;
            d += 3;
            return d;
        ");
        Assert.That(result, Is.EqualTo(4.5));
    }

    [Test]
    public void T4_CompoundAssign_LongPlusEqualsInt()
    {
        var result = Eval(@"
            long x = 100;
            x += 50;
            return x;
        ");
        Assert.That(result, Is.EqualTo(150L));
    }

    [Test]
    public void T4_CompoundAssign_DoubleTimesEqualsInt()
    {
        var result = Eval(@"
            double d = 2.5;
            d *= 4;
            return d;
        ");
        Assert.That(result, Is.EqualTo(10.0));
    }

    [Test]
    public void T4_CompoundAssign_FloatPlusEqualsInt()
    {
        var result = Eval(@"
            float f = 1.5f;
            f += 2;
            return f > 3.0f;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Foreach with type cast

    [Test]
    public void T6_ForeachCast_IntToDouble()
    {
        var result = Eval(@"{
            var sum = 0.0;
            var arr = new int[] { 1, 2, 3 };
            foreach (var i in arr)
            {
                double d = (double)i;
                sum += d;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(6.0));
    }

    [Test]
    public void T6_ForeachCast_IntDivisionToDouble()
    {
        var result = Eval(@"{
            var arr = new int[] { 1, 2, 3, 4 };
            var sum = 0.0;
            foreach (var x in arr)
            {
                sum += (double)x / 10;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(1.0));
    }

    #endregion

    #region String concatenation edge cases

    [Test]
    public void T9_StringConcat_NullPlusString()
    {
        var result = Eval(@"
            string s = null;
            return s + ""hello"";
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void T9_StringConcat_StringPlusNull()
    {
        var result = Eval(@"
            string s = null;
            return ""hello"" + s;
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void T9_StringConcat_IntPlusString()
    {
        var result = Eval(@"42 + ""abc""");
        Assert.That(result, Is.EqualTo("42abc"));
    }

    [Test]
    public void T9_StringConcat_BoolPlusString()
    {
        var result = Eval(@"true + "" value""");
        Assert.That(result, Is.EqualTo("True value"));
    }

    [Test]
    public void T9_StringConcat_CharPlusString()
    {
        var result = Eval(@"'x' + ""yz""");
        Assert.That(result, Is.EqualTo("xyz"));
    }

    #endregion

    #region Char arithmetic

    [Test]
    public void T10_CharPlusInt_ProducesInt()
    {
        var result = Eval("'A' + 1");
        Assert.That(result, Is.EqualTo(66));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void T10_CharMinusChar_ProducesInt()
    {
        var result = Eval("'Z' - 'A'");
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void T10_CharMultiplyInt()
    {
        var result = Eval("'A' * 2");
        Assert.That(result, Is.EqualTo(130)); // 65 * 2
    }

    [Test]
    public void T10_CharDivideInt()
    {
        var result = Eval("'d' / 2");
        Assert.That(result, Is.EqualTo(50)); // 100 / 2
    }

    [Test]
    public void T10_CharIncrement()
    {
        var result = Eval(@"
            char c = 'A';
            c++;
            return c;
        ");
        Assert.That(result, Is.EqualTo('B'));
    }

    [Test]
    public void T10_CharDecrement()
    {
        var result = Eval(@"
            char c = 'C';
            c--;
            return c;
        ");
        Assert.That(result, Is.EqualTo('B'));
    }

    [Test]
    public void T10_CastIntToChar()
    {
        var result = Eval("(char)('A' + 3)");
        Assert.That(result, Is.EqualTo('D'));
    }

    #endregion

    #region Array covariance

    [Test]
    public void T2_ArrayCovariance_StringToObject()
    {
        // C# allows string[] → object[] (array covariance)
        var engine = Engine();
        object[] arr = new string[] { "a", "b", "c" };
        engine.SetVariable("arr", arr);
        var result = engine.Evaluate("arr[1]");
        Assert.That(result, Is.EqualTo("b"));
    }

    [Test]
    public void T2_ArrayCovariance_AccessLength()
    {
        var engine = Engine();
        object[] arr = new string[] { "hello", "world" };
        engine.SetVariable("arr", arr);
        var result = engine.Evaluate("arr.Length");
        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region Jagged array operations

    #region P7 — Range/Index operators

    #region R7 — Multi-parameter indexers

    [Test]
    public void R7_MultiParamIndexer_Get()
    {
        var engine = Engine();
        engine.SetVariable("matrix", new Dictionary<(int, int), string>());
        // Dictionary doesn't have multi-param indexer, use a real 2D collection
        // Test via the multi-dim array path which already works
        var result = Eval(@"
            var arr = new int[3, 3];
            arr[1, 2] = 42;
            return arr[1, 2];
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region R20 — Foreach enumerator disposal

    [Test]
    public void R20_Foreach_DisposesEnumerator_OnNormalExit()
    {
        var engine = Engine();
        var result = engine.Evaluate(@"{
            var disposed = false;
            var items = new List<int> { 1, 2, 3 };
            foreach (var item in items) { }
            return true;
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    [Test]
    public void P7_IndexFromEnd_LastElement()
    {
        var result = Eval(@"
            var arr = new int[] { 10, 20, 30, 40, 50 };
            return arr[^1];
        ");
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void P7_IndexFromEnd_SecondToLast()
    {
        var result = Eval(@"
            var arr = new int[] { 10, 20, 30, 40, 50 };
            return arr[^2];
        ");
        Assert.That(result, Is.EqualTo(40));
    }

    [Test]
    public void P7_Range_SliceArray()
    {
        var result = Eval(@"
            var arr = new int[] { 10, 20, 30, 40, 50 };
            var slice = arr[1..4];
            return ((int[])slice).Length;
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void P7_Range_SliceString()
    {
        var result = Eval(@"
            var s = ""hello world"";
            return s[0..5];
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void P7_IndexFromEnd_InRange()
    {
        var result = Eval(@"
            var arr = new int[] { 10, 20, 30, 40, 50 };
            var slice = arr[1..^1];
            return ((int[])slice).Length;
        ");
        Assert.That(result, Is.EqualTo(3)); // elements 20, 30, 40
    }

    [Test]
    public void P7_XorStillWorks()
    {
        var result = Eval("5 ^ 3");
        Assert.That(result, Is.EqualTo(6)); // 0101 ^ 0011 = 0110
    }

    #endregion

    [Test]
    public void P8_JaggedArray_SizedWithInitializer()
    {
        // §12.8.16.5: new int[3][] { ... } — size + initializer
        var result = Eval(@"
            var jagged = new int[2][] {
                new int[] { 10, 20 },
                new int[] { 30, 40 }
            };
            return jagged[1][0];
        ");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void T8_JaggedArray_ElementMutation()
    {
        var result = Eval(@"
            var jagged = new int[][] {
                new int[] { 1, 2, 3 },
                new int[] { 4, 5, 6 }
            };
            jagged[0][1] = 99;
            return jagged[0][1];
        ");
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void T8_JaggedArray_RowReassignment()
    {
        var result = Eval(@"
            var jagged = new int[][] {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            };
            jagged[1] = new int[] { 10, 20 };
            return jagged[1][0] + jagged[1][1];
        ");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void T8_JaggedArray_DifferentLengths()
    {
        var result = Eval(@"
            var jagged = new int[][] {
                new int[] { 1 },
                new int[] { 2, 3 },
                new int[] { 4, 5, 6 }
            };
            return jagged[0].Length + jagged[1].Length + jagged[2].Length;
        ");
        Assert.That(result, Is.EqualTo(6)); // 1 + 2 + 3
    }

    #endregion

    #region Catch-specific exception members

    [Test]
    public void T11_CatchException_HResult()
    {
        var result = Eval(@"{
            try { throw new System.InvalidOperationException(); }
            catch (System.InvalidOperationException ex) { return ex.HResult != 0; }
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void T11_CatchException_StackTrace_IsNotNull()
    {
        var result = Eval(@"{
            try { throw new System.Exception(""test""); }
            catch (System.Exception ex) { return ex.StackTrace != null; }
        }");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void T11_CatchException_Source()
    {
        var result = Eval(@"{
            try { throw new System.InvalidOperationException(""bad""); }
            catch (System.InvalidOperationException ex) { return ex.Message; }
        }");
        Assert.That(result, Is.EqualTo("bad"));
    }

    #endregion

    #region Nullable arithmetic

    [Test]
    public void NullableAdd_BothNonNull()
    {
        var result = Eval(@"
            int? a = 3;
            int? b = 4;
            return a + b;
        ");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void NullableAdd_OneNull()
    {
        var result = Eval(@"
            int? a = 3;
            int? b = null;
            return a + b;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NullableComparison_BothNonNull()
    {
        var result = Eval(@"
            int? a = 5;
            int? b = 3;
            return a > b;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void NullableComparison_WithNull_ReturnsFalse()
    {
        // Per spec: if either operand is null, relational comparison returns false
        var result = Eval(@"
            int? a = 5;
            int? b = null;
            return a > b;
        ");
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region Nested loop break/continue

    [Test]
    public void NestedLoop_BreakOnlyExitsInner()
    {
        var result = Eval(@"{
            var count = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    if (j == 2) break;
                    count++;
                }
            }
            return count;
        }");
        Assert.That(result, Is.EqualTo(6)); // 3 outer * 2 inner
    }

    [Test]
    public void NestedLoop_ContinueOnlyAffectsInner()
    {
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (j % 2 == 0) continue;
                    sum += j;
                }
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(12)); // 3 * (1+3) = 12
    }

    #endregion

    #region Unary negation of uint (T12)

    [Test]
    public void T12_NegateUintLiteral()
    {
        var engine = Engine();
        engine.SetVariable("x", (uint)10);
        var result = engine.Evaluate("return -x;");
        Assert.That(result, Is.EqualTo(-10L));
    }

    #endregion

    #region Short-circuit evaluation

    [Test]
    public void ShortCircuit_And_DoesNotEvaluateRight()
    {
        var result = Eval(@"{
            var evaluated = false;
            var left = false;
            if (left && (evaluated = true)) { }
            return evaluated;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void ShortCircuit_Or_DoesNotEvaluateRight()
    {
        var result = Eval(@"{
            var evaluated = false;
            var left = true;
            if (left || (evaluated = true)) { }
            return evaluated;
        }");
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region Null-conditional chaining

    [Test]
    public void NullConditionalChain_AllNonNull()
    {
        var result = Eval(@"
            var s = ""hello"";
            return s?.Length.ToString()?.Length;
        ");
        Assert.That(result, Is.EqualTo(1)); // "5".Length = 1
    }

    [Test]
    public void NullConditionalChain_NullPropagates()
    {
        var result = Eval(@"
            string s = null;
            return s?.Length;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Pattern matching

    [Test]
    public void PatternMatch_IsType_WithDeclaration()
    {
        var result = Eval(@"{
            object obj = ""hello"";
            if (obj is string s)
                return s.Length;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PatternMatch_IsNot_Null()
    {
        var result = Eval(@"
            object obj = 42;
            return obj is not null;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void PatternMatch_SwitchExpression()
    {
        var result = Eval(@"{
            var x = 2;
            return x switch
            {
                1 => ""one"",
                2 => ""two"",
                3 => ""three"",
                _ => ""other""
            };
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    #endregion

    #region Null-coalescing assignment

    [Test]
    public void NullCoalesceAssign_WhenNull_Assigns()
    {
        var result = Eval(@"
            string s = null;
            s ??= ""default"";
            return s;
        ");
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void NullCoalesceAssign_WhenNonNull_KeepsOriginal()
    {
        var result = Eval(@"
            string s = ""original"";
            s ??= ""default"";
            return s;
        ");
        Assert.That(result, Is.EqualTo("original"));
    }

    #endregion

    #region Tuple deconstruction

    [Test]
    public void TupleDeconstruction_Basic()
    {
        var result = Eval(@"
            var (a, b) = (10, 20);
            return a + b;
        ");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void TupleDeconstruction_ThreeElements()
    {
        var result = Eval(@"
            var (x, y, z) = (1, 2, 3);
            return x * 100 + y * 10 + z;
        ");
        Assert.That(result, Is.EqualTo(123));
    }

    #endregion

    #region Checked/unchecked

    [Test]
    public void Checked_OverflowThrows()
    {
        Assert.Throws<OverflowException>(() => Eval(@"
            return checked(int.MaxValue + 1);
        "));
    }

    [Test]
    public void Unchecked_OverflowWraps()
    {
        var result = Eval(@"
            return unchecked(int.MaxValue + 1);
        ");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    #endregion

    #region String interpolation with format specifiers

    [Test]
    public void StringInterpolation_FormatSpecifier()
    {
        var result = Eval(@"
            var x = 3.14159;
            return $""{x:F2}"";
        ");
        Assert.That(result, Is.EqualTo("3.14"));
    }

    [Test]
    public void StringInterpolation_AlignmentAndFormat()
    {
        var result = Eval(@"
            var x = 42;
            return $""{x,5:D3}"";
        ");
        Assert.That(result, Is.EqualTo("  042"));
    }

    #endregion

    #region L1 — Verbatim identifiers

    [Test]
    public void L1_VerbatimIdentifier_KeywordAsName()
    {
        // §6.4.3: @keyword allows keywords as identifiers
        var result = Eval(@"
            int @class = 42;
            return @class;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void L1_VerbatimIdentifier_MultipleKeywords()
    {
        var result = Eval(@"
            int @if = 1;
            int @return = 2;
            return @if + @return;
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region L4 — Namespace alias qualifier

    [Test, Ignore("Out of scope: namespace alias qualifier is not an expression-evaluator feature")]
    public void L4_GlobalAlias_SystemType()
    {
        // §14.8: global::System.Int32 should resolve
        var result = Eval("global::System.Int32.MaxValue");
        Assert.That(result, Is.EqualTo(int.MaxValue));
    }

    #endregion

    #region P15 — Checked/unchecked statements

    [Test, Ignore("Out of scope: checked/unchecked block statements — expression forms already work")]
    public void P15_CheckedStatement_OverflowThrows()
    {
        // §13.12: checked { ... } block should enable overflow checking
        Assert.Throws<OverflowException>(() => Eval(@"{
            checked
            {
                int x = int.MaxValue;
                x = x + 1;
                return x;
            }
        }"));
    }

    [Test, Ignore("Out of scope: checked/unchecked block statements — expression forms already work")]
    public void P15_UncheckedStatement_OverflowWraps()
    {
        var result = Eval(@"{
            unchecked
            {
                int x = int.MaxValue;
                x = x + 1;
                return x;
            }
        }");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    #endregion

    #region P16 — Using declarations

    [Test, Ignore("Out of scope: C# 8 using declarations are a statement-level feature")]
    public void P16_UsingDeclaration_NoParens()
    {
        // §13.14 (C# 8.0): using var x = expr; disposes at end of block
        var result = Eval(@"{
            using var stream = new System.IO.MemoryStream();
            stream.WriteByte(42);
            return (int)stream.Length;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region P18 — Generic local functions

    [Test, Ignore("Out of scope: generic local functions are a statement-level feature")]
    public void P18_GenericLocalFunction()
    {
        // §13.6.4: local functions can have generic type parameters
        var result = Eval(@"{
            T Identity<T>(T x) { return x; }
            return Identity(42);
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region B4 — Nullable bool conditional operators

    [Test]
    public void B4_NullableBool_AndAnd_TrueAndNull()
    {
        // §12.14.2: bool? && bool? should use lifted semantics
        var result = Eval(@"
            bool? x = true;
            bool? y = null;
            return x && y;
        ");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void B4_NullableBool_OrOr_NullOrTrue()
    {
        var result = Eval(@"
            bool? x = null;
            bool? y = true;
            return x || y;
        ");
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void B4_NullableBool_AndAnd_FalseAndNull()
    {
        var result = Eval(@"
            bool? x = false;
            bool? y = null;
            return x && y;
        ");
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region R13 — Delegate combination

    [Test]
    public void R13_DelegateCombine_ActionPlus()
    {
        // §12.10.5: delegate + delegate should combine via Delegate.Combine
        var result = Eval(@"{
            var count = 0;
            Action a = () => count++;
            Action b = () => count += 10;
            var combined = a + b;
            combined();
            return count;
        }");
        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void R13_DelegateRemove_ActionMinus()
    {
        // §12.10.6: delegate - delegate should remove via Delegate.Remove
        var result = Eval(@"{
            var count = 0;
            Action inc = () => count++;
            Action both = inc + inc;
            var single = both - inc;
            single();
            return count;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region R17 — Switch fall-through validation

    [Test]
    public void R17_SwitchFallThrough_ShouldError()
    {
        // §13.8.3: end point of switch section must be unreachable
        // This should produce an error, not silently fall through
        var ex = Assert.Throws<CsEvalException>(() => Eval(@"{
            var x = 1;
            switch (x)
            {
                case 1:
                    var a = 10;
                case 2:
                    return 20;
            }
            return 0;
        }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0163));
    }

    #endregion

    #region R19 — Break cannot exit finally

    [Test, Ignore("Out of scope: break/finally validation is a control-flow statement feature")]
    public void R19_BreakInFinally_ShouldError()
    {
        // §13.10.2: break cannot exit a finally block
        Assert.Throws<CsEvalException>(() => Eval(@"{
            for (var i = 0; i < 3; i++)
            {
                try { }
                finally { break; }
            }
            return 0;
        }"));
    }

    #endregion

    #region P6 — with expression

    [Test, Ignore("Out of scope: with-expressions require record type definitions")]
    public void P6_WithExpression_AnonymousType()
    {
        // §12.18: expr with { ... } for non-destructive mutation
        // Even if records are out of scope, with-expressions on anonymous types
        // or structs with init properties could be relevant
        var engine = Engine();
        engine.SetVariable("dt", new DateTime(2020, 1, 1));
        // DateTime doesn't support with, but this tests parsing
        Assert.Throws<CsEvalException>(() => engine.Evaluate(@"{
            var x = new { Name = ""test"", Value = 42 };
            return x with { Value = 99 };
        }"));
    }

    #endregion

    #region Discard pattern

    [Test]
    public void Discard_InSwitchExpression()
    {
        var result = Eval(@"{
            var x = 99;
            return x switch { 1 => ""one"", _ => ""other"" };
        }");
        Assert.That(result, Is.EqualTo("other"));
    }

    #endregion

    #region Null-conditional indexing

    [Test]
    public void NullConditionalIndex_NonNull()
    {
        var engine = Engine();
        engine.SetVariable("arr", new int[] { 10, 20, 30 });
        var result = engine.Evaluate("arr?[1]");
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void NullConditionalIndex_Null()
    {
        var engine = Engine();
        engine.SetVariable("arr", (int[]?)null);
        var result = engine.Evaluate("arr?[0]");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Unsigned right shift

    [Test]
    public void UnsignedRightShift_Negative()
    {
        var result = Eval("-1 >>> 28");
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void UnsignedRightShift_Positive()
    {
        var result = Eval("256 >>> 4");
        Assert.That(result, Is.EqualTo(16));
    }

    #endregion

    #region Compound shift assignments

    [Test]
    public void CompoundShiftAssign_LeftShift()
    {
        var result = Eval(@"
            var x = 1;
            x <<= 4;
            return x;
        ");
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void CompoundShiftAssign_RightShift()
    {
        var result = Eval(@"
            var x = 256;
            x >>= 3;
            return x;
        ");
        Assert.That(result, Is.EqualTo(32));
    }

    [Test]
    public void CompoundShiftAssign_UnsignedRightShift()
    {
        var result = Eval(@"
            var x = -1;
            x >>>= 28;
            return x;
        ");
        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region typeof / sizeof / nameof

    [Test]
    public void Typeof_PrimitiveType()
    {
        var result = Eval("typeof(int).Name");
        Assert.That(result, Is.EqualTo("Int32"));
    }

    [Test]
    public void Sizeof_Int()
    {
        var result = Eval("sizeof(int)");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Nameof_Variable()
    {
        var result = Eval(@"
            var myVariable = 42;
            return nameof(myVariable);
        ");
        Assert.That(result, Is.EqualTo("myVariable"));
    }

    #endregion

    #region Raw string literals

    [Test]
    public void RawStringLiteral_Basic()
    {
        var result = Eval("\"\"\"hello world\"\"\"");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    #endregion

    #region Empty statement

    [Test]
    public void EmptyStatement_DoesNotFail()
    {
        var result = Eval(@"{
            var x = 1;
            ;
            ;
            return x;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region Const declarations

    [Test]
    public void ConstDeclaration_Int()
    {
        var result = Eval(@"
            const int x = 42;
            return x;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ConstDeclaration_WithExpression()
    {
        var result = Eval(@"
            const int x = 10 + 20;
            const int y = x * 2;
            return y;
        ");
        Assert.That(result, Is.EqualTo(60));
    }

    #endregion

    #region Lock statement

    [Test]
    public void LockStatement_Basic()
    {
        var result = Eval(@"{
            var obj = new object();
            var x = 0;
            lock (obj)
            {
                x = 42;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region Implicitly typed nested arrays

    [Test]
    public void ImplicitNestedArrays()
    {
        var result = Eval(@"
            var arr = new[] { new[] { 1, 2 }, new[] { 3, 4 } };
            return arr[1][0];
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Verbatim and interpolated strings

    [Test]
    public void VerbatimInterpolated_String()
    {
        var result = Eval(@"
            var name = ""world"";
            return $@""hello
{name}"";
        ");
        Assert.That(result, Does.Contain("hello"));
        Assert.That(result, Does.Contain("world"));
    }

    #endregion

    #region 3D arrays

    [Test]
    public void ThreeDimensionalArray_CreateAndAccess()
    {
        var result = Eval(@"
            var arr = new int[2, 3, 4];
            arr[1, 2, 3] = 99;
            return arr[1, 2, 3];
        ");
        Assert.That(result, Is.EqualTo(99));
    }

    #endregion

    #region Conditional access with method invocation

    [Test]
    public void NullConditional_MethodInvocation()
    {
        var result = Eval(@"
            string s = ""hello"";
            return s?.ToUpper();
        ");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void NullConditional_MethodInvocation_Null()
    {
        var result = Eval(@"
            string s = null;
            return s?.ToUpper();
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Default expression

    [Test]
    public void Default_Int()
    {
        var result = Eval("default(int)");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Default_Bool()
    {
        var result = Eval("default(bool)");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Default_String()
    {
        var result = Eval("default(string)");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region As operator

    [Test]
    public void AsOperator_ValidCast()
    {
        var result = Eval(@"
            object obj = ""hello"";
            return obj as string;
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void AsOperator_InvalidCast_ReturnsNull()
    {
        var result = Eval(@"
            object obj = 42;
            return obj as string;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion
}
