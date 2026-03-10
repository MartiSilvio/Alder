// Engine-only: Switch statement tests with error assertions, SetVariable, CsEval-specific syntax

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SwitchStatementTests(CompilationMode mode)
{
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

    // Migrated to .csx: TestData/Runtime/SwitchStatement/Switch_StringVariable_MatchesCase.csx
    // Migrated to .csx: TestData/Runtime/SwitchStatement/Switch_NullCase_MatchesNull.csx
    // Migrated to .csx: TestData/Runtime/SwitchStatement/Switch_NullCase_DoesNotMatchValue.csx

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
