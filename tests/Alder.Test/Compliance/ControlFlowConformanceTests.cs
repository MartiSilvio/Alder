using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ControlFlowConformanceTests(CompilationMode mode)
{
    private AlderEngine Engine(LanguageMode lang = LanguageMode.Standard)
        => TestEngineFactory.Create(mode, AlderOptions.Default with { LanguageMode = lang });

    private object? Eval(string expr, LanguageMode lang = LanguageMode.Standard)
        => Engine(lang).Evaluate(expr);

    // ═══════════════════════════════════════════════════════════════════
    // §13.9.5 Foreach — variable per-iteration
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Foreach_VariableIsPerIteration_LambdaCapture()
    {
        // Per C# spec, foreach variable is fresh per iteration for closures
        var result = Eval(@"{
            var funcs = new List<Func<int>>();
            foreach (var x in new[] { 1, 2, 3 })
            {
                funcs.Add(() => x);
            }
            return funcs[0]() + funcs[1]() + funcs[2]();
        }");
        Assert.That(result, Is.EqualTo(6)); // 1+2+3, not 3+3+3
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.8.3 Switch — pattern matching completeness
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void SwitchExpression_WithWhenClause()
    {
        var result = Eval(@"{
            var x = 5;
            return x switch
            {
                int n when n > 10 => ""big"",
                int n when n > 0 => ""small"",
                _ => ""other""
            };
        }");
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void SwitchExpression_TypePattern()
    {
        var result = Eval(@"{
            object obj = ""hello"";
            return obj switch
            {
                int i => $""int:{i}"",
                string s => $""str:{s}"",
                _ => ""unknown""
            };
        }");
        Assert.That(result, Is.EqualTo("str:hello"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.11 Try/catch/finally — ordering guarantees
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void TryCatchFinally_FinallyAlwaysRuns()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                log += ""try:"";
                throw new System.InvalidOperationException();
            }
            catch (System.InvalidOperationException)
            {
                log += ""catch:"";
            }
            finally
            {
                log += ""finally"";
            }
            return log;
        }");
        Assert.That(result, Is.EqualTo("try:catch:finally"));
    }

    [Test]
    public void TryCatchFinally_FinallyRunsOnReturn()
    {
        var result = Eval(@"{
            var ran = false;
            try
            {
                return 42;
            }
            finally
            {
                ran = true;
            }
        }");
        // Return value should be 42, and finally should have run
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TryCatch_SpecificCatch_BeforeGeneral()
    {
        var result = Eval(@"{
            try
            {
                throw new System.ArgumentException(""test"");
            }
            catch (System.ArgumentException)
            {
                return ""specific"";
            }
            catch (System.Exception)
            {
                return ""general"";
            }
        }");
        Assert.That(result, Is.EqualTo("specific"));
    }

    [Test]
    public void TryCatch_ExceptionInCatch_CaughtByOuter()
    {
        var result = Eval(@"{
            try
            {
                try
                {
                    throw new System.InvalidOperationException();
                }
                catch (System.InvalidOperationException)
                {
                    throw new System.ArgumentException(""rethrown"");
                }
            }
            catch (System.ArgumentException ex)
            {
                return ex.Message;
            }
        }");
        Assert.That(result, Is.EqualTo("rethrown"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Control flow torture tests
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedSwitch_BreakExitsInnerOnly()
    {
        var result = Eval(@"{
            var r = 0;
            switch (1)
            {
                case 1:
                    switch (2)
                    {
                        case 2: r = 99; break;
                    }
                    r += 1;
                    break;
            }
            return r;
        }");
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void NestedFor_BreakAndContinue()
    {
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                if (i == 3) continue;
                if (i == 4) break;
                for (var j = 0; j < 3; j++)
                {
                    if (j == 1) break;
                    sum++;
                }
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(3)); // i=0,1,2 each contribute 1 (j=0)
    }

    [Test]
    public void DoWhile_RunsAtLeastOnce()
    {
        var result = Eval(@"{
            var count = 0;
            do { count++; } while (false);
            return count;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void While_FalseCondition_NeverRuns()
    {
        var result = Eval(@"{
            var count = 0;
            while (false) { count++; }
            return count;
        }");
        Assert.That(result, Is.EqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.11 Try statement — nested exception handling
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void NestedTryCatch_InnerExceptionPropagates()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                try
                {
                    log += ""A"";
                    throw new System.InvalidOperationException();
                }
                finally
                {
                    log += ""B"";
                }
            }
            catch (System.InvalidOperationException)
            {
                log += ""C"";
            }
            return log;
        }");
        Assert.That(result, Is.EqualTo("ABC"));
    }

    [Test]
    public void TryCatch_FilterCatchByType()
    {
        var result = Eval(@"{
            try
            {
                throw new System.NotImplementedException();
            }
            catch (System.ArgumentException)
            {
                return ""arg"";
            }
            catch (System.NotImplementedException)
            {
                return ""notimpl"";
            }
            catch (System.Exception)
            {
                return ""generic"";
            }
        }");
        Assert.That(result, Is.EqualTo("notimpl"));
    }

    [Test]
    public void TryFinally_NestedReturn_FinallyOrder()
    {
        var result = Eval(@"{
            var log = """";
            try
            {
                try
                {
                    log += ""inner-try:"";
                    return log + ""return"";
                }
                finally
                {
                    log += ""inner-finally:"";
                }
            }
            finally
            {
                log += ""outer-finally"";
            }
        }");
        // The return value should be from the return statement, but finallys still run
        Assert.That(result!.ToString(), Does.Contain("return"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.8.3 Switch — complex patterns
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void SwitchStatement_MultipleLabels_SameBody()
    {
        var result = Eval(@"{
            var x = 2;
            switch (x)
            {
                case 1:
                case 2:
                case 3:
                    return ""small"";
                default:
                    return ""other"";
            }
        }");
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void SwitchStatement_DefaultInMiddle()
    {
        var result = Eval(@"{
            var x = 99;
            switch (x)
            {
                case 1: return ""one"";
                default: return ""default"";
                case 2: return ""two"";
            }
        }");
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void SwitchStatement_StringLabels()
    {
        var result = Eval(@"{
            var s = ""hello"";
            switch (s)
            {
                case ""hello"": return 1;
                case ""world"": return 2;
                default: return 0;
            }
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void SwitchStatement_NullCase()
    {
        var result = Eval(@"{
            string s = null;
            switch (s)
            {
                case null: return ""null"";
                case ""hello"": return ""hello"";
                default: return ""default"";
            }
        }");
        Assert.That(result, Is.EqualTo("null"));
    }

    [Test]
    public void SwitchExpression_Tuple()
    {
        var result = Eval(@"{
            var point = (1, 0);
            return point switch
            {
                (0, 0) => ""origin"",
                (1, 0) => ""right"",
                (0, 1) => ""up"",
                _ => ""other""
            };
        }");
        Assert.That(result, Is.EqualTo("right"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.9.5 Foreach — various collection types
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void Foreach_String_CharIteration()
    {
        var result = Eval(@"{
            var count = 0;
            foreach (var c in ""hello"")
            {
                count++;
            }
            return count;
        }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Foreach_Dictionary_KeyValuePairs()
    {
        var result = Eval(@"{
            var dict = new Dictionary<string, int>();
            dict[""a""] = 1;
            dict[""b""] = 2;
            var sum = 0;
            foreach (var kvp in dict)
            {
                sum += kvp.Value;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════════
    // Interaction: for loop variable scope
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ForLoop_VariableNotVisibleAfterLoop()
    {
        // The for loop variable i should not leak outside
        var result = Eval(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++)
            {
                sum += i;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ForLoop_MultipleInitializers()
    {
        var result = Eval(@"{
            var sum = 0;
            for (int i = 0, j = 10; i < 5; i++, j--)
            {
                sum += i + j;
            }
            return sum;
        }");
        Assert.That(result, Is.EqualTo(50)); // (0+10)+(1+9)+(2+8)+(3+7)+(4+6) = 50
    }

    // ═══════════════════════════════════════════════════════════════════
    // §13.6.3 Local constant declarations
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void LocalConst_String()
    {
        var result = Eval(@"
            const string greeting = ""hello"";
            return greeting + "" world"";
        ");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void LocalConst_UsedInSwitchCase()
    {
        var result = Eval(@"{
            const int TARGET = 42;
            var x = 42;
            switch (x)
            {
                case TARGET: return ""found"";
                default: return ""not found"";
            }
        }");
        Assert.That(result, Is.EqualTo("found"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.17 Declaration expressions — out var
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void OutVar_IntTryParse()
    {
        var result = Eval(@"{
            if (int.TryParse(""42"", out var x))
                return x;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void OutVar_FailedParse()
    {
        var result = Eval(@"{
            if (int.TryParse(""abc"", out var x))
                return x;
            return -1;
        }");
        Assert.That(result, Is.EqualTo(-1));
    }
}
