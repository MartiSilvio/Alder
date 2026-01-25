namespace CsEval.Test.Compilation;

/// <summary>
/// Tests for IL-compiled control flow expressions.
/// The ILCompiler generates native IL using DynamicMethod for loops
/// and other control flow, which is significantly faster than tree-walking.
/// </summary>
[TestFixture]
public class ILCompilationTests
{
    #region For Loop Compilation

    [Test]
    public void ILCompile_ForLoop_Simple()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var sum = 0; for (var i = 0; i < 5; i++) { sum = sum + i; } return sum; }");

        Assert.That(expr.TryCompile(), Is.True, "For loops should be IL-compilable");

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4 = 10
    }

    [Test]
    public void ILCompile_ForLoop_WithBreak()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var sum = 0; for (var i = 0; i < 100; i++) { if (i >= 5) break; sum = sum + i; } return sum; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4 = 10
    }

    [Test]
    public void ILCompile_ForLoop_WithContinue()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var sum = 0; for (var i = 0; i < 5; i++) { if (i == 2) continue; sum = sum + i; } return sum; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(8)); // 0+1+3+4 = 8 (skipped 2)
    }

    #endregion

    #region While Loop Compilation

    [Test]
    public void ILCompile_WhileLoop_Simple()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var i = 0; while (i < 5) { i = i + 1; } return i; }");

        Assert.That(expr.TryCompile(), Is.True, "While loops should be IL-compilable");

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ILCompile_WhileLoop_WithBreak()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var i = 0; while (true) { i = i + 1; if (i >= 5) break; } return i; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ILCompile_WhileLoop_WithContinue()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var sum = 0;
            var i = 0;
            while (i < 5) {
                i = i + 1;
                if (i == 3) continue;
                sum = sum + i;
            }
            return sum;
        }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(12)); // 1+2+4+5 = 12 (skipped 3)
    }

    #endregion

    #region Do-While Loop Compilation

    [Test]
    public void ILCompile_DoWhileLoop_Simple()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var i = 0; do { i = i + 1; } while (i < 5); return i; }");

        Assert.That(expr.TryCompile(), Is.True, "Do-while loops should be IL-compilable");

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void ILCompile_DoWhileLoop_ExecutesAtLeastOnce()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var i = 10; do { i = i + 1; } while (i < 5); return i; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(11)); // Started at 10, executed once
    }

    #endregion

    #region ForEach Loop Compilation

    [Test]
    public void ILCompile_ForEachLoop_Simple()
    {
        var engine = new CsEvalEngine()
            .SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });
        var expr = engine.Parse("{ var sum = 0; foreach (var item in items) { sum = sum + item; } return sum; }");

        Assert.That(expr.TryCompile(), Is.True, "ForEach loops should be IL-compilable");

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(15)); // 1+2+3+4+5 = 15
    }

    [Test]
    public void ILCompile_ForEachLoop_WithBreak()
    {
        var engine = new CsEvalEngine()
            .SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });
        var expr = engine.Parse("{ var sum = 0; foreach (var item in items) { if (item > 3) break; sum = sum + item; } return sum; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(6)); // 1+2+3 = 6
    }

    #endregion

    #region Nested Loops Compilation

    [Test]
    public void ILCompile_NestedLoops_Simple()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var sum = 0;
            for (var i = 0; i < 3; i++) {
                for (var j = 0; j < 3; j++) {
                    sum = sum + 1;
                }
            }
            return sum;
        }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(9)); // 3 * 3 = 9 iterations
    }

    [Test]
    public void ILCompile_NestedLoops_InnerBreak()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var count = 0;
            for (var i = 0; i < 3; i++) {
                for (var j = 0; j < 10; j++) {
                    if (j >= 2) break;
                    count = count + 1;
                }
            }
            return count;
        }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(6)); // 3 outer * 2 inner = 6
    }

    #endregion

    #region If-Else Compilation

    [Test]
    public void ILCompile_IfElse_TrueBranch()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 10; if (x > 5) { return \"big\"; } else { return \"small\"; } }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("big"));
    }

    [Test]
    public void ILCompile_IfElse_FalseBranch()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 3; if (x > 5) { return \"big\"; } else { return \"small\"; } }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void ILCompile_IfWithoutElse()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 0; if (true) { x = 10; } return x; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(10));
    }

    #endregion

    #region Return Statement Compilation

    [Test]
    public void ILCompile_Return_FromBlock()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ return 42; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ILCompile_Return_FromLoop()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            for (var i = 0; i < 100; i++) {
                if (i == 7) return i;
            }
            return -1;
        }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(7));
    }

    #endregion

    #region Variable Operations Compilation

    [Test]
    public void ILCompile_VariableDeclaration()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 42; return x; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ILCompile_CompoundAssignment()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 10; x += 5; return x; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ILCompile_IncrementDecrement_Prefix()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 5; var y = ++x; return y; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void ILCompile_IncrementDecrement_Postfix()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 5; var y = x++; return y; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5)); // Postfix returns original value
    }

    #endregion

    #region Performance Comparison

    [Test]
    public void ILCompile_PerformanceGain_LoopHeavy()
    {
        // This test verifies that compilation works for a loop that would
        // be significantly slower with tree-walking + exception-based break/continue
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var sum = 0;
            for (var i = 0; i < 1000; i++) {
                sum = sum + i;
            }
            return sum;
        }");

        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(499500)); // Sum of 0..999
    }

    #endregion

    #region Iteration Limit

    [Test]
    public void ILCompile_RespectsIterationLimit()
    {
        var options = new CsEvalOptions { MaxIterations = 100 };
        var engine = new CsEvalEngine(options);
        var expr = engine.Parse("{ var i = 0; while (true) { i = i + 1; } return i; }");

        Assert.That(expr.TryCompile(), Is.True);

        Assert.Throws<CsEvalException>(() => engine.Evaluate(expr));
    }

    #endregion

    #region Cancellation

    [Test]
    public void ILCompile_SupportsCancellation()
    {
        // Use very high iteration limit to ensure cancellation is tested, not iteration limit
        var options = new CsEvalOptions { MaxIterations = int.MaxValue };
        var engine = new CsEvalEngine(options);
        var expr = engine.Parse("{ var i = 0; while (i < 100000000) { i = i + 1; } return i; }");

        Assert.That(expr.TryCompile(), Is.True);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate(expr, null, cts.Token));
    }

    #endregion

    #region Switch Compilation

    [Test]
    public void ILCompile_Switch_Simple()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var x = 2;
            var result = """";
            switch (x) {
                case 1: result = ""one""; break;
                case 2: result = ""two""; break;
            }
            return result;
        }");
        
        Assert.That(expr.TryCompile(), Is.True, "Switch should be compilable");
        Assert.That(expr.IsCompiled, Is.True, "Switch should be compiled");
        Assert.That(engine.Evaluate(expr), Is.EqualTo("two"));
    }

    [Test]
    public void ILCompile_Switch_FallThrough()
    {
        var engine = new CsEvalEngine();
        // Fallthrough without break
        var expr = engine.Parse(@"{
            var x = 1;
            var sum = 0;
            switch (x) {
                case 1: sum += 10;
                case 2: sum += 20; break;
                case 3: sum += 30; break;
            }
            return sum;
        }");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(30)); // 10 + 20
    }

    [Test]
    public void ILCompile_Switch_Default()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var x = 99;
            var res = 0;
            switch (x) {
                case 1: res = 1; break;
                default: res = 100; break;
            }
            return res;
        }");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(100));
    }

    [Test]
    public void ILCompile_Switch_InsideLoop_WithBreak()
    {
        // break inside switch should break switch, not loop
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var sum = 0;
            for (var i = 0; i < 3; i++) {
                switch(i) {
                    case 0: sum += 1; break;
                    case 1: sum += 2; break;
                    case 2: sum += 3; break;
                }
            }
            return sum;
        }");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(6)); // 1 + 2 + 3
    }

    [Test]
    public void ILCompile_Switch_InsideLoop_WithContinue()
    {
        // continue inside switch should continue loop
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                switch(i) {
                    case 2: continue; // skip 2
                    default: sum += i; break;
                }
            }
            return sum;
        }");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(8)); // 0+1+3+4 = 8
    }

    [Test]
    public void ILCompile_Switch_Expressions()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse(@"{
            var x = 10;
            switch(x * 2) {
                case 20: return ""match"";
                default: return ""no"";
            }
        }");
        
        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo("match"));
    }

    #endregion
}
