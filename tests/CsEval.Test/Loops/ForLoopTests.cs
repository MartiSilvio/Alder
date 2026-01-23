using NUnit.Framework;

namespace CsEval.Test.Loops;

[TestFixture]
public class ForLoopTests
{
    #region Basic For Loop

    [Test]
    public void ForLoop_BasicCounter_CountsCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 5; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_FalseCondition_NeverExecutes()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var executed = false;
            for (var i = 0; i < 0; i = i + 1) {
                executed = true;
            }
            return executed;
        }");

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void ForLoop_SingleIteration_ExecutesOnce()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 1; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ForLoop_CountDown_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 10; i > 0; i = i - 1) {
                sum = sum + i;
            }
            return sum;
        }");

        // 10 + 9 + 8 + ... + 1 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region For Loop with Variables

    [Test]
    public void ForLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("limit", 10L);

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 1; i <= limit; i = i + 1) {
                sum = sum + i;
            }
            return sum;
        }");

        // Sum of 1 to 10 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void ForLoop_LoopVariableAccessible_InBody()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var result = 0;
            for (var i = 1; i <= 5; i = i + 1) {
                result = result + i * i;
            }
            return result;
        }");

        // 1 + 4 + 9 + 16 + 25 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void ForLoop_MultipleVariables_TracksAll()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 0; i < 5; i = i + 1) {
                var squared = i * i;
                total = total + squared;
            }
            return total;
        }");

        // 0 + 1 + 4 + 9 + 16 = 30
        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region For Loop Optional Parts

    [Test]
    public void ForLoop_NoInitializer_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var count = 0;
            for (; i < 5; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_NoCondition_IsInfiniteUntilBreak()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; ; i = i + 1) {
                count = count + 1;
                if (i >= 4) {
                    break;
                }
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_NoIncrement_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 5;) {
                count = count + 1;
                i = i + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_EmptyParts_WithBreak()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (;;) {
                count = count + 1;
                if (count >= 10) {
                    break;
                }
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region For Loop with Complex Conditions

    [Test]
    public void ForLoop_WithAndCondition_EvaluatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 10 && count < 5; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_WithOrCondition_EvaluatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 10;
            for (var i = 0; a < 5 || b > 5; i = i + 1) {
                a = a + 1;
                b = b - 1;
            }
            return a;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_WithTernaryInCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("useShort", true);

        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < (useShort ? 3 : 10); i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region For Loop with Return

    [Test]
    public void ForLoop_WithEarlyReturn_ExitsImmediately()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            for (var i = 0; i < 100; i = i + 1) {
                if (i == 5) {
                    return i;
                }
            }
            return -1;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("target", 7L);

        var result = engine.Evaluate(@"
        {
            for (var i = 0; i < 20; i = i + 1) {
                if (i == target) {
                    return $""Found at {i}"";
                }
            }
            return ""Not found"";
        }");

        Assert.That(result, Is.EqualTo("Found at 7"));
    }

    #endregion

    #region For Loop with Nested Structures

    [Test]
    public void ForLoop_WithNestedIf_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 10; i = i + 1) {
                if (i % 2 == 0) {
                    sum = sum + i;
                }
            }
            return sum;
        }");

        // Sum of even numbers 0-9: 0 + 2 + 4 + 6 + 8 = 20
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void ForLoop_Nested_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 0; i < 3; i = i + 1) {
                for (var j = 0; j < 3; j = j + 1) {
                    total = total + 1;
                }
            }
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void ForLoop_DeeplyNested_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var product = 1;
            for (var i = 0; i < 2; i = i + 1) {
                for (var j = 0; j < 2; j = j + 1) {
                    for (var k = 0; k < 2; k = k + 1) {
                        product = product * 2;
                    }
                }
            }
            return product;
        }");

        // 2^8 = 256
        Assert.That(result, Is.EqualTo(256));
    }

    [Test]
    public void ForLoop_NestedWithWhile_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 0; i < 3; i = i + 1) {
                var j = 0;
                while (j < 3) {
                    total = total + 1;
                    j = j + 1;
                }
            }
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    #endregion

    #region For Loop with Collections

    [Test]
    public void ForLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3, 4, 5];
            var sum = 0;
            for (var i = 0; i < 5; i = i + 1) {
                sum = sum + arr[i];
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForLoop_WithListCount_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new List<object?> { 10L, 20L, 30L, 40L });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < items.Count(); i = i + 1) {
                sum = sum + items[i];
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void ForLoop_BuildingList_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var result = [];
            for (var i = 0; i < 5; i = i + 1) {
                result = [...result, i * 2];
            }
            return result;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 0, 2, 4, 6, 8 }));
    }

    #endregion

    #region For Loop with LINQ

    [Test]
    public void ForLoop_WithLinqSum_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var numbers = [];
            for (var i = 1; i <= 5; i = i + 1) {
                numbers = [...numbers, i];
            }
            return numbers.Sum();
        }");

        Assert.That(Convert.ToDouble(result), Is.EqualTo(15.0));
    }

    [Test]
    public void ForLoop_WithLinqWhere_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var numbers = [];
            for (var i = 1; i <= 10; i = i + 1) {
                numbers = [...numbers, i];
            }
            return numbers.Where(x => x % 2 == 0).Count();
        }");

        Assert.That(Convert.ToInt32(result), Is.EqualTo(5));
    }

    #endregion

    #region For Loop with Module Access

    [Test]
    public void ForLoop_WithMathModule_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0.0;
            for (var i = 1; i <= 5; i = i + 1) {
                sum = sum + Math.Sqrt(i);
            }
            return sum;
        }");

        Assert.That(Convert.ToDouble(result), Is.EqualTo(Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) + Math.Sqrt(5)).Within(0.001));
    }

    #endregion

    #region For Loop with Strings

    [Test]
    public void ForLoop_StringConcatenation_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var str = """";
            for (var i = 0; i < 5; i = i + 1) {
                str = str + i;
            }
            return str;
        }");

        Assert.That(result, Is.EqualTo("01234"));
    }

    [Test]
    public void ForLoop_InterpolatedString_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var lines = """";
            for (var i = 1; i <= 3; i = i + 1) {
                lines = $""{lines}Line {i}\n"";
            }
            return lines;
        }");

        Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3\n"));
    }

    #endregion

    #region For Loop with Anonymous Objects

    [Test]
    public void ForLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var lastObj = null;
            for (var i = 0; i < 3; i = i + 1) {
                lastObj = new { Index = i, Squared = i * i };
            }
            return lastObj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    #endregion

    #region For Loop Single Statement Body

    [Test]
    public void ForLoop_SingleStatementBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; i = i + 1)
                sum = sum + i;
            return sum;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region For Loop Safety

    [Test]
    public void ForLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = new CsEvalEngine();

        var ex = Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; ; i = i + 1) {
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("maximum iterations"));
    }

    [Test]
    public void ForLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 10 });

        var ex = Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; ; i = i + 1) {
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void ForLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 0 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 200000; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(200000));
    }

    [Test]
    public void ForLoop_WithCancellationToken_CanBeCancelled()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 0 });
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate(@"
            {
                for (var i = 0; i < 1000000000; i = i + 1) {
                }
                return 0;
            }", null, cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region For Loop Edge Cases

    [Test]
    public void ForLoop_EmptyBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var x = 0;
            for (var i = 0; i < 5; i = i + 1) {
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ForLoop_StepByTwo_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 10; i = i + 2) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_NegativeStep_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 5; i > 0; i = i - 1) {
                sum = sum + i;
            }
            return sum;
        }");

        // 5 + 4 + 3 + 2 + 1 = 15
        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region For Loop with Arithmetic

    [Test]
    public void ForLoop_Factorial_CalculatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var factorial = 1;
            for (var n = 5; n > 1; n = n - 1) {
                factorial = factorial * n;
            }
            return factorial;
        }");

        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public void ForLoop_PowerCalculation_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var power = 1;
            for (var i = 0; i < 10; i = i + 1) {
                power = power * 2;
            }
            return power;
        }");

        Assert.That(result, Is.EqualTo(1024));
    }

    [Test]
    public void ForLoop_Fibonacci_CalculatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 1;
            for (var i = 0; i < 10; i = i + 1) {
                var temp = a + b;
                a = b;
                b = temp;
            }
            return b;
        }");

        // Fibonacci: 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
        Assert.That(result, Is.EqualTo(89));
    }

    #endregion

    #region For Loop Parsing Tests

    [Test]
    public void ForLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ for (var i = 0; i < 5; i = i + 1) { } return 0; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ForLoop_TryParse_MissingParenthesis_Fails()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ for var i = 0; i < 5; i = i + 1 { } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void ForLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"
        {
            var sum = 0;
            for (var i = 0; i < limit; i = i + 1) {
                sum = sum + i;
            }
            return sum;
        }");

        engine.SetVariable("limit", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10)); // 0+1+2+3+4

        engine.SetVariable("limit", 10L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(45)); // 0+1+2+...+9
    }

    #endregion

    #region For Loop with Break

    [Test]
    public void ForLoop_Break_ExitsLoop()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var lastI = -1;
            for (var i = 0; i < 100; i = i + 1) {
                lastI = i;
                if (i == 5) {
                    break;
                }
            }
            return lastI;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ForLoop_Break_AtStart_ExitsImmediately()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 100; i = i + 1) {
                break;
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ForLoop_Break_PreservesVariableState()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 1; i <= 10; i = i + 1) {
                sum = sum + i;
                if (sum > 10) {
                    break;
                }
            }
            return sum;
        }");

        // 1 + 2 + 3 + 4 + 5 = 15 > 10, breaks
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForLoop_Break_OnlyExitsInnerLoop()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var outerCount = 0;
            var totalInner = 0;
            for (var i = 0; i < 3; i = i + 1) {
                for (var j = 0; j < 10; j = j + 1) {
                    if (j == 2) {
                        break;
                    }
                    totalInner = totalInner + 1;
                }
                outerCount = outerCount + 1;
            }
            return outerCount * 100 + totalInner;
        }");

        // outerCount = 3, totalInner = 2 * 3 = 6
        Assert.That(result, Is.EqualTo(306));
    }

    #endregion

    #region For Loop with Continue

    [Test]
    public void ForLoop_Continue_SkipsRemainingBody()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 1; i <= 10; i = i + 1) {
                if (i % 2 == 0) {
                    continue;
                }
                sum = sum + i;
            }
            return sum;
        }");

        // Sum of odd numbers 1-10: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void ForLoop_Continue_StillExecutesIncrement()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var skipped = 0;
            var processed = 0;
            for (var i = 0; i < 10; i = i + 1) {
                if (i < 5) {
                    skipped = skipped + 1;
                    continue;
                }
                processed = processed + 1;
            }
            return skipped * 100 + processed;
        }");

        // skipped = 5, processed = 5
        Assert.That(result, Is.EqualTo(505));
    }

    [Test]
    public void ForLoop_Continue_InNestedLoop_OnlyAffectsInner()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 0; i < 3; i = i + 1) {
                for (var j = 0; j < 5; j = j + 1) {
                    if (j == 3) {
                        continue;
                    }
                    total = total + 1;
                }
            }
            return total;
        }");

        // Each inner loop: 4 iterations counted (0,1,2,4 - skip 3), outer: 3 iterations = 12
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void ForLoop_Continue_WithAccumulator()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var product = 1;
            for (var i = 1; i <= 10; i = i + 1) {
                if (i % 2 == 0) {
                    continue;
                }
                product = product * i;
            }
            return product;
        }");

        // 1 * 3 * 5 * 7 * 9 = 945
        Assert.That(result, Is.EqualTo(945));
    }

    #endregion

    #region For Loop with Break and Continue Combined

    [Test]
    public void ForLoop_BreakAndContinue_Combined()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 1; i <= 20; i = i + 1) {
                if (i % 2 == 0) {
                    continue;
                }
                if (i > 10) {
                    break;
                }
                sum = sum + i;
            }
            return sum;
        }");

        // Odd numbers 1-10: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void ForLoop_BreakAndContinue_InNestedLoops()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 1; i <= 5; i = i + 1) {
                if (i == 3) {
                    continue;
                }
                for (var j = 1; j <= 5; j = j + 1) {
                    if (j == 2) {
                        break;
                    }
                    total = total + 1;
                }
            }
            return total;
        }");

        // Outer: 1,2,4,5 (skip 3) = 4 iterations
        // Inner: only j=1 counted before break = 1 per outer
        // Total = 4
        Assert.That(result, Is.EqualTo(4));
    }

    #endregion
}
