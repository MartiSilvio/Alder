using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class WhileLoopTests
{
    #region Basic While Loop

    [Test]
    public void WhileLoop_BasicCounter_CountsCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < 5) {
                count = count + 1;
                i = i + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_FalseCondition_NeverExecutes()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var executed = false;
            while (false) {
                executed = true;
            }
            return executed;
        }");

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void WhileLoop_SingleIteration_ExecutesOnce()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var done = false;
            while (done == false) {
                count = count + 1;
                done = true;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region While Loop with Variables

    [Test]
    public void WhileLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("limit", 10L);

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            while (i <= limit) {
                sum = sum + i;
                i = i + 1;
            }
            return sum;
        }");

        // Sum of 1 to 10 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void WhileLoop_WithMultipleVariables_TracksAll()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 1;
            var count = 0;
            while (count < 10) {
                var temp = a + b;
                a = b;
                b = temp;
                count = count + 1;
            }
            return b;
        }");

        // Fibonacci: 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
        Assert.That(result, Is.EqualTo(89));
    }

    [Test]
    public void WhileLoop_VariablesScopedToBlock()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (i < 3) {
                var local = i * 2;
                total = total + local;
                i = i + 1;
            }
            return total;
        }");

        // 0*2 + 1*2 + 2*2 = 0 + 2 + 4 = 6
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region While Loop with Complex Conditions

    [Test]
    public void WhileLoop_WithAndCondition_EvaluatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var y = 0;
            while (x < 5 && y < 3) {
                x = x + 1;
                y = y + 1;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_WithOrCondition_EvaluatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var y = 10;
            while (x < 5 || y > 5) {
                x = x + 1;
                y = y - 1;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_WithNullCheck_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var text = ""test"";
            var count = 0;
            while (text != null && count < 3) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_WithComparisonExpression_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var numbers = [10, 5, 15, 3];
            var idx = 0;
            var found = -1;
            while (idx < 4 && found == -1) {
                if (numbers[idx] == 15) {
                    found = idx;
                }
                idx = idx + 1;
            }
            return found;
        }");

        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region While Loop with Return

    [Test]
    public void WhileLoop_WithEarlyReturn_ExitsImmediately()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            while (i < 100) {
                if (i == 5) {
                    return i;
                }
                i = i + 1;
            }
            return -1;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("target", 7L);

        var result = engine.Evaluate(@"
        {
            var i = 0;
            while (i < 20) {
                if (i == target) {
                    return $""Found at {i}"";
                }
                i = i + 1;
            }
            return ""Not found"";
        }");

        Assert.That(result, Is.EqualTo("Found at 7"));
    }

    #endregion

    #region While Loop with Nested Structures

    [Test]
    public void WhileLoop_WithNestedIf_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            while (i < 10) {
                if (i % 2 == 0) {
                    sum = sum + i;
                }
                i = i + 1;
            }
            return sum;
        }");

        // Sum of even numbers 0-9: 0 + 2 + 4 + 6 + 8 = 20
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void WhileLoop_Nested_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (i < 3) {
                var j = 0;
                while (j < 3) {
                    total = total + 1;
                    j = j + 1;
                }
                i = i + 1;
            }
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void WhileLoop_DeeplyNested_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var product = 1;
            var i = 1;
            while (i <= 2) {
                var j = 1;
                while (j <= 2) {
                    var k = 1;
                    while (k <= 2) {
                        product = product * 2;
                        k = k + 1;
                    }
                    j = j + 1;
                }
                i = i + 1;
            }
            return product;
        }");

        // 2^8 = 256
        Assert.That(result, Is.EqualTo(256));
    }

    #endregion

    #region While Loop with Collections

    [Test]
    public void WhileLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3, 4, 5];
            var sum = 0;
            var i = 0;
            while (i < 5) {
                sum = sum + arr[i];
                i = i + 1;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void WhileLoop_WithListCount_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new List<object?> { 10L, 20L, 30L, 40L });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            while (i < items.Count()) {
                sum = sum + items[i];
                i = i + 1;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void WhileLoop_BuildingList_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var result = [];
            var i = 0;
            while (i < 5) {
                result = [...result, i * 2];
                i = i + 1;
            }
            return result;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 0L, 2L, 4L, 6L, 8L }));
    }

    #endregion

    #region While Loop with LINQ

    [Test]
    public void WhileLoop_WithLinqSum_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var numbers = [];
            var i = 1;
            while (i <= 5) {
                numbers = [...numbers, i];
                i = i + 1;
            }
            return numbers.Sum();
        }");

        Assert.That(Convert.ToDouble(result), Is.EqualTo(15.0));
    }

    [Test]
    public void WhileLoop_WithLinqWhere_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var numbers = [];
            var i = 1;
            while (i <= 10) {
                numbers = [...numbers, i];
                i = i + 1;
            }
            return numbers.Where(x => x % 2 == 0).Count();
        }");

        Assert.That(Convert.ToInt32(result), Is.EqualTo(5));
    }

    #endregion

    #region While Loop with Module Access

    [Test]
    public void WhileLoop_WithMathModule_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0.0;
            var i = 1;
            while (i <= 5) {
                sum = sum + Math.Sqrt(i);
                i = i + 1;
            }
            return sum;
        }");

        // sqrt(1) + sqrt(2) + sqrt(3) + sqrt(4) + sqrt(5) = 1 + 1.414 + 1.732 + 2 + 2.236 ≈ 8.382
        Assert.That(Convert.ToDouble(result), Is.EqualTo(Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) + Math.Sqrt(5)).Within(0.001));
    }

    [Test]
    public void WhileLoop_WithDateTimeModule_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < 3) {
                var now = DateTime.Now;
                if (now != null) {
                    count = count + 1;
                }
                i = i + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region While Loop with Strings

    [Test]
    public void WhileLoop_StringConcatenation_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var str = """";
            var i = 0;
            while (i < 5) {
                str = str + i;
                i = i + 1;
            }
            return str;
        }");

        Assert.That(result, Is.EqualTo("01234"));
    }

    [Test]
    public void WhileLoop_InterpolatedString_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var lines = """";
            var i = 1;
            while (i <= 3) {
                lines = $""{lines}Line {i}\n"";
                i = i + 1;
            }
            return lines;
        }");

        Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3\n"));
    }

    #endregion

    #region While Loop with Anonymous Objects

    [Test]
    public void WhileLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var lastObj = null;
            while (i < 3) {
                lastObj = new { Index = i, Squared = i * i };
                i = i + 1;
            }
            return lastObj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    #endregion

    #region While Loop Single Statement Body

    [Test]
    public void WhileLoop_SingleStatementBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < 5)
                i = i + 1;
            return i;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region While Loop Safety

    [Test]
    public void WhileLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = new CsEvalEngine();

        var ex = Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                while (true) {
                    i = i + 1;
                }
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("maximum iterations"));
    }

    [Test]
    public void WhileLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 10 });

        var ex = Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                while (true) {
                    i = i + 1;
                }
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void WhileLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 0 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < 200000) {
                count = count + 1;
                i = i + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(200000));
    }

    [Test]
    public void WhileLoop_WithCancellationToken_CanBeCancelled()
    {
        // Use disabled iteration limit so cancellation is the only way to stop
        var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 0 });
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate(@"
            {
                var i = 0;
                while (i < 1000000000) {
                    i = i + 1;
                }
                return i;
            }", null, cts.Token);
        });

        Thread.Sleep(100); // Give it time to start
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region While Loop Edge Cases

    [Test]
    public void WhileLoop_EmptyBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 5;
            while (i < 3) {
            }
            return i;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_WithTernaryCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("useLimit", true);

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < (useLimit ? 5 : 10)) {
                count = count + 1;
                i = i + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_ConditionEvaluatedEachIteration()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var limit = 5;
            var i = 0;
            var sum = 0;
            while (i < limit) {
                sum = sum + i;
                i = i + 1;
                if (i == 3) {
                    limit = 3;
                }
            }
            return sum;
        }");

        // Iterations: i=0 (sum=0), i=1 (sum=1), i=2 (sum=3), then limit becomes 3, loop exits
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_WithNullCoalesce_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("maybeNull", null);

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var num = maybeNull ?? 5;
            while (num > 0) {
                count = count + 1;
                num = num - 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region While Loop with Arithmetic

    [Test]
    public void WhileLoop_Factorial_CalculatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var n = 5;
            var factorial = 1;
            while (n > 1) {
                factorial = factorial * n;
                n = n - 1;
            }
            return factorial;
        }");

        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public void WhileLoop_GCD_CalculatesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var a = 48;
            var b = 18;
            while (b != 0) {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }");

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void WhileLoop_PowerCalculation_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var baseNum = 2;
            var exp = 10;
            var power = 1;
            while (exp > 0) {
                power = power * baseNum;
                exp = exp - 1;
            }
            return power;
        }");

        Assert.That(result, Is.EqualTo(1024));
    }

    #endregion

    #region While Loop Parsing Tests

    [Test]
    public void WhileLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ var i = 0; while (i < 5) { i = i + 1; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_TryParse_MissingParenthesis_Fails()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ while i < 5 { } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void WhileLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"
        {
            var sum = 0;
            var i = 0;
            while (i < limit) {
                sum = sum + i;
                i = i + 1;
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

    #region While Loop with Break

    [Test]
    public void WhileLoop_Break_ExitsLoop()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            while (i < 100) {
                if (i == 5) {
                    break;
                }
                i = i + 1;
            }
            return i;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_Break_AtStart_ExitsImmediately()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            while (true) {
                break;
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void WhileLoop_Break_PreservesVariableState()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            while (i <= 10) {
                sum = sum + i;
                if (sum > 10) {
                    break;
                }
                i = i + 1;
            }
            return sum;
        }");

        // 1 + 2 + 3 + 4 + 5 = 15 > 10, breaks
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void WhileLoop_Break_InNestedIf_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var found = -1;
            while (i < 20) {
                if (i % 7 == 0 && i > 0) {
                    found = i;
                    break;
                }
                i = i + 1;
            }
            return found;
        }");

        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void WhileLoop_Break_OnlyExitsInnerLoop()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var outerCount = 0;
            var totalInner = 0;
            var i = 0;
            while (i < 3) {
                var j = 0;
                while (j < 10) {
                    if (j == 2) {
                        break;
                    }
                    totalInner = totalInner + 1;
                    j = j + 1;
                }
                outerCount = outerCount + 1;
                i = i + 1;
            }
            return outerCount * 100 + totalInner;
        }");

        // outerCount = 3, totalInner = 2 * 3 = 6
        Assert.That(result, Is.EqualTo(306));
    }

    [Test]
    public void WhileLoop_Break_WithCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("target", 42L);

        var result = engine.Evaluate(@"
        {
            var arr = [10, 20, 42, 50, 60];
            var i = 0;
            var foundIndex = -1;
            while (i < 5) {
                if (arr[i] == target) {
                    foundIndex = i;
                    break;
                }
                i = i + 1;
            }
            return foundIndex;
        }");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void WhileLoop_Break_AfterSomeIterations()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var iterations = 0;
            var i = 0;
            while (i < 1000) {
                iterations = iterations + 1;
                i = i + 1;
                if (i >= 50) {
                    break;
                }
            }
            return iterations;
        }");

        Assert.That(result, Is.EqualTo(50));
    }

    #endregion

    #region While Loop with Continue

    [Test]
    public void WhileLoop_Continue_SkipsRemainingBody()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            while (i < 10) {
                i = i + 1;
                if (i % 2 == 0) {
                    continue;
                }
                sum = sum + i;
            }
            return sum;
        }");

        // Sum of odd numbers 1-9: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void WhileLoop_Continue_WithMultipleConditions()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            while (i < 20) {
                i = i + 1;
                if (i % 2 == 0) {
                    continue;
                }
                if (i % 3 == 0) {
                    continue;
                }
                sum = sum + i;
            }
            return sum;
        }");

        // Numbers 1-20 not divisible by 2 or 3: 1, 5, 7, 11, 13, 17, 19 = 73
        Assert.That(result, Is.EqualTo(73));
    }

    [Test]
    public void WhileLoop_Continue_InNestedLoop_OnlyAffectsInner()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (i < 3) {
                var j = 0;
                while (j < 5) {
                    j = j + 1;
                    if (j == 3) {
                        continue;
                    }
                    total = total + 1;
                }
                i = i + 1;
            }
            return total;
        }");

        // Each inner loop: 4 iterations counted (1,2,4,5 - skip 3), outer: 3 iterations = 12
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void WhileLoop_Continue_SkipsToConditionCheck()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var skipped = 0;
            var processed = 0;
            var i = 0;
            while (i < 10) {
                i = i + 1;
                if (i <= 5) {
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
    public void WhileLoop_Continue_WithAccumulator()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var product = 1;
            var i = 0;
            while (i < 10) {
                i = i + 1;
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

    [Test]
    public void WhileLoop_Continue_DoesNotAffectCondition()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var iterations = 0;
            var i = 0;
            while (i < 5) {
                iterations = iterations + 1;
                i = i + 1;
                continue;
                count = count + 1;
            }
            return iterations * 100 + count;
        }");

        // iterations = 5, count = 0 (never reached due to continue)
        Assert.That(result, Is.EqualTo(500));
    }

    #endregion

    #region While Loop with Break and Continue Combined

    [Test]
    public void WhileLoop_BreakAndContinue_Combined()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            while (true) {
                i = i + 1;
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
    public void WhileLoop_BreakAndContinue_InNestedLoops()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (i < 5) {
                i = i + 1;
                if (i == 3) {
                    continue;
                }
                var j = 0;
                while (j < 5) {
                    j = j + 1;
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

    [Test]
    public void WhileLoop_BreakAndContinue_FindFirstMatch()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var data = [1, -2, 3, -4, 5, -6, 7];
            var i = 0;
            var firstPositiveAfterNegative = -1;
            var sawNegative = false;
            while (i < 7) {
                var val = data[i];
                i = i + 1;
                if (val < 0) {
                    sawNegative = true;
                    continue;
                }
                if (sawNegative) {
                    firstPositiveAfterNegative = val;
                    break;
                }
            }
            return firstPositiveAfterNegative;
        }");

        // First positive after negative: 3 (after -2)
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_MultipleContinues_InSameIteration()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            while (i < 10) {
                i = i + 1;
                if (i < 3) {
                    continue;
                }
                if (i > 7) {
                    continue;
                }
                count = count + 1;
            }
            return count;
        }");

        // Only 3, 4, 5, 6, 7 pass both conditions = 5
        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Break and Continue Parsing

    [Test]
    public void WhileLoop_Break_TryParse_Succeeds()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ var i = 0; while (true) { break; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_Continue_TryParse_Succeeds()
    {
        var engine = new CsEvalEngine();
        var success = engine.TryParse("{ var i = 0; while (i < 5) { i = i + 1; continue; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_BreakWithSemicolon_ParsesCorrectly()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate(@"
        {
            var i = 0;
            while (i < 10) {
                i = i + 1;
                break;
            }
            return i;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    #endregion
}
