namespace CsEval.Test.Compilation;

[TestFixture]
public class CompilationTests
{
    #region Basic Compilation

    [Test]
    public void Compile_SimpleLiteral_Integer()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("42");

        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(expr.IsCompiled, Is.True);
        Assert.That(expr.IsCompilable, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Compile_SimpleLiteral_String()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("\"hello\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Compile_SimpleLiteral_Boolean()
    {
        var engine = new CsEvalEngine();
        var exprTrue = engine.Parse("true");
        var exprFalse = engine.Parse("false");

        Assert.That(exprTrue.TryCompile(), Is.True);
        Assert.That(exprFalse.TryCompile(), Is.True);

        Assert.That(engine.Evaluate(exprTrue), Is.EqualTo(true));
        Assert.That(engine.Evaluate(exprFalse), Is.EqualTo(false));
    }

    [Test]
    public void Compile_SimpleLiteral_Null()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("null");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Arithmetic Expressions

    [Test]
    public void Compile_SimpleArithmetic_Addition()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("1 + 2");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Compile_SimpleArithmetic_Multiplication()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("3 * 4");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Compile_SimpleArithmetic_Complex()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("1 + 2 * 3");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Compile_SimpleArithmetic_Division()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("10 / 2");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void Compile_SimpleArithmetic_Modulo()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("7 % 3");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Compile_Negation()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("-5");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(-5));
    }

