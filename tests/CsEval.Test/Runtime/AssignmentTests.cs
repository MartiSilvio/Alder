namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class AssignmentTests(CompilationMode mode)
{
    #region Assignment with Different Types

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

    #region Assignment in Conditionals

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
