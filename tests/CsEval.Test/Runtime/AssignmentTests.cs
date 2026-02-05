namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AssignmentTests(CompilationMode mode) 
{
    // Basic Assignment

    [Test]
    public void Assignment_SimpleVariable_UpdatesValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            x = 20;
            return x;
        }");

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Assignment_MultipleAssignments_TracksLatestValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            x = 2;
            x = 3;
            x = 4;
            return x;
        }");

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Assignment_ToExpressionResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            x = x + 10;
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Assignment_ChainedArithmetic_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            x = x + 1;
            x = x * 2;
            x = x - 1;
            return x;
        }");

        // 1 -> 2 -> 4 -> 3
        Assert.That(result, Is.EqualTo(3));
    }


    // Assignment with Different Types

    [Test]
    public void Assignment_StringValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""hello"";
            s = ""world"";
            return s;
        }");

        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void Assignment_BooleanValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var flag = true;
            flag = false;
            return flag;
        }");

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Assignment_NullValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = ""something"";
            obj = null;
            return obj;
        }");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Assignment_DoubleValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var d = 1.5;
            d = 3.14;
            return d;
        }");

        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Assignment_ArrayValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr = [4, 5, 6];
            return arr;
        }");

        Assert.That(result, Is.TypeOf<List<int>>());
        Assert.That(result, Is.EqualTo(new List<int> { 4, 5, 6 }));
    }

    [Test]
    public void Assignment_AnonymousObject_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj = new { Name = ""Jane"", Age = 30 };
            return obj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("Jane"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }


    // Assignment with External Variables

    [Test]
    public void Assignment_ToExternalVariable_UpdatesValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10L);

        var result = engine.Evaluate(@"
        {
            x = 50;
            return x;
        }");

        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public void Assignment_CombiningExternalAndLocal_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("multiplier", 3);

        var result = engine.Evaluate(@"
        {
            var total = 0;
            total = multiplier * 10;
            return total;
        }");

        Assert.That(result, Is.EqualTo(30));
    }


    // Assignment Expression Returns Value

    [Test]
    public void Assignment_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var y = x = 42;
            return y;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Assignment_ChainedAssignment_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 0;
            var c = 0;
            a = b = c = 100;
            return a + b + c;
        }");

        Assert.That(result, Is.EqualTo(300));
    }


    // Assignment in Conditionals

    [Test]
    public void Assignment_InsideIf_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            if (true) {
                x = 100;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void Assignment_InsideElse_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            if (false) {
                x = 50;
            } else {
                x = 100;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void Assignment_ConditionalBranches_UpdatesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("condition", true);

        var result = engine.Evaluate(@"
        {
            var msg = ""initial"";
            if (condition) {
                msg = ""was true"";
            } else {
                msg = ""was false"";
            }
            return msg;
        }");

        Assert.That(result, Is.EqualTo("was true"));
    }


    // Assignment with LINQ

    [Test]
    public void Assignment_WithLinqResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate(@"
        {
            var filtered = numbers.Where(x => x > 2).ToList();
            return filtered;
        }");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new List<int> { 3, 4, 5 }));
    }

    [Test]
    public void Assignment_AccumulatingLinqResults_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [1, 2, 3];
            items = [...items, 4, 5];
            var filtered = items.Where(x => x > 2).ToList();
            return filtered;
        }");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new List<int> { 3, 4, 5 }));
    }


    // Assignment with Modules

    [Test]
    public void Assignment_WithMathResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var absValue = 0.0;
            absValue = Math.Abs(-42.5);
            return absValue;
        }");

        Assert.That(result, Is.EqualTo(42.5));
    }

    [Test]
    public void Assignment_WithStringConcat_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var greeting = ""Hello"";
            greeting = greeting + "" World"";
            return greeting;
        }");

        Assert.That(result, Is.EqualTo("Hello World"));
    }


    // Assignment Scoping

    [Test]
    public void Assignment_InnerBlockModifiesOuter_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            if (true) {
                x = 2;
                if (true) {
                    x = 3;
                }
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Assignment_MultipleVariables_TracksIndependently()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 1;
            var b = 10;
            var c = 100;
            a = a + 1;
            b = b + 1;
            c = c + 1;
            return a + b + c;
        }");

        // 2 + 11 + 101 = 114
        Assert.That(result, Is.EqualTo(114));
    }


    // Assignment Error Cases

    [Test]
    public void Assignment_ToUndefinedVariable_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                undefinedVar = 10;
                return undefinedVar;
            }"));
    }


    // Assignment with Interpolated Strings

    [Test]
    public void Assignment_InterpolatedString_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var name = ""Alice"";
            var greeting = """";
            greeting = $""Hello, {name}!"";
            name = ""Bob"";
            greeting = $""Hello, {name}!"";
            return greeting;
        }");

        Assert.That(result, Is.EqualTo("Hello, Bob!"));
    }


    // Assignment with Ternary

    [Test]
    public void Assignment_FromTernary_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("condition", true);

        var result = engine.Evaluate(@"
        {
            var result = 0;
            result = condition ? 100 : 200;
            return result;
        }");

        Assert.That(result, Is.EqualTo(100));
    }


    // Assignment with Null Coalesce

    [Test]
    public void Assignment_FromNullCoalesce_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("maybeNull", null);

        var result = engine.Evaluate(@"
        {
            var fallback = 0;
            fallback = maybeNull ?? 42;
            return fallback;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Assignment_VsNullCoalesceAssign_BothWork()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            int? a = null;
            int? b = null;

            a = 10;
            b ??= 20;

            return a + b;
        }");

        Assert.That(result, Is.EqualTo(30));
    }


    // Pre-Parsed Assignment

    [Test]
    public void Assignment_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x = x * 2;
            return x;
        }");

        engine.SetVariable("startVal", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("startVal", 100L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(200));
    }


    // Compound Assignment - Basic Arithmetic

    [Test]
    public void CompoundAssignment_PlusEquals_Integer_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            x += 5;
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_MinusEquals_Integer_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 20;
            x -= 8;
            return x;
        }");

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void CompoundAssignment_StarEquals_Integer_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 6;
            x *= 7;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CompoundAssignment_SlashEquals_Double_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 100.0;
            x /= 4;
            return x;
        }");

        Assert.That(result, Is.EqualTo(25.0));
    }

    [Test]
    public void CompoundAssignment_PercentEquals_Integer_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 17;
            x %= 5;
            return x;
        }");

        Assert.That(result, Is.EqualTo(2));
    }


    // Compound Assignment - Bitwise Operators

    [Test]
    public void CompoundAssignment_AmpEquals_BitwiseAnd_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 15;
            x &= 9;
            return x;
        }");

        // 15 = 1111, 9 = 1001, AND = 1001 = 9
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void CompoundAssignment_PipeEquals_BitwiseOr_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            x |= 3;
            return x;
        }");

        // 5 = 101, 3 = 011, OR = 111 = 7
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void CompoundAssignment_CaretEquals_BitwiseXor_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 12;
            x ^= 5;
            return x;
        }");

        // 12 = 1100, 5 = 0101, XOR = 1001 = 9
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void CompoundAssignment_LessLessEquals_LeftShift_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            x <<= 4;
            return x;
        }");

        // 1 << 4 = 16
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void CompoundAssignment_GreaterGreaterEquals_RightShift_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 32;
            x >>= 2;
            return x;
        }");

        // 32 >> 2 = 8
        Assert.That(result, Is.EqualTo(8));
    }


    // Compound Assignment - Different Numeric Types

    [Test]
    public void CompoundAssignment_Long_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            long x = 10000000000;
            x += 5000000000;
            return x;
        }");

        Assert.That(result, Is.EqualTo(15000000000L));
    }

    [Test]
    public void CompoundAssignment_Float_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            float x = 3.14f;
            x += 2.86f;
            return x;
        }");

        Assert.That(result, Is.EqualTo(6.0f).Within(0.001f));
    }

    [Test]
    public void CompoundAssignment_Decimal_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            decimal x = 100.50m;
            x -= 25.25m;
            return x;
        }");

        Assert.That(result, Is.EqualTo(75.25m));
    }

    [Test]
    public void CompoundAssignment_IntPlusDouble_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var x = 10;
            x += 5.5;
            return x;
        }"));
    }


    // Compound Assignment - String Concatenation

    [Test]
    public void CompoundAssignment_StringPlusEquals_Concatenates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""Hello"";
            s += "" World"";
            return s;
        }");

        Assert.That(result, Is.EqualTo("Hello World"));
    }

    [Test]
    public void CompoundAssignment_StringPlusEquals_Multiple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""A"";
            s += ""B"";
            s += ""C"";
            s += ""D"";
            return s;
        }");

        Assert.That(result, Is.EqualTo("ABCD"));
    }

    [Test]
    public void CompoundAssignment_StringPlusEquals_WithNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var s = ""Count: "";
            s += 42;
            return s;
        }");

        Assert.That(result, Is.EqualTo("Count: 42"));
    }


    // Compound Assignment - In Loops

    [Test]
    public void CompoundAssignment_InWhileLoop_Accumulates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            while (i <= 5) {
                sum += i;
                i += 1;
            }
            return sum;
        }");

        // 1 + 2 + 3 + 4 + 5 = 15
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_InForLoop_Accumulates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var product = 1;
            for (var i = 1; i <= 5; i += 1) {
                product *= i;
            }
            return product;
        }");

        // 1 * 2 * 3 * 4 * 5 = 120
        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public void CompoundAssignment_InForEachLoop_Accumulates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in [1, 2, 3, 4, 5]) {
                sum += n;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_InDoWhileLoop_Accumulates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            do {
                sum += i;
                i += 1;
            } while (i <= 3);
            return sum;
        }");

        // 1 + 2 + 3 = 6
        Assert.That(result, Is.EqualTo(6));
    }


    // Compound Assignment - With Expressions

    [Test]
    public void CompoundAssignment_WithExpressionRHS_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            var y = 5;
            x += y * 2;
            return x;
        }");

        // 10 + (5 * 2) = 20
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void CompoundAssignment_WithMethodCallRHS_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10.0;
            x += Math.Abs(-5);
            return x;
        }");

        Assert.That(result, Is.EqualTo(15.0));
    }

    [Test]
    public void CompoundAssignment_WithTernaryRHS_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("condition", true);

        var result = engine.Evaluate(@"
        {
            var x = 100;
            x += condition ? 50 : 25;
            return x;
        }");

        Assert.That(result, Is.EqualTo(150));
    }

    [Test]
    public void CompoundAssignment_WithNullCoalesceRHS_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("maybeNull", null);

        var result = engine.Evaluate(@"
        {
            var x = 10;
            x += maybeNull ?? 5;
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }


    // Compound Assignment - Chained Operations

    [Test]
    public void CompoundAssignment_MultipleOnSameVariable_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            x += 5;
            x -= 3;
            x *= 2;
            x /= 4;
            return x;
        }");

        // ((10 + 5) - 3) * 2 / 4 = 12 * 2 / 4 = 24 / 4 = 6
        Assert.That(result, Is.EqualTo(6.0));
    }

    [Test]
    public void CompoundAssignment_ReturnsNewValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            var y = x += 10;
            return y;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_MultipleVariables_Independent()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 10;
            var b = 20;
            var c = 30;
            a += 1;
            b += 2;
            c += 3;
            return a + b + c;
        }");

        // 11 + 22 + 33 = 66
        Assert.That(result, Is.EqualTo(66));
    }


    // Compound Assignment - External Variables

    [Test]
    public void CompoundAssignment_ExternalVariable_Updates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("counter", 100L);

        var result = engine.Evaluate(@"
        {
            counter += 50;
            return counter;
        }");

        Assert.That(result, Is.EqualTo(150L));
    }

    [Test]
    public void CompoundAssignment_CombiningExternalAndLocal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("baseValue", 100L);

        // Use long for local since int += long is not valid in C#
        var result = engine.Evaluate(@"
        {
            var local = 10L;
            local += baseValue;
            baseValue += local;
            return baseValue;
        }");

        // local = 10 + 100 = 110
        // baseValue = 100 + 110 = 210
        Assert.That(result, Is.EqualTo(210L));
    }


    // Compound Assignment - In Conditionals

    [Test]
    public void CompoundAssignment_InsideIfBlock_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            if (true) {
                x += 5;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_InsideElseBlock_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            if (false) {
                x += 100;
            } else {
                x += 5;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void CompoundAssignment_ConditionalPathSelection()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("shouldAdd", true);

        var result = engine.Evaluate(@"
        {
            var amount = 0;
            if (shouldAdd) {
                amount += 10;
            } else {
                amount -= 10;
            }
            return amount;
        }");

        Assert.That(result, Is.EqualTo(10));
    }


    // Compound Assignment - Error Cases

    [Test]
    public void CompoundAssignment_UndefinedVariable_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                undefinedVar += 10;
                return undefinedVar;
            }"));
    }

    [Test]
    public void CompoundAssignment_IncompatibleTypes_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var s = ""hello"";
                s -= ""world"";
                return s;
            }"));
    }

    [Test]
    public void CompoundAssignment_DivisionByZero_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<DivideByZeroException>(() =>
            engine.Evaluate(@"
            {
                var x = 10;
                x /= 0;
                return x;
            }"));
    }

    [Test]
    public void CompoundAssignment_ModuloByZero_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<DivideByZeroException>(() =>
            engine.Evaluate(@"
            {
                var x = 10;
                x %= 0;
                return x;
            }"));
    }


    // Compound Assignment - All Operators Comprehensive

    [Test]
    public void CompoundAssignment_AllArithmeticOperators_WorkCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 100.0;

            x += 50;
            var afterPlus = x;

            x -= 25;
            var afterMinus = x;

            x *= 2;
            var afterMult = x;

            x /= 5;
            var afterDiv = x;

            x = 17;
            x %= 5;
            var afterMod = x;

            return afterPlus + afterMinus + afterMult + afterDiv + afterMod;
        }");

        // 150 + 125 + 250 + 50 + 2 = 577
        Assert.That(result, Is.EqualTo(577.0));
    }

    [Test]
    public void CompoundAssignment_AllBitwiseOperators_WorkCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var andResult = 15;
            andResult &= 9;

            var orResult = 5;
            orResult |= 3;

            var xorResult = 12;
            xorResult ^= 5;

            var leftShift = 1;
            leftShift <<= 4;

            var rightShift = 32;
            rightShift >>= 2;

            return andResult + orResult + xorResult + leftShift + rightShift;
        }");

        // 9 + 7 + 9 + 16 + 8 = 49
        Assert.That(result, Is.EqualTo(49));
    }


    // Compound Assignment - Pre-Parsed

    [Test]
    public void CompoundAssignment_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x += increment;
            return x;
        }");

        engine.SetVariable("startVal", 10L);
        engine.SetVariable("increment", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(15L));

        engine.SetVariable("startVal", 100L);
        engine.SetVariable("increment", 50L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(150L));
    }


    // Compound Assignment - Edge Cases

    [Test]
    public void CompoundAssignment_ZeroValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            x += 0;
            x -= 0;
            x *= 0;
            return x;
        }");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CompoundAssignment_NegativeNumbers_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = -10;
            x += -5;
            return x;
        }");

        Assert.That(result, Is.EqualTo(-15));
    }

    [Test]
    public void CompoundAssignment_VeryLargeNumbers_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 9000000000000000000L;
            x += 1;
            return x;
        }");

        Assert.That(result, Is.EqualTo(9000000000000000001L));
    }

    [Test]
    public void CompoundAssignment_ShiftOperations_EdgeCases()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 255;
            x <<= 0;
            var noShift = x;

            x = 1;
            x <<= 63;
            var maxShift = x;

            return noShift;
        }");

        Assert.That(result, Is.EqualTo(255));
    }


    // Increment/Decrement - Prefix Increment

    [Test]
    public void PrefixIncrement_Integer_IncrementsAndReturnsNewValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            var y = ++x;
            return y;
        }");

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void PrefixIncrement_UpdatesVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            ++x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void PrefixIncrement_Long_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            long x = 9000000000;
            ++x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(9000000001L));
    }

    [Test]
    public void PrefixIncrement_Double_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 3.5;
            var y = ++x;
            return y;
        }");

        Assert.That(result, Is.EqualTo(4.5));
    }

    [Test]
    public void PrefixIncrement_Float_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            float x = 2.5f;
            ++x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(3.5f));
    }

    [Test]
    public void PrefixIncrement_Decimal_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            decimal x = 99.99m;
            ++x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(100.99m));
    }


    // Increment/Decrement - Postfix Increment

    [Test]
    public void PostfixIncrement_Integer_ReturnsOldValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            var y = x++;
            return y;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PostfixIncrement_UpdatesVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            x++;
            return x;
        }");

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void PostfixIncrement_ReturnsDifferentThanVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            var captured = x++;
            return captured + x;
        }");

        // captured = 10, x = 11, sum = 21
        Assert.That(result, Is.EqualTo(21));
    }

    [Test]
    public void PostfixIncrement_Long_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            long x = 5000000000;
            var old = x++;
            return old;
        }");

        Assert.That(result, Is.EqualTo(5000000000L));
    }

    [Test]
    public void PostfixIncrement_Double_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1.5;
            var old = x++;
            return old;
        }");

        Assert.That(result, Is.EqualTo(1.5));
    }


    // Increment/Decrement - Prefix Decrement

    [Test]
    public void PrefixDecrement_Integer_DecrementsAndReturnsNewValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            var y = --x;
            return y;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void PrefixDecrement_UpdatesVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            --x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void PrefixDecrement_ToNegative_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            --x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void PrefixDecrement_Long_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            long x = 9000000001;
            --x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(9000000000L));
    }

    [Test]
    public void PrefixDecrement_Double_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5.5;
            var y = --x;
            return y;
        }");

        Assert.That(result, Is.EqualTo(4.5));
    }


    // Increment/Decrement - Postfix Decrement

    [Test]
    public void PostfixDecrement_Integer_ReturnsOldValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            var y = x--;
            return y;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void PostfixDecrement_UpdatesVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            x--;
            return x;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void PostfixDecrement_ReturnsDifferentThanVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 15;
            var captured = x--;
            return captured + x;
        }");

        // captured = 15, x = 14, sum = 29
        Assert.That(result, Is.EqualTo(29));
    }


    // Increment/Decrement - In Loops

    [Test]
    public void PrefixIncrement_InForLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; ++i) {
                sum += i;
            }
            return sum;
        }");

        // 0 + 1 + 2 + 3 + 4 = 10
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void PostfixIncrement_InForLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += i;
            }
            return sum;
        }");

        // 0 + 1 + 2 + 3 + 4 = 10
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void PrefixDecrement_InWhileLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var count = 5;
            var sum = 0;
            while (count > 0) {
                sum += --count;
            }
            return sum;
        }");

        // 4 + 3 + 2 + 1 + 0 = 10
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void PostfixDecrement_InWhileLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var count = 5;
            var sum = 0;
            while (count > 0) {
                sum += count--;
            }
            return sum;
        }");

        // 5 + 4 + 3 + 2 + 1 = 15
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void IncrementDecrement_InDoWhileLoop_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var sum = 0;
            do {
                sum += i++;
            } while (i < 4);
            return sum;
        }");

        // 0 + 1 + 2 + 3 = 6
        Assert.That(result, Is.EqualTo(6));
    }


    // Increment/Decrement - Multiple Operations

    [Test]
    public void MultipleIncrements_OnSameVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            x++;
            x++;
            x++;
            return x;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void MultipleDecrements_OnSameVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            x--;
            x--;
            x--;
            return x;
        }");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void MixedIncrementDecrement_OnSameVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            x++;
            x--;
            ++x;
            --x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void IncrementDecrement_MultipleVariables()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 10;
            a++;
            b--;
            a++;
            b--;
            return a + b;
        }");

        // a = 2, b = 8, sum = 10
        Assert.That(result, Is.EqualTo(10));
    }


    // Increment/Decrement - With Other Operations

    [Test]
    public void Increment_WithAddition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            var y = x++ + 10;
            return y;
        }");

        // x is 5 when added (postfix), so y = 5 + 10 = 15
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void PrefixIncrement_WithAddition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            var y = ++x + 10;
            return y;
        }");

        // x becomes 6 first (prefix), so y = 6 + 10 = 16
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void Increment_WithCompoundAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            x++;
            x += 10;
            return x;
        }");

        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void Increment_WithConditional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 5;
            if (x++ > 4) {
                return x;
            }
            return 0;
        }");

        // x is 5 in condition (postfix), but x becomes 6 after
        Assert.That(result, Is.EqualTo(6));
    }


    // Increment/Decrement - External Variables

    [Test]
    public void Increment_ExternalVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("counter", 100L);

        var result = engine.Evaluate(@"
        {
            counter++;
            return counter;
        }");

        Assert.That(result, Is.EqualTo(101L));
    }

    [Test]
    public void Decrement_ExternalVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("counter", 100L);

        var result = engine.Evaluate(@"
        {
            counter--;
            return counter;
        }");

        Assert.That(result, Is.EqualTo(99L));
    }

    [Test]
    public void PrefixIncrement_ExternalVariable_ReturnsNewValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("val", 50L);

        var result = engine.Evaluate(@"
        {
            var captured = ++val;
            return captured;
        }");

        Assert.That(result, Is.EqualTo(51L));
    }

    [Test]
    public void PostfixIncrement_ExternalVariable_ReturnsOldValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("val", 50L);

        var result = engine.Evaluate(@"
        {
            var captured = val++;
            return captured;
        }");

        Assert.That(result, Is.EqualTo(50L));
    }


    // Increment/Decrement - Edge Cases

    [Test]
    public void Increment_FromZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 0;
            return ++x;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Decrement_ToZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 1;
            return --x;
        }");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Increment_NegativeNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = -5;
            return ++x;
        }");

        Assert.That(result, Is.EqualTo(-4));
    }

    [Test]
    public void Decrement_NegativeNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = -5;
            return --x;
        }");

        Assert.That(result, Is.EqualTo(-6));
    }

    [Test]
    public void Increment_VeryLargeNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            long x = 9223372036854775806;
            ++x;
            return x;
        }");

        Assert.That(result, Is.EqualTo(9223372036854775807L));
    }


    // Increment/Decrement - Prefix vs Postfix Comparison

    [Test]
    public void PrefixVsPostfix_InExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 5;
            var b = 5;
            var prefixResult = ++a;
            var postfixResult = b++;
            return prefixResult - postfixResult;
        }");

        // prefix returns 6, postfix returns 5, diff = 1
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void PrefixVsPostfix_SameEndValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 5;
            var b = 5;
            ++a;
            b++;
            return a == b;
        }");

        // Both end up at 6
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void PrefixVsPostfix_DecrementComparison()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var a = 10;
            var b = 10;
            var prefixResult = --a;
            var postfixResult = b--;
            return prefixResult + postfixResult;
        }");

        // prefix returns 9, postfix returns 10, sum = 19
        Assert.That(result, Is.EqualTo(19));
    }


    // Increment/Decrement - Pre-Parsed

    [Test]
    public void IncrementDecrement_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x++;
            ++x;
            return x;
        }");

        engine.SetVariable("startVal", 0L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(2L));

        engine.SetVariable("startVal", 100L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(102L));
    }


    // Index Assignment - Array/List

    [Test]
    public void IndexAssignment_List_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[1] = 10;
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IndexAssignment_List_FirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[0] = 100;
            return arr[0];
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void IndexAssignment_List_LastElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr[2] = 999;
            return arr[2];
        }");

        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void IndexAssignment_List_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            var x = arr[1] = 42;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void IndexAssignment_ExternalList_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var list = new List<int> { 1, 2, 3 };
        engine.SetVariable("arr", list);

        engine.Evaluate("arr[1] = 99");

        Assert.That(list[1], Is.EqualTo(99));
    }

    [Test]
    public void IndexAssignment_ExternalArray_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var arr = new object?[] { 1, 2, 3 };
        engine.SetVariable("arr", arr);

        engine.Evaluate("arr[0] = 999");

        Assert.That(arr[0], Is.EqualTo(999));
    }

    [Test]
    public void IndexAssignment_OutOfRange_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Evaluate(@"
            {
                var arr = [1, 2, 3];
                arr[10] = 5;
                return arr;
            }"));
    }

    [Test]
    public void IndexAssignment_NegativeIndex_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.Evaluate(@"
            {
                var arr = [1, 2, 3];
                arr[-1] = 5;
                return arr;
            }"));
    }

    [Test]
    public void IndexAssignment_OnNull_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("arr[0] = 5"));
    }


    // Index Assignment - Dictionary

    [Test]
    public void IndexAssignment_Dictionary_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { name = ""John"" };
            dict[""name""] = ""Jane"";
            return dict[""name""];
        }");

        Assert.That(result, Is.EqualTo("Jane"));
    }

    [Test]
    public void IndexAssignment_Dictionary_AddsNewKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { name = ""John"" };
            dict[""age""] = 30;
            return dict[""age""];
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void IndexAssignment_ExternalDictionary_ModifiesOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var dict = new Dictionary<string, object?> { ["key"] = "old" };
        engine.SetVariable("dict", dict);

        engine.Evaluate(@"dict[""key""] = ""new""");

        Assert.That(dict["key"], Is.EqualTo("new"));
    }

    [Test]
    public void IndexAssignment_Dictionary_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var dict = new { a = 1 };
            var x = dict[""a""] = 100;
            return x;
        }");

        Assert.That(result, Is.EqualTo(100));
    }


    // Index Assignment - In Loops

    [Test]
    public void IndexAssignment_InForLoop_PopulatesArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [0, 0, 0, 0, 0];
            for (var i = 0; i < 5; i++) {
                arr[i] = i * 10;
            }
            return arr[3];
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void IndexAssignment_InWhileLoop_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            var i = 0;
            while (i < 3) {
                arr[i] = arr[i] * 2;
                i++;
            }
            return arr[1];
        }");

        Assert.That(result, Is.EqualTo(4));
    }


    // Property Assignment - Anonymous Object

    [Test]
    public void PropertyAssignment_AnonymousObject_SetsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj.Name = ""Jane"";
            return obj.Name;
        }");

        Assert.That(result, Is.EqualTo("Jane"));
    }

    [Test]
    public void PropertyAssignment_AnonymousObject_AddsNewProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Name = ""John"" };
            obj.Age = 30;
            return obj.Age;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void PropertyAssignment_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Value = 0 };
            var x = obj.Value = 42;
            return x;
        }");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PropertyAssignment_NestedObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Inner = new { Value = 10 } };
            obj.Inner.Value = 99;
            return obj.Inner.Value;
        }");

        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void PropertyAssignment_OnNull_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("obj.Name = \"test\""));
    }


    // Property Assignment - Typed Objects

    [Test]
    public void PropertyAssignment_TypedObject_SetsProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var person = new TestPerson { Name = "John", Age = 25 };
        engine.SetVariable("person", person);

        engine.Evaluate(@"person.Name = ""Jane""");

        Assert.That(person.Name, Is.EqualTo("Jane"));
    }

    [Test]
    public void PropertyAssignment_TypedObject_SetsIntProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var person = new TestPerson { Name = "John", Age = 25 };
        engine.SetVariable("person", person);

        engine.Evaluate("person.Age = 30");

        Assert.That(person.Age, Is.EqualTo(30));
    }

    [Test]
    public void PropertyAssignment_ReadOnlyProperty_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("text", "hello");

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("text.Length = 10"));
    }

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }


    // Property Assignment - In Loops

    [Test]
    public void PropertyAssignment_InForLoop_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Counter = 0 };
            for (var i = 0; i < 5; i++) {
                obj.Counter = obj.Counter + 1;
            }
            return obj.Counter;
        }");

        Assert.That(result, Is.EqualTo(5));
    }


    // Mixed Index and Property Assignment

    [Test]
    public void MixedAssignment_ArrayOfObjects()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [new { Value = 1 }, new { Value = 2 }];
            items[0].Value = 100;
            return items[0].Value;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void MixedAssignment_ObjectWithArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var obj = new { Items = [1, 2, 3] };
            obj.Items[1] = 99;
            return obj.Items[1];
        }");

        Assert.That(result, Is.EqualTo(99));
    }

}
