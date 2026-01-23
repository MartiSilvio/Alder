using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class AssignmentTests
{
    #region Basic Assignment

    [Test]
    public void Assignment_SimpleVariable_UpdatesValue()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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

    #endregion

    #region Assignment with Different Types

    [Test]
    public void Assignment_StringValue_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3];
            arr = [4, 5, 6];
            return arr;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 4L, 5L, 6L }));
    }

    [Test]
    public void Assignment_AnonymousObject_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
        engine.SetVariable("multiplier", 3L);

        var result = engine.Evaluate(@"
        {
            var total = 0;
            total = multiplier * 10;
            return total;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region Assignment Expression Returns Value

    [Test]
    public void Assignment_ReturnsAssignedValue()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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

    #endregion

    #region Assignment in Conditionals

    [Test]
    public void Assignment_InsideIf_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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

    #endregion

    #region Assignment with LINQ

    [Test]
    public void Assignment_WithLinqResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("numbers", new List<object?> { 1L, 2L, 3L, 4L, 5L });

        var result = engine.Evaluate(@"
        {
            var filtered = numbers;
            filtered = numbers.Where(x => x > 2);
            return filtered;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 3L, 4L, 5L }));
    }

    [Test]
    public void Assignment_AccumulatingLinqResults_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var items = [1, 2, 3];
            items = [...items, 4, 5];
            items = items.Where(x => x > 2);
            return items;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 3L, 4L, 5L }));
    }

    #endregion

    #region Assignment with Modules

    [Test]
    public void Assignment_WithMathResult_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var greeting = ""Hello"";
            greeting = greeting + "" World"";
            return greeting;
        }");

        Assert.That(result, Is.EqualTo("Hello World"));
    }

    #endregion

    #region Assignment Scoping

    [Test]
    public void Assignment_InnerBlockModifiesOuter_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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

    #endregion

    #region Assignment Error Cases

    [Test]
    public void Assignment_ToUndefinedVariable_ThrowsException()
    {
        var engine = new CsEvalEngine();

        Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            engine.Evaluate(@"
            {
                undefinedVar = 10;
                return undefinedVar;
            }"));
    }

    #endregion

    #region Assignment with Interpolated Strings

    [Test]
    public void Assignment_InterpolatedString_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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

    #endregion

    #region Assignment with Ternary

    [Test]
    public void Assignment_FromTernary_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("condition", true);

        var result = engine.Evaluate(@"
        {
            var result = 0;
            result = condition ? 100 : 200;
            return result;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    #endregion

    #region Assignment with Null Coalesce

    [Test]
    public void Assignment_FromNullCoalesce_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var a = null;
            var b = null;

            a = 10;
            b ??= 20;

            return a + b;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region Pre-Parsed Assignment

    [Test]
    public void Assignment_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine();
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
