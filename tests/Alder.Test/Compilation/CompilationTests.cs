using System.Linq.Expressions;

namespace Alder.Test.Compilation;

[TestFixture]
public class CompilationTests
{
    #region Basic Compilation

    [Test]
    public void Compile_SimpleLiteral_Integer()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("42");

        Assert.That(engine.TryCompile(expr), Is.True);
        Assert.That(expr.IsCompiled, Is.True);
        Assert.That(expr.IsCompilable, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Compile_SimpleLiteral_String()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("\"hello\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Compile_SimpleLiteral_Boolean()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var exprTrue = engine.Parse("true");
        var exprFalse = engine.Parse("false");

        Assert.That(engine.TryCompile(exprTrue), Is.True);
        Assert.That(engine.TryCompile(exprFalse), Is.True);

        Assert.That(engine.Evaluate(exprTrue), Is.EqualTo(true));
        Assert.That(engine.Evaluate(exprFalse), Is.EqualTo(false));
    }

    [Test]
    public void Compile_SimpleLiteral_Null()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("null");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Arithmetic Expressions

    [Test]
    public void Compile_SimpleArithmetic_Addition()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("1 + 2");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Compile_SimpleArithmetic_Multiplication()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("3 * 4");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void Compile_SimpleArithmetic_Complex()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("1 + 2 * 3");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void Compile_SimpleArithmetic_Division()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("10 / 2");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void Compile_SimpleArithmetic_Modulo()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("7 % 3");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Compile_Negation()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("-5");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(-5));
    }

    [Test]
    public void Compile_StringConcatenation()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("\"hello\" + \" \" + \"world\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hello world"));
    }

    #endregion

    #region Variable Access

    [Test]
    public void Compile_VariableAccess()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10);
        var expr = engine.Parse("x");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Compile_VariableWithArithmetic()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10)
            .SetVariable("y", 20);
        var expr = engine.Parse("x + y");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Compile_VariableChangesAfterCompilation()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10);
        var expr = engine.Parse("x * 2");

        Assert.That(engine.TryCompile(expr), Is.True);

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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("5 == 5");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_NotEquals()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("5 != 3");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_LessThan()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("3 < 5");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_Comparison_GreaterThan()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("5 > 3");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Logical Operators

    [Test]
    public void Compile_LogicalAnd()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("true && true");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalOr()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("false || true");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalNot()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("!false");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Compile_LogicalShortCircuit_And()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 0);
        // If short-circuit works, right side should not be evaluated when left is false
        var expr = engine.Parse("false && x");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Compile_LogicalShortCircuit_Or()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 0);
        // If short-circuit works, right side should not be evaluated when left is true
        var expr = engine.Parse("true || x");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Ternary and Null Coalesce

    [Test]
    public void Compile_Ternary_TrueBranch()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10);
        var expr = engine.Parse("x > 5 ? \"big\" : \"small\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("big"));
    }

    [Test]
    public void Compile_Ternary_FalseBranch()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 3);
        var expr = engine.Parse("x > 5 ? \"big\" : \"small\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("small"));
    }

    [Test]
    public void Compile_NullCoalesce_LeftNotNull()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", "value");
        var expr = engine.Parse("x ?? \"default\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void Compile_NullCoalesce_LeftNull()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", null);
        var expr = engine.Parse("x ?? \"default\"");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("default"));
    }

    #endregion

    #region Property Access

    [Test]
    public void Compile_PropertyAccess_Dictionary()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 });
        var expr = engine.Parse("person.Name");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Compile_PropertyAccess_TypedObject()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", new TestPerson { Name = "John", Age = 30 });
        var expr = engine.Parse("person.Name");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("John"));
    }

    [Test]
    public void Compile_PropertyAccess_NullSafe()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("person", null);
        var expr = engine.Parse("person?.Name");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region IL-Compiled Expressions (Control Flow)

    [Test]
    public void Compile_Blocks()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("{ var x = 1; return x; }");

        Assert.That(engine.TryCompile(expr), Is.True);
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Compile_ReturnsIsCompilableTrue_ForLinq()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("items", new List<int> { 1, 2, 3 });
        var expr = engine.Parse("items.Where((x) => x > 1).ToList()");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.InstanceOf<IList>());
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Compile_ReturnsIsCompilableTrue_ForLambda()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("(x) => x * 2");

        // Lambdas are now IL-compilable
        Assert.That(engine.TryCompile(expr), Is.True);
    }

    [Test]
    public void Compile_NamedArguments()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        engine.SetVariable("str", "hello");
        var expr = engine.Parse("str.Substring(startIndex: 0, length: 3)");

        Assert.That(engine.TryCompile(expr), Is.True);
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hel"));
    }

    [Test]
    public void Compile_Assignment()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10);
        var expr = engine.Parse("x = 20");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(20));

        // Verify assignment took effect by reading x back
        Assert.That(engine.Evaluate("x"), Is.EqualTo(20));
    }

    [Test]
    public void Compile_Loops()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.Parse("{ var i = 0; while (i < 3) { i = i + 1; } return i; }");

        Assert.That(engine.TryCompile(expr), Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Automatic Compilation

    [Test]
    public void Parse_DoesNotCompileAutomatically()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
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
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());

        // Object literals are not yet compilable
        var expr = engine.Parse("new { a = 1, b = 2 }");

        // Should not be compiled after Parse (not compilable)
        Assert.That(expr.IsCompiled, Is.False);

        // But should still work via tree-walking
        var result = engine.Evaluate(expr) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["a"], Is.EqualTo(1));
        Assert.That(result["b"], Is.EqualTo(2));
    }

    [Test]
    public void CompilationMode_Interpreted_DoesNotCompileOnEvaluate()
    {
        var options = AlderOptions.Default;
        var engine = new AlderEngine(options)
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");
        var result = engine.Evaluate(expr);

        // Should work but NOT compile (tree-walking only)
        Assert.That(result, Is.EqualTo(20));
        Assert.That(expr.IsCompiled, Is.False);
    }

    [Test]
    public void CompilationMode_Compiled_CompilesOnFirstEvaluate()
    {
        var options = AlderOptions.Default.UseCompiler();
        var engine = new AlderEngine(options)
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
    public void ExplicitCompile_OnCompiledEngine_Works()
    {
        var options = AlderOptions.Default.UseCompiler();
        var engine = new AlderEngine(options)
            .SetVariable("x", 10);

        var expr = engine.Parse("x * 2");

        engine.TryCompile(expr);

        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void CompilationMode_Compiled_CompilesNamedArguments()
    {
        var options = AlderOptions.Default.UseCompiler();
        var engine = new AlderEngine(options);
        engine.SetVariable("str", "hello");

        // Named arguments are now IL-compilable
        var expr = engine.Parse("str.Substring(startIndex: 0, length: 3)");

        // Compiled mode should compile and run successfully
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hel"));
    }

    [Test]
    public void CompilationMode_Compiled_DoesNotFallBackToInterpreted_WhenNotCompilable()
    {
        var options = AlderOptions.Default.UseCompiler();
        var engine = new AlderEngine(options);

        // Known non-compilable pattern: switch with fall-through from a non-empty case.
        var expr = engine.Parse(@"{
            var x = 1;
            var sum = 0;
            switch (x) {
                case 1: sum += 10;
                case 2: sum += 20; break;
            }
            return sum;
        }");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CS0163));
        Assert.That(ex.Message, Does.Contain("CS0163"));
        Assert.That(expr.IsCompiled, Is.False);
    }

    [Test]
    public void CompilationMode_Compiled_DoesNotFallBack_WhenIlEmitterDoesNotSupportNode()
    {
        var options = AlderOptions.Default.UseCompiler(new ThrowingExpressionCompiler());
        var engine = new AlderEngine(options);
        var expr = engine.Parse("1 + 2");

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CSEV0001));
        Assert.That(ex.Message, Does.Contain("Forced compile failure"));
        Assert.That(expr.IsCompiled, Is.False);
    }

    #endregion

    private sealed class ThrowingExpressionCompiler : IExpressionCompiler
    {
        public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
            where TDelegate : Delegate
            => throw new InvalidOperationException("Forced compile failure");
    }

    #region ParseAndCompile

    [Test]
    public void ParseAndCompile_ReturnsCompiledExpression()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.ParseAndCompile("1 + 2");

        Assert.That(expr.IsCompiled, Is.True);
    }

    [Test]
    public void ParseAndCompile_BlockExpression_NowCompiled()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        var expr = engine.ParseAndCompile("{ var x = 1; return x; }");

        // Blocks are now IL-compilable
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ParseAndCompile_NamedArgumentsNowCompilable()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler());
        engine.SetVariable("str", "hello");
        var expr = engine.ParseAndCompile("str.Substring(startIndex: 0, length: 3)");

        // Named arguments are now IL-compilable
        Assert.That(expr.IsCompiled, Is.True);

        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hel"));
    }

    #endregion

    #region Thread Safety

    [Test]
    public void Compile_ThreadSafe_ParallelCompilations()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 10);
        var expr = engine.Parse("x * 2");

        // Multiple threads trying to compile simultaneously
        Parallel.For(0, 100, _ =>
        {
            engine.TryCompile(expr);
        });

        Assert.That(expr.IsCompiled, Is.True);
        Assert.That(engine.Evaluate(expr), Is.EqualTo(20));
    }

    [Test]
    public void Compile_ThreadSafe_ParallelEvaluations()
    {
        var engine = new AlderEngine(AlderOptions.Default.UseCompiler())
            .SetVariable("x", 5);
        var expr = engine.Parse("x + x");

        // Expression is NOT compiled after Parse()
        Assert.That(expr.IsCompiled, Is.False);

        // Explicitly compile before parallel evaluations
        engine.TryCompile(expr);
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
