namespace CsEval.Test.Operators;

// Engine-only: All remaining tests verify exception throwing behavior (Assert.Catch),
// use SetVariable, or test CsEval-specific runtime model (int ?? throw).
// Non-throwing paths migrated to TestData/ThrowExpression/*.csx

/// <summary>
/// Tests for throw expressions (ECMA-334 §12.16 - Throw expression operator).
/// Throw expressions allow exception throwing in expression contexts like null-coalescing (??)
/// and conditional (?:) operators.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ThrowExpressionTests(CompilationMode mode)
{
    #region ECMA-334 §12.16 - Null-coalescing with throw

    // Engine-only: CsEval runtime model treats int as object?, Roslyn rejects `42 ?? throw` (int not nullable)
    [Test]
    public void NullCoalesce_NonNullInt_ReturnsValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("42 ?? throw new Exception(\"fail\")");
        Assert.That(result, Is.EqualTo(42));
    }

    // Engine-only: exception verification, SetVariable
    [Test]
    public void NullCoalesce_Null_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var variables = new Dictionary<string, object?> { { "x", null } };
        foreach (var (name, value) in variables)
            engine.SetVariable(name, value);

        var ex = Assert.Catch<Exception>(() => engine.Evaluate("x ?? throw new Exception(\"value was null\")"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("value was null"));
    }

    // Engine-only: exception verification, SetVariable
    [Test]
    public void NullCoalesce_Null_ThrowsArgumentException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var variables = new Dictionary<string, object?> { { "x", null } };
        foreach (var (name, value) in variables)
            engine.SetVariable(name, value);

        var ex = Assert.Catch<ArgumentException>(() => engine.Evaluate("x ?? throw new ArgumentException(\"bad arg\", \"param\")"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ParamName, Is.EqualTo("param"));
    }

    #endregion

    #region ECMA-334 §12.16 - Conditional with throw

    // Engine-only: exception verification
    [Test]
    public void Conditional_FalseCondition_ElseThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Catch<InvalidOperationException>(() =>
            engine.Evaluate("false ? 42 : throw new InvalidOperationException(\"not allowed\")"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("not allowed"));
    }

    // Engine-only: exception verification
    [Test]
    public void Conditional_TrueCondition_ThenThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Catch<InvalidOperationException>(() =>
            engine.Evaluate("true ? throw new InvalidOperationException(\"not allowed\") : 42"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("not allowed"));
    }

    #endregion

    #region ECMA-334 §12.16 - Standalone throw expression

    // Engine-only: exception verification
    [Test]
    public void Standalone_ThrowExpression_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Catch<Exception>(() =>
            engine.Evaluate("throw new Exception(\"standalone throw\")"));
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("standalone throw"));
    }

    #endregion

    #region Error cases

    // Engine-only: error test
    [Test]
    public void Throw_NonExceptionType_ProducesError()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // new Object() is not an Exception, should error
        Assert.Catch(() => engine.Evaluate("throw new Object()"));
    }

    #endregion
}
