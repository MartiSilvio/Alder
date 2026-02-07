using System.Collections;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AssignmentTests(CompilationMode mode)
{
    #region Basic Assignment

    [TestCase("{ var x = 10; x = 20; return x; }", 20,
        TestName = "Assignment_SimpleVariable_UpdatesValue")]
    [TestCase("{ var x = 1; x = 2; x = 3; x = 4; return x; }", 4,
        TestName = "Assignment_MultipleAssignments_TracksLatestValue")]
    [TestCase("{ var x = 5; x = x + 10; return x; }", 15,
        TestName = "Assignment_ToExpressionResult_WorksCorrectly")]
    [TestCase("{ var x = 1; x = x + 1; x = x * 2; x = x - 1; return x; }", 3,
        TestName = "Assignment_ChainedArithmetic_WorksCorrectly")]
    public async Task Assignment_Basic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Assignment with Different Types

    [TestCase("""
        {
            var s = "hello";
            s = "world";
            return s;
        }
        """, "world",
        TestName = "Assignment_StringValue_WorksCorrectly")]
    [TestCase("{ var flag = true; flag = false; return flag; }", false,
        TestName = "Assignment_BooleanValue_WorksCorrectly")]
    [TestCase("{ var d = 1.5; d = 3.14; return d; }", 3.14,
        TestName = "Assignment_DoubleValue_WorksCorrectly")]
    public async Task Assignment_Types(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("""
        {
            var obj = "something";
            obj = null;
            return obj;
        }
        """, null,
        TestName = "Assignment_NullValue_WorksCorrectly")]
    public async Task Assignment_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // CsEval-specific: [1,2,3] collection expression with var -- engine-only test
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

        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That(result, Is.EqualTo(new int[] { 4, 5, 6 }));
    }

    // CsEval-specific: mutable anonymous objects -- engine-only test
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

    #endregion

    #region Assignment with External Variables

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
    public async Task Assignment_CombiningExternalAndLocal_WorksCorrectly()
    {
        var variables = new Dictionary<string, object?> { ["multiplier"] = 3 };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var total = 0;
                total = multiplier * 10;
                return total;
            }
            """, variables, 30, mode);
    }

    #endregion

    #region Assignment Expression Returns Value

    [TestCase("""
        {
            var x = 0;
            var y = x = 42;
            return y;
        }
        """, 42,
        TestName = "Assignment_ReturnsAssignedValue")]
    [TestCase("""
        {
            var a = 0;
            var b = 0;
            var c = 0;
            a = b = c = 100;
            return a + b + c;
        }
        """, 300,
        TestName = "Assignment_ChainedAssignment_WorksCorrectly")]
    public async Task Assignment_ExpressionReturns(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Assignment in Conditionals

    [TestCase("""
        {
            var x = 0;
            if (true) {
                x = 100;
            }
            return x;
        }
        """, 100,
        TestName = "Assignment_InsideIf_WorksCorrectly")]
    [TestCase("""
        {
            var x = 0;
            if (false) {
                x = 50;
            } else {
                x = 100;
            }
            return x;
        }
        """, 100,
        TestName = "Assignment_InsideElse_WorksCorrectly")]
    public async Task Assignment_InConditionals(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [Test]
    public async Task Assignment_ConditionalBranches_UpdatesCorrectly()
    {
        var variables = new Dictionary<string, object?> { ["condition"] = true };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var msg = "initial";
                if (condition) {
                    msg = "was true";
                } else {
                    msg = "was false";
                }
                return msg;
            }
            """, variables, "was true", mode);
    }

    #endregion

    #region Assignment with LINQ

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

    // CsEval-specific: [..items, 4, 5] spread syntax -- engine-only test
    [Test]
    public void Assignment_AccumulatingLinqResults_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [1, 2, 3];
            items = [..items, 4, 5];
            var filtered = items.Where(x => x > 2).ToList();
            return filtered;
        }");

        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Is.EqualTo(new List<int> { 3, 4, 5 }));
    }

    #endregion

    #region Assignment with Modules

    [TestCase("""
        {
            var absValue = 0.0;
            absValue = Math.Abs(-42.5);
            return absValue;
        }
        """, 42.5,
        TestName = "Assignment_WithMathResult_WorksCorrectly")]
    [TestCase("""
        {
            var greeting = "Hello";
            greeting = greeting + " World";
            return greeting;
        }
        """, "Hello World",
        TestName = "Assignment_WithStringConcat_WorksCorrectly")]
    public async Task Assignment_WithModules(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Assignment Scoping

    [TestCase("""
        {
            var x = 1;
            if (true) {
                x = 2;
                if (true) {
                    x = 3;
                }
            }
            return x;
        }
        """, 3,
        TestName = "Assignment_InnerBlockModifiesOuter_WorksCorrectly")]
    [TestCase("""
        {
            var a = 1;
            var b = 10;
            var c = 100;
            a = a + 1;
            b = b + 1;
            c = c + 1;
            return a + b + c;
        }
        """, 114,
        TestName = "Assignment_MultipleVariables_TracksIndependently")]
    public async Task Assignment_Scoping(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Assignment Error Cases

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

    #endregion

    #region Assignment with Interpolated Strings

    [TestCase("""
        {
            var name = "Alice";
            var greeting = "";
            greeting = $"Hello, {name}!";
            name = "Bob";
            greeting = $"Hello, {name}!";
            return greeting;
        }
        """, "Hello, Bob!",
        TestName = "Assignment_InterpolatedString_WorksCorrectly")]
    public async Task Assignment_InterpolatedStrings(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Assignment with Ternary

    [Test]
    public async Task Assignment_FromTernary_WorksCorrectly()
    {
        var variables = new Dictionary<string, object?> { ["condition"] = true };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var result = 0;
                result = condition ? 100 : 200;
                return result;
            }
            """, variables, 100, mode);
    }

    #endregion

    #region Assignment with Null Coalesce

    [TestCase("""
        {
            int? maybeNull = null;
            var fallback = 0;
            fallback = maybeNull ?? 42;
            return fallback;
        }
        """, 42,
        TestName = "Assignment_FromNullCoalesce_WorksCorrectly")]
    [TestCase("""
        {
            int? a = null;
            int? b = null;
            a = 10;
            b ??= 20;
            return a + b;
        }
        """, 30,
        TestName = "Assignment_VsNullCoalesceAssign_BothWork")]
    public async Task Assignment_NullCoalesce(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Pre-Parsed Assignment

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

    #endregion
}
