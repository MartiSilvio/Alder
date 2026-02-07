using CsEval.TestData.Data;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SwitchStatementTests(CompilationMode mode)
{
    #region Basic Switch

    [TestCaseSource(typeof(SwitchStatementData), nameof(SwitchStatementData.ValueCases))]
    public async Task Eval_Switch(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Invalid Syntax Tests

    [Test]
    public void NonEmptyCase_WithoutBreak_ThrowsError()
    {
        // C# requires explicit break/return/throw for non-empty cases (CS0163)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(() => engine.Evaluate("""
            {
                var x = 1;
                var result = "";
                switch (x) {
                    case 1:
                        result = result + "one";
                    case 2:
                        result = result + "two";
                        break;
                }
                return result;
            }
            """),
            Throws.TypeOf<CsEvalException>().With.Message.Contains("CS0163"));
    }

    #endregion

    #region Tests with External Variables

    [Test]
    public async Task Switch_StringWithVariable()
    {
        var variables = new Dictionary<string, object?> { ["input"] = "test" };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var result = "";
                switch (input) {
                    case "test":
                        result = "matched test";
                        break;
                    default:
                        result = "no match";
                        break;
                }
                return result;
            }
            """, variables, "matched test", mode);
    }

    // Engine-only: uses anonymous object as external variable (not serializable to Roslyn)
    [Test]
    public void Switch_PropertyAccessInSwitch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", new { Status = "active" });
        var result = engine.Evaluate(@"
        {
            var result = """";
            switch (obj.Status) {
                case ""active"":
                    result = ""is active"";
                    break;
                case ""inactive"":
                    result = ""is inactive"";
                    break;
                default:
                    result = ""unknown status"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("is active"));
    }

    [Test]
    public async Task Switch_NullCase_MatchesNull()
    {
        var variables = new Dictionary<string, object?> { ["input"] = null };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var result = "";
                switch (input) {
                    case null:
                        result = "is null";
                        break;
                    default:
                        result = "not null";
                        break;
                }
                return result;
            }
            """, variables, "is null", mode);
    }

    [Test]
    public async Task Switch_NullCase_DoesNotMatchValue()
    {
        var variables = new Dictionary<string, object?> { ["input"] = "hello" };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var result = "";
                switch (input) {
                    case null:
                        result = "is null";
                        break;
                    default:
                        result = "not null";
                        break;
                }
                return result;
            }
            """, variables, "not null", mode);
    }

    #endregion

    #region Switch with Collections (CsEval-specific syntax)

    // Engine-only: uses [10, 20, 30] collection expression syntax
    [Test]
    public void Switch_WithIndexAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var arr = [10, 20, 30];
            var result = """";
            switch (arr[1]) {
                case 10:
                    result = ""ten"";
                    break;
                case 20:
                    result = ""twenty"";
                    break;
                case 30:
                    result = ""thirty"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("twenty"));
    }

    // Engine-only: uses [1, 2, 3, 2, 1] collection expression syntax
    [Test]
    public void Switch_InsideLoop()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [1, 2, 3, 2, 1];
            var countOnes = 0;
            var countTwos = 0;
            var countOthers = 0;
            foreach (var item in items) {
                switch (item) {
                    case 1:
                        countOnes = countOnes + 1;
                        break;
                    case 2:
                        countTwos = countTwos + 1;
                        break;
                    default:
                        countOthers = countOthers + 1;
                        break;
                }
            }
            return countOnes * 100 + countTwos * 10 + countOthers;
        }");

        Assert.That(result, Is.EqualTo(221));
    }

    #endregion

    #region Calculator Scenario

    [TestCaseSource(typeof(SwitchStatementData), nameof(SwitchStatementData.ParityCases))]
    public async Task Switch_Calculator(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Parsing Tests

    // Engine-only: tests CsEval parsing internals (TryParse)
    [Test]
    public void Switch_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse(@"
        {
            var x = 1;
            switch (x) {
                case 1:
                    return ""one"";
                default:
                    return ""other"";
            }
        }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void Switch_TryParse_MissingParenthesis_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ switch x { case 1: break; } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Switch_TryParse_MissingBrace_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ switch (x) case 1: break; }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Switch_TryParse_MissingColon_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ switch (x) { case 1 break; } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    // Engine-only: tests pre-parsed expression reuse with SetVariable
    [Test]
    public void Switch_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var result = """";
            switch (num) {
                case 1:
                    result = ""one"";
                    break;
                case 2:
                    result = ""two"";
                    break;
                default:
                    result = ""other"";
                    break;
            }
            return result;
        }");

        engine.SetVariable("num", 1L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo("one"));

        engine.SetVariable("num", 2L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo("two"));

        engine.SetVariable("num", 99L);
        var result3 = engine.Evaluate(expr);
        Assert.That(result3, Is.EqualTo("other"));
    }

    #endregion
}
