namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IncrementDecrementTests(CompilationMode mode)
{
    #region Prefix Increment

    [TestCase("{ var x = 5; var y = ++x; return y; }", 6,
        TestName = "PrefixIncrement_Integer_IncrementsAndReturnsNewValue")]
    [TestCase("{ var x = 10; ++x; return x; }", 11,
        TestName = "PrefixIncrement_UpdatesVariable")]
    [TestCase("{ var x = 3.5; var y = ++x; return y; }", 4.5,
        TestName = "PrefixIncrement_Double_WorksCorrectly")]
    public async Task PrefixIncrement(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long x = 9000000000; ++x; return x; }",
        TestName = "PrefixIncrement_Long_WorksCorrectly")]
    [TestCase("{ decimal x = 99.99m; ++x; return x; }",
        TestName = "PrefixIncrement_Decimal_WorksCorrectly")]
    public async Task PrefixIncrement_LargeTypes(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [Test]
    public async Task PrefixIncrement_Float_WorksCorrectly()
    {
        var expr = "{ float x = 2.5f; ++x; return x; }";
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(3.5f));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    #endregion

    #region Postfix Increment

    [TestCase("{ var x = 5; var y = x++; return y; }", 5,
        TestName = "PostfixIncrement_Integer_ReturnsOldValue")]
    [TestCase("{ var x = 5; x++; return x; }", 6,
        TestName = "PostfixIncrement_UpdatesVariable")]
    [TestCase("""
        {
            var x = 10;
            var captured = x++;
            return captured + x;
        }
        """, 21,
        TestName = "PostfixIncrement_ReturnsDifferentThanVariable")]
    [TestCase("{ var x = 1.5; var old = x++; return old; }", 1.5,
        TestName = "PostfixIncrement_Double_WorksCorrectly")]
    public async Task PostfixIncrement(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long x = 5000000000; var old = x++; return old; }",
        TestName = "PostfixIncrement_Long_WorksCorrectly")]
    public async Task PostfixIncrement_LargeTypes(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Prefix Decrement

    [TestCase("{ var x = 10; var y = --x; return y; }", 9,
        TestName = "PrefixDecrement_Integer_DecrementsAndReturnsNewValue")]
    [TestCase("{ var x = 5; --x; return x; }", 4,
        TestName = "PrefixDecrement_UpdatesVariable")]
    [TestCase("{ var x = 0; --x; return x; }", -1,
        TestName = "PrefixDecrement_ToNegative_WorksCorrectly")]
    [TestCase("{ var x = 5.5; var y = --x; return y; }", 4.5,
        TestName = "PrefixDecrement_Double_WorksCorrectly")]
    public async Task PrefixDecrement(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long x = 9000000001; --x; return x; }",
        TestName = "PrefixDecrement_Long_WorksCorrectly")]
    public async Task PrefixDecrement_LargeTypes(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Postfix Decrement

    [TestCase("{ var x = 10; var y = x--; return y; }", 10,
        TestName = "PostfixDecrement_Integer_ReturnsOldValue")]
    [TestCase("{ var x = 10; x--; return x; }", 9,
        TestName = "PostfixDecrement_UpdatesVariable")]
    [TestCase("""
        {
            var x = 15;
            var captured = x--;
            return captured + x;
        }
        """, 29,
        TestName = "PostfixDecrement_ReturnsDifferentThanVariable")]
    public async Task PostfixDecrement(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region In Loops

    [TestCase("""
        {
            var sum = 0;
            for (var i = 0; i < 5; ++i) {
                sum += i;
            }
            return sum;
        }
        """, 10,
        TestName = "PrefixIncrement_InForLoop_WorksCorrectly")]
    [TestCase("""
        {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += i;
            }
            return sum;
        }
        """, 10,
        TestName = "PostfixIncrement_InForLoop_WorksCorrectly")]
    [TestCase("""
        {
            var count = 5;
            var sum = 0;
            while (count > 0) {
                sum += --count;
            }
            return sum;
        }
        """, 10,
        TestName = "PrefixDecrement_InWhileLoop_WorksCorrectly")]
    [TestCase("""
        {
            var count = 5;
            var sum = 0;
            while (count > 0) {
                sum += count--;
            }
            return sum;
        }
        """, 15,
        TestName = "PostfixDecrement_InWhileLoop_WorksCorrectly")]
    [TestCase("""
        {
            var i = 0;
            var sum = 0;
            do {
                sum += i++;
            } while (i < 4);
            return sum;
        }
        """, 6,
        TestName = "IncrementDecrement_InDoWhileLoop_WorksCorrectly")]
    public async Task IncrementDecrement_InLoops(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Multiple Operations

    [TestCase("{ var x = 0; x++; x++; x++; return x; }", 3,
        TestName = "MultipleIncrements_OnSameVariable")]
    [TestCase("{ var x = 10; x--; x--; x--; return x; }", 7,
        TestName = "MultipleDecrements_OnSameVariable")]
    [TestCase("{ var x = 5; x++; x--; ++x; --x; return x; }", 5,
        TestName = "MixedIncrementDecrement_OnSameVariable")]
    [TestCase("""
        {
            var a = 0;
            var b = 10;
            a++;
            b--;
            a++;
            b--;
            return a + b;
        }
        """, 10,
        TestName = "IncrementDecrement_MultipleVariables")]
    public async Task IncrementDecrement_MultipleOps(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region With Other Operations

    [TestCase("{ var x = 5; var y = x++ + 10; return y; }", 15,
        TestName = "Increment_WithAddition")]
    [TestCase("{ var x = 5; var y = ++x + 10; return y; }", 16,
        TestName = "PrefixIncrement_WithAddition")]
    [TestCase("{ var x = 5; x++; x += 10; return x; }", 16,
        TestName = "Increment_WithCompoundAssignment")]
    [TestCase("""
        {
            var x = 5;
            if (x++ > 4) {
                return x;
            }
            return 0;
        }
        """, 6,
        TestName = "Increment_WithConditional")]
    public async Task IncrementDecrement_WithOtherOps(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region External Variables

    [Test]
    public async Task Increment_ExternalVariable()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter++; return counter; }", variables, 101L, mode);
    }

    [Test]
    public async Task Decrement_ExternalVariable()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter--; return counter; }", variables, 99L, mode);
    }

    [Test]
    public async Task PrefixIncrement_ExternalVariable_ReturnsNewValue()
    {
        var variables = new Dictionary<string, object?> { ["val"] = 50L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ var captured = ++val; return captured; }", variables, 51L, mode);
    }

    [Test]
    public async Task PostfixIncrement_ExternalVariable_ReturnsOldValue()
    {
        var variables = new Dictionary<string, object?> { ["val"] = 50L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ var captured = val++; return captured; }", variables, 50L, mode);
    }

    #endregion

    #region Edge Cases

    [TestCase("{ var x = 0; return ++x; }", 1,
        TestName = "Increment_FromZero")]
    [TestCase("{ var x = 1; return --x; }", 0,
        TestName = "Decrement_ToZero")]
    [TestCase("{ var x = -5; return ++x; }", -4,
        TestName = "Increment_NegativeNumber")]
    [TestCase("{ var x = -5; return --x; }", -6,
        TestName = "Decrement_NegativeNumber")]
    public async Task IncrementDecrement_EdgeCases(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long x = 9223372036854775806; ++x; return x; }",
        TestName = "Increment_VeryLargeNumber")]
    public async Task IncrementDecrement_LargeNumbers(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Prefix vs Postfix Comparison

    [TestCase("""
        {
            var a = 5;
            var b = 5;
            var prefixResult = ++a;
            var postfixResult = b++;
            return prefixResult - postfixResult;
        }
        """, 1,
        TestName = "PrefixVsPostfix_InExpression")]
    [TestCase("""
        {
            var a = 5;
            var b = 5;
            ++a;
            b++;
            return a == b;
        }
        """, true,
        TestName = "PrefixVsPostfix_SameEndValue")]
    [TestCase("""
        {
            var a = 10;
            var b = 10;
            var prefixResult = --a;
            var postfixResult = b--;
            return prefixResult + postfixResult;
        }
        """, 19,
        TestName = "PrefixVsPostfix_DecrementComparison")]
    public async Task PrefixVsPostfix(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Pre-Parsed

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

    #endregion
}