    [Test]
    public void Compile_StringConcatenation()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("\"hello\" + \" \" + \"world\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hello world"));
    }

    #endregion

    #region Variable Access

    [Test]
    public void Compile_VariableAccess()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);
        var expr = engine.Parse("x");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Compile_VariableWithArithmetic()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10)
            .SetVariable("y", 20);
        var expr = engine.Parse("x + y");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Compile_VariableChangesAfterCompilation()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);
        var expr = engine.Parse("x * 2");

        Assert.That(expr.TryCompile(), Is.True);

        Assert.That(engine.Evaluate(expr), Is.EqualTo(20));

        // Change variable value
        engine.SetVariable("x", 5);

        // Compiled expression should use new value
        Assert.That(engine.Evaluate(expr), Is.EqualTo(10));
    }

    #endregion

    #region Comparison Operators

    [Test]
    public void Compile_Comparison_Equals()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("5 == 5");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_NotEquals()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("5 != 3");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_LessThan()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("3 < 5");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_GreaterThan()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("5 > 3");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Logical Operators

    [Test]
    public void Compile_LogicalAnd()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("true && true");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalOr()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("false || true");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalNot()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("!false");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalShortCircuit_And()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 0);
        // If short-circuit works, right side should not be evaluated when left is false
        var expr = engine.Parse("false && x");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Compile_LogicalShortCircuit_Or()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 0);
        // If short-circuit works, right side should not be evaluated when left is true
        var expr = engine.Parse("true || x");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Ternary and Null Coalesce

    [Test]
    public void Compile_Ternary_TrueBranch()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);
        var expr = engine.Parse("x > 5 ? \"big\" : \"small\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("big"));
    }

    [Test]
    public void Compile_Ternary_FalseBranch()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 3);
        var expr = engine.Parse("x > 5 ? \"big\" : \"small\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void Compile_NullCoalesce_LeftNotNull()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", "value");
        var expr = engine.Parse("x ?? \"default\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void Compile_NullCoalesce_LeftNull()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", null);
        var expr = engine.Parse("x ?? \"default\"");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("default"));
    }

    #endregion

    #region Property Access

    [Test]
    public void Compile_PropertyAccess_Dictionary()
    {
        var engine = new CsEvalEngine()
            .SetVariable("person", new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 });
        var expr = engine.Parse("person.Name");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Compile_PropertyAccess_TypedObject()
    {
        var engine = new CsEvalEngine()
            .SetVariable("person", new TestPerson { Name = "John", Age = 30 });
        var expr = engine.Parse("person.Name");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Compile_PropertyAccess_NullSafe()
    {
        var engine = new CsEvalEngine()
            .SetVariable("person", null);
        var expr = engine.Parse("person?.Name");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region IL-Compiled Expressions (Control Flow)

    [Test]
    public void Compile_Blocks()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var x = 1; return x; }");

        Assert.That(expr.TryCompile(), Is.True);
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Compile_ReturnsIsCompilableFalse_ForLinq()
    {
        var engine = new CsEvalEngine()
            .SetVariable("items", new List<int> { 1, 2, 3 });
        var expr = engine.Parse("items.Where((x) => x > 1)");

        Assert.That(expr.TryCompile(), Is.False);

        // Should still work via tree-walking
        var result = engine.Evaluate(expr) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Compile_ReturnsIsCompilableFalse_ForLambda()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("(x) => x * 2");

        Assert.That(expr.TryCompile(), Is.False);
    }

    [Test]
    public void Compile_Assignment()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);
        var expr = engine.Parse("x = 20");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(20));

        // Verify assignment took effect by reading x back
        Assert.That(engine.Evaluate("x"), Is.EqualTo(20));
    }

    [Test]
    public void Compile_Loops()
    {
        var engine = new CsEvalEngine();
        var expr = engine.Parse("{ var i = 0; while (i < 3) { i = i + 1; } return i; }");

        Assert.That(expr.TryCompile(), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Automatic Compilation

    [Test]
    public void Parse_DoesNotCompileAutomatically()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");

        // Parse() should NOT compile automatically - compilation is lazy
        Assert.That(expr.IsCompiled, Is.False);

        // But Evaluate() should trigger lazy compilation and work
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(20));
        
        // After evaluation, it should be compiled (if CompileExpressions is true)
        Assert.That(expr.IsCompiled, Is.True);
    }

    [Test]
    public void Parse_NonCompilableExpressions_FallBackToTreeWalking()
    {
        var engine = new CsEvalEngine()
            .SetVariable("items", new List<int> { 1, 2, 3 });

        // LINQ with lambdas is not compilable
        var expr = engine.Parse("items.Where((x) => x > 1)");

        // Should not be compiled (not compilable)
        Assert.That(expr.IsCompiled, Is.False);

        // But should still work via tree-walking
        var result = engine.Evaluate(expr) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void CompilationMode_OnDemand_DoesNotCompileOnEvaluate()
    {
        var options = new CsEvalOptions { CompilationMode = CompilationMode.OnDemand };
        var engine = new CsEvalEngine(options)
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");
        var result = engine.Evaluate(expr);

        // Should work but NOT compile (tree-walking only)
        Assert.That(result, Is.EqualTo(20));
        Assert.That(expr.IsCompiled, Is.False);
    }

    [Test]
    public void CompilationMode_Eager_CompilesOnFirstEvaluate()
    {
        var options = new CsEvalOptions { CompilationMode = CompilationMode.Eager };
        var engine = new CsEvalEngine(options)
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");
        
        // Not compiled after Parse
        Assert.That(expr.IsCompiled, Is.False);
        
        var result = engine.Evaluate(expr);

        // Should work AND be compiled after first evaluation
        Assert.That(result, Is.EqualTo(20));
        Assert.That(expr.IsCompiled, Is.True);
    }

    [Test]
    public void CompilationMode_OnDemand_ExplicitCompileStillWorks()
    {
        var options = new CsEvalOptions { CompilationMode = CompilationMode.OnDemand };
        var engine = new CsEvalEngine(options)
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");
        
        // User explicitly compiles regardless of option
        expr.TryCompile();
        
        Assert.That(expr.IsCompiled, Is.True);
        
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(20));
    }

    #endregion

    #region ParseAndCompile

    [Test]
    public void ParseAndCompile_ReturnsCompiledExpression()
    {
        var engine = new CsEvalEngine();
        var expr = engine.ParseAndCompile("1 + 2");

        Assert.That(expr.IsCompiled, Is.True);
    }

    [Test]
    public void ParseAndCompile_BlockExpression_NowCompiled()
    {
        var engine = new CsEvalEngine();
        var expr = engine.ParseAndCompile("{ var x = 1; return x; }");

        // Blocks are now IL-compilable
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ParseAndCompile_NonCompilableExpression_StillWorks()
    {
        var engine = new CsEvalEngine()
            .SetVariable("items", new List<int> { 1, 2, 3 });
        var expr = engine.ParseAndCompile("items.Where((x) => x > 1)");

        // LINQ with lambdas is not compilable
        Assert.That(expr.IsCompiled, Is.False);

        // Should still work via tree-walking
        var result = engine.Evaluate(expr) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #endregion

    #region Thread Safety

    [Test]
    public void Compile_ThreadSafe_ParallelCompilations()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 10);
        var expr = engine.Parse("x * 2");

        // Multiple threads trying to compile simultaneously
        Parallel.For(0, 100, _ =>
        {
            expr.TryCompile();
        });

        Assert.That(expr.IsCompiled, Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(20));
    }

    [Test]
    public void Compile_ThreadSafe_ParallelEvaluations()
    {
        var engine = new CsEvalEngine()
            .SetVariable("x", 5);
        var expr = engine.Parse("x + x");

        // Expression is NOT compiled after Parse()
        Assert.That(expr.IsCompiled, Is.False);
        
        // Explicitly compile before parallel evaluations
        expr.TryCompile();
        Assert.That(expr.IsCompiled, Is.True);

        var results = new System.Collections.Concurrent.ConcurrentBag<object?>();

        Parallel.For(0, 100, _ =>
        {
            var child = engine.CreateChild();
            results.Add(child.Evaluate(expr));
        });

        // All results should be correct
        Assert.That(results.All(r => (int)r! == 10), Is.True);
    }

    #endregion

    #region Helpers

    private class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion
}
