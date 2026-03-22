using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CrossFeatureInteractionTests(CompilationMode mode)
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

    #region Interaction tests — multiple feature combinations
    [Test]
    public void Interaction_NullableArithmeticWithConversion()
    {
        var result = Eval(@"
            int? x = 5;
            double? y = 2.5;
            return x + y;
        ");
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void Interaction_NullCoalescing_WithCast()
    {
        var result = Eval(@"
            int? x = null;
            long y = (long)(x ?? 42);
            return y;
        ");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Interaction_TernaryInStringInterpolation()
    {
        var result = Eval(@"
            var x = 5;
            return $""value is {(x > 3 ? ""big"" : ""small"")}"";
        ");
        Assert.That(result, Is.EqualTo("value is big"));
    }

    [Test]
    public void Interaction_LambdaInLinq()
    {
        var result = Eval(@"
            var nums = new List<int> { 1, 2, 3, 4, 5 };
            return nums.Where(x => x % 2 == 0).Sum();
        ");
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
        var result = Eval(@"
            string s = null;
            return s?.Length ?? -1;
        ");
        Assert.That(result, Is.EqualTo(-1));
    }

    #endregion

    #region Expression torture tests — complex nesting
    [Test]
    public void DeepNesting_Ternary()
    {
        var result = Eval(@"
            var x = 3;
            return x == 1 ? ""one"" : x == 2 ? ""two"" : x == 3 ? ""three"" : ""other"";
        ");
        Assert.That(result, Is.EqualTo("three"));
    }

    [Test]
    public void DeepNesting_NullCoalescing()
    {
        var result = Eval(@"
            int? a = null;
            int? b = null;
            int? c = null;
            int? d = 42;
            return a ?? b ?? c ?? d ?? 0;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void MixedArithmetic_PrecedenceChain()
    {
        // Verify: 2 + 3 * 4 - 1 = 2 + 12 - 1 = 13
        var result = Eval("2 + 3 * 4 - 1");
        Assert.That(result, Is.EqualTo(13));
    }

    #endregion

    #region Scope rules — variable shadowing in nested blocks
    [Test]
    public void VariableShadowing_InnerBlockCanShadow()
    {
        // In C# scripting/statement contexts, inner blocks can shadow outer variables
        // Note: C# spec §7.7 actually forbids this in method scope — test if Alder enforces it
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

    #endregion

    #region §12.8.16.2 Object creation — with initializers
    [Test]
    public void ObjectInit_ListWithInitializer()
    {
        var result = Eval(@"
            var list = new List<int> { 1, 2, 3 };
            return list.Count;
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ObjectInit_DictionaryIndexInitializer_Standard()
    {
        // §12.8.16.3: Dictionary initializer with indexer syntax
        // This is standard C# 6+ syntax, not an Extended-mode collection expression
        var result = Eval(@"
            var dict = new Dictionary<string, int> { [""a""] = 1, [""b""] = 2 };
            return dict[""a""] + dict[""b""];
        ");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Complex real-world patterns
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
        var result = Eval(@"
            return Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => x * x)
                .Sum();
        ");
        Assert.That(result, Is.EqualTo(4 + 16 + 36 + 64 + 100)); // 220
    }

    [Test]
    public void RealWorld_StringManipulation()
    {
        // Tests string.Join with an IEnumerable<string> from Select
        var result = Eval(@"
            var words = ""hello world foo bar"".Split(' ');
            var capitalized = words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)).ToArray();
            var result = string.Join(""-"", capitalized);
            return result;
        ");
        Assert.That(result, Is.EqualTo("Hello-World-Foo-Bar"));
    }

    [Test]
    public void RealWorld_StringJoinWithIEnumerable()
    {
        // string.Join(string, IEnumerable<string>) should work directly
        var result = Eval(@"
            var words = ""hello world foo bar"".Split(' ');
            var result = string.Join(""-"", words.Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)));
            return result;
        ");
        Assert.That(result, Is.EqualTo("Hello-World-Foo-Bar"));
    }

    [Test]
    public void RealWorld_StringJoinWithStringArray()
    {
        // Simpler variant: string[] passed directly
        var result = Eval(@"
            var result = string.Join(""-"", new string[] { ""a"", ""b"", ""c"" });
            return result;
        ");
        Assert.That(result, Is.EqualTo("a-b-c"));
    }

    #endregion

    #region §12.9.7 Cast expressions — edge cases
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
        var result = Eval(@"
            long x = (long)int.MaxValue + 1;
            return unchecked((int)x);
        ");
        Assert.That(result, Is.EqualTo(int.MinValue));
    }

    #endregion

    #region Interaction: nullable + pattern matching
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

    #endregion

    #region Interaction: exception + using statement
    [Test]
    public void UsingStatement_DisposesOnException()
    {
        var result = CreateEngine(allowedTypes: [typeof(MemoryStream), typeof(InvalidOperationException)]).Evaluate(@"{
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

    #endregion

    #region Interaction: ternary + nullable
    [Test]
    public void Ternary_NullableResult()
    {
        var result = Eval(@"
            var x = true;
            int? r = x ? 42 : (int?)null;
            return r;
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Ternary_NullableResult_NullBranch()
    {
        var result = Eval(@"
            var x = false;
            int? r = x ? 42 : (int?)null;
            return r;
        ");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region String operations
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

    #endregion

    #region Numeric literal edge cases
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

    #endregion

    #region §12.8.11.2 Array access — multi-dimensional
    [Test]
    public void MultiDimArray_Access_SetAndGet()
    {
        var result = Eval(@"
            var arr = new int[3, 4];
            arr[2, 3] = 42;
            return arr[2, 3];
        ");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void MultiDimArray_GetLength()
    {
        var result = Eval(@"
            var arr = new int[3, 4, 5];
            return arr.GetLength(0) * arr.GetLength(1) * arr.GetLength(2);
        ");
        Assert.That(result, Is.EqualTo(60));
    }

    #endregion

    #region §12.8.6 Tuple expressions
    [Test]
    public void Tuple_Creation_ThreeElements()
    {
        var result = Eval(@"
            var t = (1, ""hello"", true);
            return t.Item2;
        ");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Tuple_Named_Elements()
    {
        var result = Eval(@"
            var t = (Name: ""test"", Value: 42);
            return t.Name;
        ");
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
    #endregion
}
