// Engine-only: All tests verify variable scoping via error assertions (CsEvalException for leaked variables)
// or use CsEval-specific [1,2,3] collection expression and [..spread] syntax

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ScopingTests(CompilationMode mode)
{
    [Test]
    public void LetInExpression_EvaluatesWithLocalScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });

        var result = engine.Evaluate("let x = 5 in x * x");

        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void LetInDestructuring_EvaluatesWithScopedBindings()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("person", new Dictionary<string, object?>
        {
            ["Name"] = "Ada",
            ["Age"] = 20
        });

        var result = engine.Evaluate("let { Name, Age } = person in Name + \":\" + Age");

        Assert.That(result, Is.EqualTo("Ada:20"));
    }

    [Test]
    public void LetInDestructuring_TempVarDoesNotShadowUserVariable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        // The destructuring temp variable uses a <angle-bracket> name that
        // is illegal in user C# code, so it cannot collide with user variables.
        engine.SetVariable("person", new Dictionary<string, object?>
        {
            ["Name"] = "Ada",
            ["Age"] = 20
        });
        // Set a variable with the old-style temp name to prove it doesn't interfere
        engine.SetVariable("__let_tmp_1_1_0", "SHADOW");

        var result = engine.Evaluate("let { Name, Age } = person in Name + \":\" + Age");

        Assert.That(result, Is.EqualTo("Ada:20"));
    }

    #region ForEach Loop Scoping

    // Engine-only: error tests and CsEval-specific [1,2,3] syntax
    [Test]
    public void ForEachLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
    public void ForEachLoop_NestedLoops_InnerVariableDoesNotLeakToOuter()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                for (var i = 0; i < 3; i = i + 1)
                    var x = i * 2;
                return x;
            }"));

        Assert.That(ex!.Message, Does.Contain("x").Or.Contain("Undefined"));
    }

    #endregion

    #region While Loop Scoping

    // Engine-only: error tests for variable leaking
    [Test]
    public void WhileLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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

    #endregion

    #region Do-While Loop Scoping

    // Engine-only: error tests for variable leaking
    [Test]
    public void DoWhileLoop_BodyVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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

    #endregion

    #region If Statement Scoping

    // Engine-only: error tests for variable leaking
    [Test]
    public void IfStatement_ThenBranchVariable_DoesNotLeakToParentScope()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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

    #region Block Statement Scoping (via If)

    // Engine-only: error test for variable leaking
    [Test]
    public void BlockInIf_VariablesScoped_AsExpected()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

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
}
