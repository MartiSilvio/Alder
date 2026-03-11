using CsEval.Parsing;

namespace CsEval.Test.Core;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ApiFeatureTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode });

    #region TryEvaluate

    [Test]
    public void TryEvaluate_ValidExpression_ReturnsTrueWithCorrectResult()
    {
        var engine = CreateEngine();
        var success = engine.TryEvaluate("1 + 2", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void TryEvaluate_InvalidExpression_ReturnsFalse()
    {
        var engine = CreateEngine();
        var success = engine.TryEvaluate("1 +", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryEvaluate_UndefinedVariable_ReturnsFalse()
    {
        var engine = CreateEngine();
        var success = engine.TryEvaluate("undefinedVar + 1", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryEvaluate_WithVariablesDictionary_ReturnsTrueWithCorrectResult()
    {
        var engine = CreateEngine();
        var vars = new Dictionary<string, object?> { ["x"] = 10, ["y"] = 5 };
        var success = engine.TryEvaluate("x + y", out var result, vars);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void TryEvaluate_GenericInt_ReturnsTypedResult()
    {
        var engine = CreateEngine();
        var success = engine.TryEvaluate<int>("2 + 3", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void TryEvaluate_GenericTypeMismatch_ReturnsFalse()
    {
        var engine = CreateEngine();
        var success = engine.TryEvaluate<int>("\"hello\"", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(default(int)));
    }

    #endregion

    #region TryValidate

    [Test]
    public void TryValidate_ValidExpression_ReturnsTrue()
    {
        var engine = CreateEngine();
        var success = engine.TryValidate("1 + 2", out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryValidate_SyntaxError_ReturnsFalseWithDiagnostics()
    {
        var engine = CreateEngine();
        var success = engine.TryValidate("1 +", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostics[0].Message, Is.Not.Empty);
    }

    [Test]
    public void TryValidate_UndefinedVariable_ReturnsFalse()
    {
        var engine = CreateEngine();
        var success = engine.TryValidate("undefinedVar + 1", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
    }

    [Test]
    public void TryValidate_WithRegisteredVariable_ReturnsTrue()
    {
        var engine = CreateEngine();
        engine.SetVariable("x", 42);
        var success = engine.TryValidate("x + 1", out var diagnostics);

        Assert.That(success, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryValidate_SyntaxError_DiagnosticHasPosition()
    {
        var engine = CreateEngine();
        var success = engine.TryValidate("(1 + 2", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].Line, Is.Not.Null);
        Assert.That(diagnostics[0].Column, Is.Not.Null);
    }

    [Test]
    public void TryValidate_MultipleUndefinedIdentifiers_ReportCodesAndLocations()
    {
        var engine = CreateEngine();
        var success = engine.TryValidate("foo + bar + baz", out var diagnostics);

        Assert.That(success, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(3));
        Assert.That(diagnostics.All(d => d.Code == CsEval.Diagnostics.DiagnosticCode.CS0103), Is.True);
        Assert.That(diagnostics.All(d => d.Line.HasValue && d.Column.HasValue), Is.True);
        Assert.That(diagnostics.Select(d => d.Message), Has.Some.Contains("foo"));
        Assert.That(diagnostics.Select(d => d.Message), Has.Some.Contains("bar"));
        Assert.That(diagnostics.Select(d => d.Message), Has.Some.Contains("baz"));
    }

    #endregion

    #region AST Access

    [Test]
    public void Ast_IsAccessible_ReturnsCorrectType()
    {
        var engine = CreateEngine();
        var expression = engine.Parse("1 + 2");

        Assert.That(expression.Ast, Is.Not.Null);
        Assert.That(expression.Ast, Is.InstanceOf<Expr>());
        Assert.That(expression.Ast, Is.InstanceOf<BinaryExpr>());
    }

    #endregion

    #region GetVariables

    [Test]
    public void GetVariables_ReturnsIdentifierNames()
    {
        var engine = CreateEngine();
        var expression = engine.Parse("x + y");
        var variables = expression.GetVariables();

        Assert.That(variables, Has.Count.EqualTo(2));
        Assert.That(variables, Does.Contain("x"));
        Assert.That(variables, Does.Contain("y"));
    }

    [Test]
    public void GetVariables_NoVariables_ReturnsEmpty()
    {
        var engine = CreateEngine();
        var expression = engine.Parse("1 + 2");
        var variables = expression.GetVariables();

        Assert.That(variables, Is.Empty);
    }

    [Test]
    public void GetVariables_WithDuplicates_ReturnsDistinct()
    {
        var engine = CreateEngine();
        var expression = engine.Parse("x + x + x");
        var variables = expression.GetVariables();

        Assert.That(variables, Has.Count.EqualTo(1));
        Assert.That(variables, Does.Contain("x"));
    }

    [Test]
    public void GetVariables_ComplexExpression_FindsAllVariables()
    {
        var engine = CreateEngine();
        var expression = engine.Parse("a > 0 ? b * c : d + 1");
        var variables = expression.GetVariables();

        Assert.That(variables, Has.Count.EqualTo(4));
        Assert.That(variables, Does.Contain("a"));
        Assert.That(variables, Does.Contain("b"));
        Assert.That(variables, Does.Contain("c"));
        Assert.That(variables, Does.Contain("d"));
    }

    [Test]
    public void GetVariables_ExcludesDeclaredLocals()
    {
        var engine = CreateEngine();
        // x is declared locally, y is unbound
        var expression = engine.Parse("var x = 1; x + y");
        var variables = expression.GetVariables();

        Assert.That(variables, Does.Contain("y"));
        Assert.That(variables, Does.Not.Contain("x"));
    }

    [Test]
    public void GetVariables_LambdaParametersExcluded()
    {
        var engine = CreateEngine();
        // Lambda parameter 'x' should be excluded, 'items' is unbound
        var expression = engine.Parse("items.Where((x) => x > 0)");
        var variables = expression.GetVariables();

        Assert.That(variables, Does.Contain("items"));
        Assert.That(variables, Does.Not.Contain("x"));
    }

    #endregion

    #region IDisposable

    [Test]
    public void Dispose_ThenEvaluate_ThrowsObjectDisposedException()
    {
        var engine = CreateEngine();
        engine.Dispose();

        Assert.Throws<ObjectDisposedException>(() => engine.Evaluate("1 + 2"));
    }

    [Test]
    public void Dispose_IsIdempotent()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.DoesNotThrow(() => engine.Dispose());
    }

    [Test]
    public void Dispose_UsingPattern_Works()
    {
        using var engine = CreateEngine();
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion
}
