namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CompoundAssignmentTests(CompilationMode mode)
{
    #region Basic Arithmetic

    [TestCase("{ var x = 10; x += 5; return x; }", 15,
        TestName = "CompoundAssignment_PlusEquals_Integer_WorksCorrectly")]
    [TestCase("{ var x = 20; x -= 8; return x; }", 12,
        TestName = "CompoundAssignment_MinusEquals_Integer_WorksCorrectly")]
    [TestCase("{ var x = 6; x *= 7; return x; }", 42,
        TestName = "CompoundAssignment_StarEquals_Integer_WorksCorrectly")]
    [TestCase("{ var x = 100.0; x /= 4; return x; }", 25.0,
        TestName = "CompoundAssignment_SlashEquals_Double_WorksCorrectly")]
    [TestCase("{ var x = 17; x %= 5; return x; }", 2,
        TestName = "CompoundAssignment_PercentEquals_Integer_WorksCorrectly")]
    public async Task CompoundAssignment_BasicArithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Bitwise Operators

    [TestCase("{ var x = 15; x &= 9; return x; }", 9,
        TestName = "CompoundAssignment_AmpEquals_BitwiseAnd_WorksCorrectly")]
    [TestCase("{ var x = 5; x |= 3; return x; }", 7,
        TestName = "CompoundAssignment_PipeEquals_BitwiseOr_WorksCorrectly")]
    [TestCase("{ var x = 12; x ^= 5; return x; }", 9,
        TestName = "CompoundAssignment_CaretEquals_BitwiseXor_WorksCorrectly")]
    [TestCase("{ var x = 1; x <<= 4; return x; }", 16,
        TestName = "CompoundAssignment_LessLessEquals_LeftShift_WorksCorrectly")]
    [TestCase("{ var x = 32; x >>= 2; return x; }", 8,
        TestName = "CompoundAssignment_GreaterGreaterEquals_RightShift_WorksCorrectly")]
    public async Task CompoundAssignment_Bitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Different Numeric Types

    [TestCase("{ long x = 10000000000; x += 5000000000; return x; }",
        TestName = "CompoundAssignment_Long_WorksCorrectly")]
    [TestCase("{ decimal x = 100.50m; x -= 25.25m; return x; }",
        TestName = "CompoundAssignment_Decimal_WorksCorrectly")]
    public async Task CompoundAssignment_NumericTypes(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [Test]
    public async Task CompoundAssignment_Float_WorksCorrectly()
    {
        // Float needs Within() tolerance -- use parity-only form
        var expr = "{ float x = 3.14f; x += 2.86f; return x; }";
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(6.0f).Within(0.001f));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
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

    #endregion

    #region String Concatenation

    [TestCase("{ var s = \"Hello\"; s += \" World\"; return s; }", "Hello World",
        TestName = "CompoundAssignment_StringPlusEquals_Concatenates")]
    [TestCase("{ var s = \"A\"; s += \"B\"; s += \"C\"; s += \"D\"; return s; }", "ABCD",
        TestName = "CompoundAssignment_StringPlusEquals_Multiple")]
    [TestCase("{ var s = \"Count: \"; s += 42; return s; }", "Count: 42",
        TestName = "CompoundAssignment_StringPlusEquals_WithNumber")]
    public async Task CompoundAssignment_StringConcat(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region In Loops

    [TestCase("""
        {
            var sum = 0;
            var i = 1;
            while (i <= 5) {
                sum += i;
                i += 1;
            }
            return sum;
        }
        """, 15,
        TestName = "CompoundAssignment_InWhileLoop_Accumulates")]
    [TestCase("""
        {
            var product = 1;
            for (var i = 1; i <= 5; i += 1) {
                product *= i;
            }
            return product;
        }
        """, 120,
        TestName = "CompoundAssignment_InForLoop_Accumulates")]
    [TestCase("""
        {
            var sum = 0;
            var i = 1;
            do {
                sum += i;
                i += 1;
            } while (i <= 3);
            return sum;
        }
        """, 6,
        TestName = "CompoundAssignment_InDoWhileLoop_Accumulates")]
    public async Task CompoundAssignment_InLoops(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // ForEach with collection expression -- engine-only (CsEval-specific [1,2,3] syntax)
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

    #endregion

    #region With Expressions

    [TestCase("""
        {
            var x = 10;
            var y = 5;
            x += y * 2;
            return x;
        }
        """, 20,
        TestName = "CompoundAssignment_WithExpressionRHS_WorksCorrectly")]
    [TestCase("""
        {
            var x = 10.0;
            x += Math.Abs(-5);
            return x;
        }
        """, 15.0,
        TestName = "CompoundAssignment_WithMethodCallRHS_WorksCorrectly")]
    public async Task CompoundAssignment_WithExpressions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public async Task CompoundAssignment_WithTernaryRHS_WorksCorrectly()
    {
        var expr = "{ var x = 100; x += true ? 50 : 25; return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, 150, mode);
    }

    [Test]
    public async Task CompoundAssignment_WithNullCoalesceRHS_WorksCorrectly()
    {
        var expr = """
        {
            int? maybeNull = null;
            var x = 10;
            x += maybeNull ?? 5;
            return x;
        }
        """;
        await TestHelpers.RunCSharpParityTestAsync(expr, 15, mode);
    }

    #endregion

    #region Chained Operations

    [TestCase("""
        {
            var x = 100.0;
            x += 50;
            x -= 25;
            x *= 2;
            x /= 4;
            return x;
        }
        """,
        TestName = "CompoundAssignment_MultipleOnSameVariable_WorksCorrectly")]
    public async Task CompoundAssignment_MultipleOnSameVariable(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ var x = 5; var y = x += 10; return y; }", 15,
        TestName = "CompoundAssignment_ReturnsNewValue")]
    [TestCase("""
        {
            var a = 10;
            var b = 20;
            var c = 30;
            a += 1;
            b += 2;
            c += 3;
            return a + b + c;
        }
        """, 66,
        TestName = "CompoundAssignment_MultipleVariables_Independent")]
    public async Task CompoundAssignment_Chained(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region External Variables

    [Test]
    public async Task CompoundAssignment_ExternalVariable_Updates()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter += 50; return counter; }", variables, 150L, mode);
    }

    [Test]
    public async Task CompoundAssignment_CombiningExternalAndLocal()
    {
        var variables = new Dictionary<string, object?> { ["baseValue"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var local = 10L;
                local += baseValue;
                baseValue += local;
                return baseValue;
            }
            """, variables, 210L, mode);
    }

    #endregion

    #region In Conditionals

    [TestCase("""
        {
            var x = 10;
            if (true) {
                x += 5;
            }
            return x;
        }
        """, 15,
        TestName = "CompoundAssignment_InsideIfBlock_WorksCorrectly")]
    [TestCase("""
        {
            var x = 10;
            if (false) {
                x += 100;
            } else {
                x += 5;
            }
            return x;
        }
        """, 15,
        TestName = "CompoundAssignment_InsideElseBlock_WorksCorrectly")]
    public async Task CompoundAssignment_InConditionals(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public async Task CompoundAssignment_ConditionalPathSelection()
    {
        var variables = new Dictionary<string, object?> { ["shouldAdd"] = true };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var amount = 0;
                if (shouldAdd) {
                    amount += 10;
                } else {
                    amount -= 10;
                }
                return amount;
            }
            """, variables, 10, mode);
    }

    #endregion

    #region Error Cases

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

    #endregion

    #region All Operators Comprehensive

    [TestCase("""
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
        }
        """,
        TestName = "CompoundAssignment_AllArithmeticOperators_WorkCorrectly")]
    [TestCase("""
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
        }
        """,
        TestName = "CompoundAssignment_AllBitwiseOperators_WorkCorrectly")]
    public async Task CompoundAssignment_AllOperators(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Pre-Parsed

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

    #endregion

    #region Edge Cases

    [TestCase("{ var x = 0; x += 0; x -= 0; x *= 0; return x; }", 0,
        TestName = "CompoundAssignment_ZeroValue_WorksCorrectly")]
    [TestCase("{ var x = -10; x += -5; return x; }", -15,
        TestName = "CompoundAssignment_NegativeNumbers_WorksCorrectly")]
    public async Task CompoundAssignment_EdgeCases(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ var x = 9000000000000000000L; x += 1; return x; }",
        TestName = "CompoundAssignment_VeryLargeNumbers_WorksCorrectly")]
    public async Task CompoundAssignment_LargeNumbers(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("""
        {
            var x = 255;
            x <<= 0;
            var noShift = x;
            x = 1;
            x <<= 63;
            var maxShift = x;
            return noShift;
        }
        """, 255,
        TestName = "CompoundAssignment_ShiftOperations_EdgeCases")]
    public async Task CompoundAssignment_ShiftEdgeCases(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
