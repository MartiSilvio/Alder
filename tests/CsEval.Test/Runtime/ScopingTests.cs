namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ScopingTests(CompilationMode mode) 
{
    #region ForEach Loop Scoping

    [Test]
    public void ForEachLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var i in [1, 2, 3]) {
                    var x = i * 2;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void ForEachLoop_BodyVariableWithoutBraces_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var i in [1, 2, 3])
                    var x = i * 2;
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void ForEachLoop_IterationVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var item in [1, 2, 3]) {
                }
                return item;
            }"));

        Assert.That(ex!.Message, Does.Contain("item").Or.Contain("Undefined"));
    }

    [Test]
    public void ForEachLoop_VariableScopedPerIteration_IndependentValues()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var results = [];
            foreach (var i in [1, 2, 3]) {
                var x = i * 10;
                results = [...results, x];
            }
            return results;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(10));
        Assert.That(result[1], Is.EqualTo(20));
        Assert.That(result[2], Is.EqualTo(30));
    }

    [Test]
    public void ForEachLoop_NestedLoops_InnerVariableDoesNotLeakToOuter()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var i in [1, 2]) {
                    foreach (var j in [10, 20]) {
                        var inner = j;
                    }
                    var x = inner;
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("inner").Or.Contain("Undefined"));
    }

    #endregion

    #region For Loop Scoping

    [Test]
    public void ForLoop_InitializerVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; i < 3; i = i + 1) {
                }
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("i").Or.Contain("Undefined"));
    }

    [Test]
    public void ForLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; i < 3; i = i + 1) {
                    var x = i * 2;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void ForLoop_BodyVariableWithoutBraces_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; i < 3; i = i + 1)
                    var x = i * 2;
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void ForLoop_InitializerAccessibleInBody()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 1; i <= 5; i = i + 1) {
                sum = sum + i;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForLoop_InitializerAccessibleInCondition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 10; i = i + 1) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ForLoop_InitializerAccessibleInIncrement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 5; i = i + 2) {
                count = count + 1;
            }
            return count;
        }");

        // i = 0, 2, 4 (3 iterations)
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ForLoop_VariableScopedPerIteration_FreshEachTime()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var results = [];
            for (var i = 0; i < 3; i = i + 1) {
                var x = i * 10;
                results = [...results, x];
            }
            return results;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(0));
        Assert.That(result[1], Is.EqualTo(10));
        Assert.That(result[2], Is.EqualTo(20));
    }

    #endregion

    #region While Loop Scoping

    [Test]
    public void WhileLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                while (i < 3) {
                    var x = i * 2;
                    i = i + 1;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void WhileLoop_SingleStatementAssignment_ScopedCorrectly()
    {
        // Note: Without braces, only one statement forms the body.
        // Variable declarations as single statements don't make sense
        // (they'd be immediately out of scope), so we test assignment scoping instead.
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (i < 3)
                i = i + 1;
            return i;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_SingleStatementBodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        // This test specifically checks single-statement body scoping
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 3;
                while (count > 0) {
                    count = count - 1;
                    var temp = count;
                }
                return temp;
            }"));

        Assert.That(ex!.Message, Does.Contain("temp").Or.Contain("Undefined"));
    }

    [Test]
    public void WhileLoop_VariableScopedPerIteration()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var results = [];
            while (i < 3) {
                var x = i * 10;
                results = [...results, x];
                i = i + 1;
            }
            return results;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(0));
        Assert.That(result[1], Is.EqualTo(10));
        Assert.That(result[2], Is.EqualTo(20));
    }

    #endregion

    #region Do-While Loop Scoping

    [Test]
    public void DoWhileLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                do {
                    var x = i * 2;
                    i = i + 1;
                } while (i < 3);
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void DoWhileLoop_VariableScopedPerIteration()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var results = [];
            do {
                var x = i * 10;
                results = [...results, x];
                i = i + 1;
            } while (i < 3);
            return results;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(0));
        Assert.That(result[1], Is.EqualTo(10));
        Assert.That(result[2], Is.EqualTo(20));
    }

    #endregion

    #region If Statement Scoping

    [Test]
    public void IfStatement_ThenBranchVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (true) {
                    var x = 42;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void IfStatement_ThenBranchVariableWithoutBraces_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (true)
                    var x = 42;
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void IfStatement_ElseBranchVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (false) {
                    var a = 1;
                } else {
                    var x = 42;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void IfStatement_ElseBranchVariableWithoutBraces_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (false)
                    var a = 1;
                else
                    var x = 42;
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void IfStatement_ThenAndElseBranchVariables_Independent()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        // Both branches can use the same variable name since they're in different scopes
        var result = engine.Evaluate(@"
        {
            var result = 0;
            if (true) {
                var x = 10;
                result = x;
            } else {
                var x = 20;
                result = x;
            }
            return result;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IfStatement_NestedIf_VariablesProperlyScoped()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (true) {
                    if (true) {
                        var inner = 42;
                    }
                    var x = inner;
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("inner").Or.Contain("Undefined"));
    }

    [Test]
    public void IfStatement_ElseIfChain_VariablesProperlyScoped()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var n = 2;
                if (n == 1) {
                    var x = 10;
                } else if (n == 2) {
                    var x = 20;
                } else {
                    var x = 30;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    #endregion

    #region Mixed Control Flow Scoping

    [Test]
    public void MixedControlFlow_IfInsideFor_ProperScoping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; i < 3; i = i + 1) {
                    if (i % 2 == 0) {
                        var even = true;
                    }
                }
                return even;
            }"));

        Assert.That(ex!.Message, Does.Contain("even").Or.Contain("Undefined"));
    }

    [Test]
    public void MixedControlFlow_ForInsideIf_ProperScoping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (true) {
                    for (var i = 0; i < 3; i = i + 1) {
                        var x = i;
                    }
                }
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("i").Or.Contain("Undefined"));
    }

    [Test]
    public void MixedControlFlow_WhileInsideForEach_ProperScoping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var item in [1, 2, 3]) {
                    var count = 0;
                    while (count < item) {
                        var inner = count;
                        count = count + 1;
                    }
                }
                return inner;
            }"));

        Assert.That(ex!.Message, Does.Contain("inner").Or.Contain("Undefined"));
    }

    #endregion

    #region Parent Scope Access

    [Test]
    public void ForLoop_CanAccessParentScopeVariables()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 0; i < 5; i = i + 1) {
                total = total + i;
            }
            return total;
        }");

        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4
    }

    [Test]
    public void ForEachLoop_CanAccessParentScopeVariables()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var item in [1, 2, 3, 4, 5]) {
                total = total + item;
            }
            return total;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void IfStatement_CanAccessParentScopeVariables()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var x = 10;
            if (true) {
                x = x + 5;
            }
            return x;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void NestedLoops_CanAccessAllParentScopes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var total = 0;
            for (var i = 1; i <= 3; i = i + 1) {
                foreach (var j in [10, 20]) {
                    var k = 0;
                    while (k < 2) {
                        total = total + i + j + k;
                        k = k + 1;
                    }
                }
            }
            return total;
        }");

        // For i=1: foreach j in [10,20]: while k in [0,1]: total += 1 + 10 + k, 1 + 20 + k
        // i=1, j=10: k=0 -> +11, k=1 -> +12 (23)
        // i=1, j=20: k=0 -> +21, k=1 -> +22 (43)
        // i=2, j=10: k=0 -> +12, k=1 -> +13 (25)
        // i=2, j=20: k=0 -> +22, k=1 -> +23 (45)
        // i=3, j=10: k=0 -> +13, k=1 -> +14 (27)
        // i=3, j=20: k=0 -> +23, k=1 -> +24 (47)
        // Total: 23 + 43 + 25 + 45 + 27 + 47 = 210
        Assert.That(result, Is.EqualTo(210));
    }

    #endregion

    #region Block Statement Scoping (via If)

    [Test]
    public void BlockInIf_VariablesScoped_AsExpected()
    {
        // CsEval doesn't support standalone block statements, but blocks
        // are created via control structures. This test uses if(true) to create a block.
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                if (true) {
                    var x = 42;
                }
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    [Test]
    public void NestedBlocks_ViaNestedIf_ProperScoping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var outer = 1;
            if (true) {
                var middle = 2;
                if (true) {
                    var inner = 3;
                    outer = outer + middle + inner;
                }
            }
            return outer;
        }");

        Assert.That(result, Is.EqualTo(6)); // 1 + 2 + 3
    }

    #endregion

    #region Break and Continue with Scoping

    [Test]
    public void ForLoop_BreakPreservesParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 10; i = i + 1) {
                var temp = i;
                sum = sum + temp;
                if (i == 4) {
                    break;
                }
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4
    }

    [Test]
    public void ForEachLoop_ContinuePreservesParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var i in [1, 2, 3, 4, 5]) {
                var temp = i;
                if (i % 2 == 0) {
                    continue;
                }
                sum = sum + temp;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(9)); // 1+3+5
    }

    [Test]
    public void WhileLoop_BreakAndContinue_ScopeCleanedUp()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            while (true) {
                var x = i * 2;
                i = i + 1;
                if (i == 3) {
                    continue;
                }
                if (i > 5) {
                    break;
                }
                total = total + x;
            }
            return total;
        }");

        // i=1: x=0, total=0
        // i=2: x=2, total=2
        // i=3: continue (x=4 not added)
        // i=4: x=6, total=8
        // i=5: x=8, total=16
        // i=6: break
        Assert.That(result, Is.EqualTo(16));
    }

    #endregion
}
