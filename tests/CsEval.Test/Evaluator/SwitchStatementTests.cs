namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SwitchStatementTests(CompilationMode mode) : TestBase
{
    #region Basic Switch

    [Test]
    public void Switch_BasicCase_MatchesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 2;
            var result = """";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                case 2:
                    result = ""two"";
                    break;
                case 3:
                    result = ""three"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Switch_FirstCase_MatchesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var result = """";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                case 2:
                    result = ""two"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("one"));
    }

    [Test]
    public void Switch_LastCase_MatchesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 3;
            var result = """";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                case 2:
                    result = ""two"";
                    break;
                case 3:
                    result = ""three"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("three"));
    }

    [Test]
    public void Switch_NoMatch_ResultUnchanged()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var result = ""initial"";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                case 2:
                    result = ""two"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("initial"));
    }

    #endregion

    #region Default Case

    [Test]
    public void Switch_DefaultCase_ExecutesWhenNoMatch()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var result = """";
            switch (x) {
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

        Assert.That(result, Is.EqualTo("other"));
    }

    [Test]
    public void Switch_DefaultCase_SkippedWhenCaseMatches()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var result = """";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                default:
                    result = ""other"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("one"));
    }

    [Test]
    public void Switch_DefaultCaseFirst_StillWorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var result = """";
            switch (x) {
                default:
                    result = ""other"";
                    break;
                case 1:
                    result = ""one"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("other"));
    }

    [Test]
    public void Switch_DefaultCaseMiddle_StillWorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            var result = """";
            switch (x) {
                case 1:
                    result = ""one"";
                    break;
                default:
                    result = ""other"";
                    break;
                case 2:
                    result = ""two"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("other"));
    }

    [Test]
    public void Switch_OnlyDefaultCase_ExecutesAlways()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 42;
            var result = """";
            switch (x) {
                default:
                    result = ""default only"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("default only"));
    }

    #endregion

    #region Fall-Through Behavior

    [Test]
    public void Switch_FallThrough_WithoutBreak()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var result = """";
            switch (x) {
                case 1:
                    result = result + ""one"";
                case 2:
                    result = result + ""two"";
                    break;
                case 3:
                    result = result + ""three"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("onetwo"));
    }

    [Test]
    public void Switch_FallThroughToDefault()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var result = """";
            switch (x) {
                case 1:
                    result = result + ""one"";
                default:
                    result = result + ""default"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("onedefault"));
    }

    [Test]
    public void Switch_FallThroughAllCases()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var count = 0;
            switch (x) {
                case 1:
                    count = count + 1;
                case 2:
                    count = count + 1;
                case 3:
                    count = count + 1;
                    break;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region String Cases

    [Test]
    public void Switch_StringCase_MatchesExactly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var fruit = ""apple"";
            var category = """";
            switch (fruit) {
                case ""apple"":
                    category = ""pome"";
                    break;
                case ""orange"":
                    category = ""citrus"";
                    break;
                case ""banana"":
                    category = ""tropical"";
                    break;
                default:
                    category = ""unknown"";
                    break;
            }
            return category;
        }");

        Assert.That(result, Is.EqualTo("pome"));
    }

    [Test]
    public void Switch_StringCase_CaseSensitive()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var fruit = ""Apple"";
            var category = """";
            switch (fruit) {
                case ""apple"":
                    category = ""lowercase"";
                    break;
                case ""Apple"":
                    category = ""capitalized"";
                    break;
                default:
                    category = ""unknown"";
                    break;
            }
            return category;
        }");

        Assert.That(result, Is.EqualTo("capitalized"));
    }

    [Test]
    public void Switch_StringWithVariable()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("input", "test");
        var result = engine.Evaluate(@"
        {
            var result = """";
            switch (input) {
                case ""test"":
                    result = ""matched test"";
                    break;
                default:
                    result = ""no match"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("matched test"));
    }

    #endregion

    #region Expression Cases

    [Test]
    public void Switch_ExpressionInSwitchValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var a = 3;
            var b = 2;
            var result = """";
            switch (a + b) {
                case 4:
                    result = ""four"";
                    break;
                case 5:
                    result = ""five"";
                    break;
                case 6:
                    result = ""six"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("five"));
    }

    [Test]
    public void Switch_ExpressionInCasePattern()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 10;
            var multiplier = 2;
            var result = """";
            switch (x) {
                case 5 * 2:
                    result = ""matched 5*2"";
                    break;
                case 3 * 4:
                    result = ""matched 3*4"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("matched 5*2"));
    }

    [Test]
    public void Switch_PropertyAccessInSwitch()
    {
        var engine = CreateEngine(mode);
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

    #region Switch with Return

    [Test]
    public void Switch_ReturnInCase_ExitsBlock()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 2;
            switch (x) {
                case 1:
                    return ""one"";
                case 2:
                    return ""two"";
                case 3:
                    return ""three"";
            }
            return ""no match"";
        }");

        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Switch_ReturnInDefault_ExitsBlock()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            switch (x) {
                case 1:
                    return ""one"";
                case 2:
                    return ""two"";
                default:
                    return ""default"";
            }
            return ""after switch"";
        }");

        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void Switch_NoMatchNoDefault_ContinuesAfter()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 99;
            switch (x) {
                case 1:
                    return ""one"";
                case 2:
                    return ""two"";
            }
            return ""no match"";
        }");

        Assert.That(result, Is.EqualTo("no match"));
    }

    #endregion

    #region Multiple Statements in Case

    [Test]
    public void Switch_MultipleStatementsInCase()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
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
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Switch_LoopInsideCase()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
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
        }");

        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4
    }

    [Test]
    public void Switch_IfInsideCase()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var val = 15;
            var category = """";
            switch (x) {
                case 1:
                    if (val > 10) {
                        category = ""high"";
                    } else {
                        category = ""low"";
                    }
                    break;
            }
            return category;
        }");

        Assert.That(result, Is.EqualTo("high"));
    }

    #endregion

    #region Nested Switch

    [Test]
    public void Switch_NestedSwitch()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var outer = 1;
            var inner = 2;
            var result = """";
            switch (outer) {
                case 1:
                    switch (inner) {
                        case 1:
                            result = ""1-1"";
                            break;
                        case 2:
                            result = ""1-2"";
                            break;
                    }
                    break;
                case 2:
                    result = ""2-x"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("1-2"));
    }

    [Test]
    public void Switch_BreakInNestedSwitch_OnlyExitsInner()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var outer = 1;
            var inner = 1;
            var log = """";
            switch (outer) {
                case 1:
                    log = log + ""outer1-"";
                    switch (inner) {
                        case 1:
                            log = log + ""inner1"";
                            break;
                    }
                    log = log + ""-afterinner"";
                    break;
            }
            return log;
        }");

        Assert.That(result, Is.EqualTo("outer1-inner1-afterinner"));
    }

    #endregion

    #region Boolean Cases

    [Test]
    public void Switch_BooleanCase_True()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var flag = true;
            var result = """";
            switch (flag) {
                case true:
                    result = ""is true"";
                    break;
                case false:
                    result = ""is false"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("is true"));
    }

    [Test]
    public void Switch_BooleanCase_False()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var flag = false;
            var result = """";
            switch (flag) {
                case true:
                    result = ""is true"";
                    break;
                case false:
                    result = ""is false"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("is false"));
    }

    #endregion

    #region Null Cases

    [Test]
    public void Switch_NullCase_MatchesNull()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("input", null);
        var result = engine.Evaluate(@"
        {
            var result = """";
            switch (input) {
                case null:
                    result = ""is null"";
                    break;
                default:
                    result = ""not null"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("is null"));
    }

    [Test]
    public void Switch_NullCase_DoesNotMatchValue()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("input", "hello");
        var result = engine.Evaluate(@"
        {
            var result = """";
            switch (input) {
                case null:
                    result = ""is null"";
                    break;
                default:
                    result = ""not null"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("not null"));
    }

    #endregion

    #region Empty Cases and Switch

    [Test]
    public void Switch_EmptySwitch_NoError()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            switch (x) {
            }
            return ""after switch"";
        }");

        Assert.That(result, Is.EqualTo("after switch"));
    }

    [Test]
    public void Switch_EmptyCase_FallsThroughToNext()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            var result = """";
            switch (x) {
                case 1:
                case 2:
                    result = ""one or two"";
                    break;
                case 3:
                    result = ""three"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("one or two"));
    }

    [Test]
    public void Switch_MultipleFallThroughCases()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 2;
            var result = """";
            switch (x) {
                case 1:
                case 2:
                case 3:
                    result = ""1, 2, or 3"";
                    break;
                default:
                    result = ""other"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("1, 2, or 3"));
    }

    #endregion

    #region Switch with Collections

    [Test]
    public void Switch_WithIndexAccess()
    {
        var engine = CreateEngine(mode);
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

    [Test]
    public void Switch_InsideLoop()
    {
        var engine = CreateEngine(mode);
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

        Assert.That(result, Is.EqualTo(221)); // 2 ones, 2 twos, 1 other
    }

    #endregion

    #region Switch with Different Numeric Types

    [Test]
    public void Switch_LongValues()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1000000000000;
            var result = """";
            switch (x) {
                case 1000000000000:
                    result = ""trillion"";
                    break;
                default:
                    result = ""other"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("trillion"));
    }

    [Test]
    public void Switch_DoubleValues()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 3.14;
            var result = """";
            switch (x) {
                case 3.14:
                    result = ""pi"";
                    break;
                case 2.71:
                    result = ""e"";
                    break;
                default:
                    result = ""other"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("pi"));
    }

    #endregion

    #region Parsing Tests

    [Test]
    public void Switch_TryParse_ValidExpression_Succeeds()
    {
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ switch x { case 1: break; } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Switch_TryParse_MissingBrace_Fails()
    {
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ switch (x) case 1: break; }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Switch_TryParse_MissingColon_Fails()
    {
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ switch (x) { case 1 break; } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Switch_PreParsed_CanBeReused()
    {
        var engine = CreateEngine(mode);
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

    #region Real-World Scenarios

    [Test]
    public void Switch_DayOfWeek_Scenario()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var day = 3;
            var dayName = """";
            switch (day) {
                case 0:
                    dayName = ""Sunday"";
                    break;
                case 1:
                    dayName = ""Monday"";
                    break;
                case 2:
                    dayName = ""Tuesday"";
                    break;
                case 3:
                    dayName = ""Wednesday"";
                    break;
                case 4:
                    dayName = ""Thursday"";
                    break;
                case 5:
                    dayName = ""Friday"";
                    break;
                case 6:
                    dayName = ""Saturday"";
                    break;
                default:
                    dayName = ""Invalid"";
                    break;
            }
            return dayName;
        }");

        Assert.That(result, Is.EqualTo("Wednesday"));
    }

    [Test]
    public void Switch_GradeCalculation_Scenario()
    {
        var engine = CreateEngine(mode);
        // Use explicit category value to avoid division type issues
        var result = engine.Evaluate(@"
        {
            var category = 8;
            var grade = """";
            switch (category) {
                case 10:
                case 9:
                    grade = ""A"";
                    break;
                case 8:
                    grade = ""B"";
                    break;
                case 7:
                    grade = ""C"";
                    break;
                case 6:
                    grade = ""D"";
                    break;
                default:
                    grade = ""F"";
                    break;
            }
            return grade;
        }");

        Assert.That(result, Is.EqualTo("B"));
    }

    [Test]
    public void Switch_HttpStatus_Scenario()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var statusCode = 404;
            var message = """";
            switch (statusCode) {
                case 200:
                    message = ""OK"";
                    break;
                case 201:
                    message = ""Created"";
                    break;
                case 400:
                    message = ""Bad Request"";
                    break;
                case 401:
                    message = ""Unauthorized"";
                    break;
                case 403:
                    message = ""Forbidden"";
                    break;
                case 404:
                    message = ""Not Found"";
                    break;
                case 500:
                    message = ""Internal Server Error"";
                    break;
                default:
                    message = ""Unknown Status"";
                    break;
            }
            return message;
        }");

        Assert.That(result, Is.EqualTo("Not Found"));
    }

    [Test]
    public void Switch_CalculatorOperation_Scenario()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var a = 10.0;
            var b = 3.0;
            var op = ""/"";
            var calcResult = 0.0;
            switch (op) {
                case ""+"":
                    calcResult = a + b;
                    break;
                case ""-"":
                    calcResult = a - b;
                    break;
                case ""*"":
                    calcResult = a * b;
                    break;
                case ""/"":
                    calcResult = a / b;
                    break;
                default:
                    calcResult = 0;
                    break;
            }
            return calcResult;
        }");

        // C# behavior: double / double = double
        Assert.That(Convert.ToDouble(result), Is.EqualTo(10.0 / 3.0).Within(0.001));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Switch_ZeroValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var result = """";
            switch (x) {
                case 0:
                    result = ""zero"";
                    break;
                case 1:
                    result = ""one"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("zero"));
    }

    [Test]
    public void Switch_NegativeValue()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = -1;
            var result = """";
            switch (x) {
                case -1:
                    result = ""minus one"";
                    break;
                case 0:
                    result = ""zero"";
                    break;
                case 1:
                    result = ""one"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("minus one"));
    }

    [Test]
    public void Switch_EmptyString()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = """";
            var result = """";
            switch (x) {
                case """":
                    result = ""empty"";
                    break;
                default:
                    result = ""not empty"";
                    break;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo("empty"));
    }

    [Test]
    public void Switch_CaseValueChangesAfterMatch()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
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
        }");

        Assert.That(result, Is.EqualTo(1)); // Only first case executes
    }

    #endregion
}
