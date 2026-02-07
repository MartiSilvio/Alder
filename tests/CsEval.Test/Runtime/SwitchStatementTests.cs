namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SwitchStatementTests(CompilationMode mode)
{
    #region Basic Switch

    [TestCase("""
        {
            var x = 2;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                case 2:
                    result = "two";
                    break;
                case 3:
                    result = "three";
                    break;
            }
            return result;
        }
        """,
        "two",
        TestName = "BasicCase_MatchesMiddle")]
    [TestCase("""
        {
            var x = 1;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                case 2:
                    result = "two";
                    break;
            }
            return result;
        }
        """,
        "one",
        TestName = "BasicCase_MatchesFirst")]
    [TestCase("""
        {
            var x = 3;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                case 2:
                    result = "two";
                    break;
                case 3:
                    result = "three";
                    break;
            }
            return result;
        }
        """,
        "three",
        TestName = "BasicCase_MatchesLast")]
    [TestCase("""
        {
            var x = 99;
            var result = "initial";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                case 2:
                    result = "two";
                    break;
            }
            return result;
        }
        """,
        "initial",
        TestName = "NoMatch_ResultUnchanged")]
    [TestCase("""
        {
            var x = 99;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                case 2:
                    result = "two";
                    break;
                default:
                    result = "other";
                    break;
            }
            return result;
        }
        """,
        "other",
        TestName = "DefaultCase_ExecutesWhenNoMatch")]
    [TestCase("""
        {
            var x = 1;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                default:
                    result = "other";
                    break;
            }
            return result;
        }
        """,
        "one",
        TestName = "DefaultCase_SkippedWhenCaseMatches")]
    [TestCase("""
        {
            var x = 99;
            var result = "";
            switch (x) {
                default:
                    result = "other";
                    break;
                case 1:
                    result = "one";
                    break;
            }
            return result;
        }
        """,
        "other",
        TestName = "DefaultCaseFirst_StillWorks")]
    [TestCase("""
        {
            var x = 99;
            var result = "";
            switch (x) {
                case 1:
                    result = "one";
                    break;
                default:
                    result = "other";
                    break;
                case 2:
                    result = "two";
                    break;
            }
            return result;
        }
        """,
        "other",
        TestName = "DefaultCaseMiddle_StillWorks")]
    [TestCase("""
        {
            var x = 42;
            var result = "";
            switch (x) {
                default:
                    result = "default only";
                    break;
            }
            return result;
        }
        """,
        "default only",
        TestName = "OnlyDefaultCase_ExecutesAlways")]
    [TestCase("""
        {
            var x = 1;
            var result = "";
            switch (x) {
                case 1:
                case 2:
                    result = "one or two";
                    break;
                case 3:
                    result = "three";
                    break;
            }
            return result;
        }
        """,
        "one or two",
        TestName = "EmptyCaseFallThrough")]
    [TestCase("""
        {
            var x = 2;
            var count = 0;
            switch (x) {
                case 1:
                case 2:
                case 3:
                    count = count + 1;
                    break;
            }
            return count;
        }
        """,
        1,
        TestName = "MultipleEmptyCasesFallThrough")]
    [TestCase("""
        {
            var fruit = "apple";
            var category = "";
            switch (fruit) {
                case "apple":
                    category = "pome";
                    break;
                case "orange":
                    category = "citrus";
                    break;
                case "banana":
                    category = "tropical";
                    break;
                default:
                    category = "unknown";
                    break;
            }
            return category;
        }
        """,
        "pome",
        TestName = "StringCase_MatchesExactly")]
    [TestCase("""
        {
            var fruit = "Apple";
            var category = "";
            switch (fruit) {
                case "apple":
                    category = "lowercase";
                    break;
                case "Apple":
                    category = "capitalized";
                    break;
                default:
                    category = "unknown";
                    break;
            }
            return category;
        }
        """,
        "capitalized",
        TestName = "StringCase_CaseSensitive")]
    [TestCase("""
        {
            var a = 3;
            var b = 2;
            var result = "";
            switch (a + b) {
                case 4:
                    result = "four";
                    break;
                case 5:
                    result = "five";
                    break;
                case 6:
                    result = "six";
                    break;
            }
            return result;
        }
        """,
        "five",
        TestName = "ExpressionInSwitchValue")]
    [TestCase("""
        {
            var x = 10;
            var multiplier = 2;
            var result = "";
            switch (x) {
                case 5 * 2:
                    result = "matched 5*2";
                    break;
                case 3 * 4:
                    result = "matched 3*4";
                    break;
            }
            return result;
        }
        """,
        "matched 5*2",
        TestName = "ExpressionInCasePattern")]
    [TestCase("""
        {
            var x = 2;
            switch (x) {
                case 1:
                    return "one";
                case 2:
                    return "two";
                case 3:
                    return "three";
            }
            return "no match";
        }
        """,
        "two",
        TestName = "ReturnInCase_ExitsBlock")]
    [TestCase("""
        {
            var x = 99;
            switch (x) {
                case 1:
                    return "one";
                case 2:
                    return "two";
                default:
                    return "default";
            }
            return "after switch";
        }
        """,
        "default",
        TestName = "ReturnInDefault_ExitsBlock")]
    [TestCase("""
        {
            var x = 99;
            switch (x) {
                case 1:
                    return "one";
                case 2:
                    return "two";
            }
            return "no match";
        }
        """,
        "no match",
        TestName = "NoMatchNoDefault_ContinuesAfter")]
    [TestCase("""
        {
            var x = 2;
            var total = 0;
            switch (x) {
                case 2:
                    var a = 10;
                    var b = 20;
                    total = a + b;
                    break;
            }
            return total;
        }
        """,
        30,
        TestName = "MultipleStatementsInCase")]
    [TestCase("""
        {
            var x = 1;
            var sum = 0;
            switch (x) {
                case 1:
                    var i = 0;
                    while (i < 5) {
                        sum = sum + i;
                        i = i + 1;
                    }
                    break;
            }
            return sum;
        }
        """,
        10,
        TestName = "LoopInsideCase")]
    [TestCase("""
        {
            var x = 1;
            var val = 15;
            var category = "";
            switch (x) {
                case 1:
                    if (val > 10) {
                        category = "high";
                    } else {
                        category = "low";
                    }
                    break;
            }
            return category;
        }
        """,
        "high",
        TestName = "IfInsideCase")]
    [TestCase("""
        {
            var outer = 1;
            var inner = 2;
            var result = "";
            switch (outer) {
                case 1:
                    switch (inner) {
                        case 1:
                            result = "1-1";
                            break;
                        case 2:
                            result = "1-2";
                            break;
                    }
                    break;
                case 2:
                    result = "2-x";
                    break;
            }
            return result;
        }
        """,
        "1-2",
        TestName = "NestedSwitch")]
    [TestCase("""
        {
            var outer = 1;
            var inner = 1;
            var log = "";
            switch (outer) {
                case 1:
                    log = log + "outer1-";
                    switch (inner) {
                        case 1:
                            log = log + "inner1";
                            break;
                    }
                    log = log + "-afterinner";
                    break;
            }
            return log;
        }
        """,
        "outer1-inner1-afterinner",
        TestName = "BreakInNestedSwitch_OnlyExitsInner")]
    [TestCase("""
        {
            var flag = true;
            var result = "";
            switch (flag) {
                case true:
                    result = "is true";
                    break;
                case false:
                    result = "is false";
                    break;
            }
            return result;
        }
        """,
        "is true",
        TestName = "BooleanCase_True")]
    [TestCase("""
        {
            var flag = false;
            var result = "";
            switch (flag) {
                case true:
                    result = "is true";
                    break;
                case false:
                    result = "is false";
                    break;
            }
            return result;
        }
        """,
        "is false",
        TestName = "BooleanCase_False")]
    [TestCase("""
        {
            var x = 1;
            switch (x) {
            }
            return "after switch";
        }
        """,
        "after switch",
        TestName = "EmptySwitch_NoError")]
    [TestCase("""
        {
            var x = 1;
            var result = "";
            switch (x) {
                case 1:
                case 2:
                    result = "one or two";
                    break;
                case 3:
                    result = "three";
                    break;
            }
            return result;
        }
        """,
        "one or two",
        TestName = "EmptyCase_FallsThroughToNext")]
    [TestCase("""
        {
            var x = 2;
            var result = "";
            switch (x) {
                case 1:
                case 2:
                case 3:
                    result = "1, 2, or 3";
                    break;
                default:
                    result = "other";
                    break;
            }
            return result;
        }
        """,
        "1, 2, or 3",
        TestName = "MultipleFallThroughCases")]
    [TestCase("""
        {
            var x = 1000000000000;
            var result = "";
            switch (x) {
                case 1000000000000:
                    result = "trillion";
                    break;
                default:
                    result = "other";
                    break;
            }
            return result;
        }
        """,
        "trillion",
        TestName = "LongValues")]
    [TestCase("""
        {
            var x = 3.14;
            var result = "";
            switch (x) {
                case 3.14:
                    result = "pi";
                    break;
                case 2.71:
                    result = "e";
                    break;
                default:
                    result = "other";
                    break;
            }
            return result;
        }
        """,
        "pi",
        TestName = "DoubleValues")]
    [TestCase("""
        {
            var day = 3;
            var dayName = "";
            switch (day) {
                case 0:
                    dayName = "Sunday";
                    break;
                case 1:
                    dayName = "Monday";
                    break;
                case 2:
                    dayName = "Tuesday";
                    break;
                case 3:
                    dayName = "Wednesday";
                    break;
                case 4:
                    dayName = "Thursday";
                    break;
                case 5:
                    dayName = "Friday";
                    break;
                case 6:
                    dayName = "Saturday";
                    break;
                default:
                    dayName = "Invalid";
                    break;
            }
            return dayName;
        }
        """,
        "Wednesday",
        TestName = "DayOfWeek_Scenario")]
    [TestCase("""
        {
            var category = 8;
            var grade = "";
            switch (category) {
                case 10:
                case 9:
                    grade = "A";
                    break;
                case 8:
                    grade = "B";
                    break;
                case 7:
                    grade = "C";
                    break;
                case 6:
                    grade = "D";
                    break;
                default:
                    grade = "F";
                    break;
            }
            return grade;
        }
        """,
        "B",
        TestName = "GradeCalculation_Scenario")]
    [TestCase("""
        {
            var statusCode = 404;
            var message = "";
            switch (statusCode) {
                case 200:
                    message = "OK";
                    break;
                case 201:
                    message = "Created";
                    break;
                case 400:
                    message = "Bad Request";
                    break;
                case 401:
                    message = "Unauthorized";
                    break;
                case 403:
                    message = "Forbidden";
                    break;
                case 404:
                    message = "Not Found";
                    break;
                case 500:
                    message = "Internal Server Error";
                    break;
                default:
                    message = "Unknown Status";
                    break;
            }
            return message;
        }
        """,
        "Not Found",
        TestName = "HttpStatus_Scenario")]
    [TestCase("""
        {
            var x = 0;
            var result = "";
            switch (x) {
                case 0:
                    result = "zero";
                    break;
                case 1:
                    result = "one";
                    break;
            }
            return result;
        }
        """,
        "zero",
        TestName = "ZeroValue")]
    [TestCase("""
        {
            var x = -1;
            var result = "";
            switch (x) {
                case -1:
                    result = "minus one";
                    break;
                case 0:
                    result = "zero";
                    break;
                case 1:
                    result = "one";
                    break;
            }
            return result;
        }
        """,
        "minus one",
        TestName = "NegativeValue")]
    [TestCase("""
        {
            var x = "";
            var result = "";
            switch (x) {
                case "":
                    result = "empty";
                    break;
                default:
                    result = "not empty";
                    break;
            }
            return result;
        }
        """,
        "empty",
        TestName = "EmptyString")]
    [TestCase("""
        {
            var x = 1;
            var count = 0;
            switch (x) {
                case 1:
                    x = 2;
                    count = count + 1;
                    break;
                case 2:
                    count = count + 10;
                    break;
            }
            return count;
        }
        """,
        1,
        TestName = "CaseValueChangesAfterMatch")]
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

    [TestCase("""
        {
            var a = 10.0;
            var b = 3.0;
            var op = "/";
            var calcResult = 0.0;
            switch (op) {
                case "+":
                    calcResult = a + b;
                    break;
                case "-":
                    calcResult = a - b;
                    break;
                case "*":
                    calcResult = a * b;
                    break;
                case "/":
                    calcResult = a / b;
                    break;
                default:
                    calcResult = 0;
                    break;
            }
            return calcResult;
        }
        """,
        TestName = "Switch_CalculatorOperation_Scenario")]
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
