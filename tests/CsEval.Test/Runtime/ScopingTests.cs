namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ScopingTests(CompilationMode mode)
{
    #region ForEach Loop Scoping

    // Engine-only: error tests and CsEval-specific [1,2,3] syntax
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
                results = [..results, x];
            }
            return results;
        }");

        Assert.That(result, Is.TypeOf<int[]>());
        var list = (System.Collections.IList)result!;
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(10));
        Assert.That(list[1], Is.EqualTo(20));
        Assert.That(list[2], Is.EqualTo(30));
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

    // Engine-only: error tests for variable leaking
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

    // Engine-only: uses [..results, x] spread syntax
    [Test]
    public void ForLoop_VariableScopedPerIteration_FreshEachTime()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var results = [];
            for (var i = 0; i < 3; i = i + 1) {
                var x = i * 10;
                results = [..results, x];
            }
            return results;
        }");

        Assert.That(result, Is.TypeOf<int[]>());
        var list = (System.Collections.IList)result!;
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(0));
        Assert.That(list[1], Is.EqualTo(10));
        Assert.That(list[2], Is.EqualTo(20));
    }

    #endregion

    #region While Loop Scoping

    // Engine-only: error tests for variable leaking
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
    public void WhileLoop_SingleStatementBodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

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

    // Engine-only: uses [..results, x] spread syntax
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
                results = [..results, x];
                i = i + 1;
            }
            return results;
        }");

        Assert.That(result, Is.TypeOf<int[]>());
        var list = (System.Collections.IList)result!;
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(0));
        Assert.That(list[1], Is.EqualTo(10));
        Assert.That(list[2], Is.EqualTo(20));
    }

    #endregion

    #region Do-While Loop Scoping

    // Engine-only: error tests for variable leaking
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

    // Engine-only: uses [..results, x] spread syntax
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
                results = [..results, x];
                i = i + 1;
            } while (i < 3);
            return results;
        }");

        Assert.That(result, Is.TypeOf<int[]>());
        var list = (System.Collections.IList)result!;
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(0));
        Assert.That(list[1], Is.EqualTo(10));
        Assert.That(list[2], Is.EqualTo(20));
    }

    #endregion

    #region If Statement Scoping

    // Engine-only: error tests for variable leaking
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

    // Engine-only: error tests for variable leaking
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

    // Engine-only: uses [1, 2, 3, 4, 5] collection expression syntax
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

    // Engine-only: uses [10, 20] collection expression syntax
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

        Assert.That(result, Is.EqualTo(210));
    }

    #endregion

    #region Block Statement Scoping (via If)

    // Engine-only: error test for variable leaking
    [Test]
    public void BlockInIf_VariablesScoped_AsExpected()
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

    #endregion

    #region Break and Continue with Scoping

    // Engine-only: uses [1, 2, 3, 4, 5] collection expression syntax
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

        Assert.That(result, Is.EqualTo(9));
    }

    #endregion
}
