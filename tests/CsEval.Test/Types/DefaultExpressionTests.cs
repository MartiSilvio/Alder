namespace CsEval.Test.Types;

/// <summary>
/// Tests for default(T) and default literal expressions (ECMA-334 §12.8.20).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DefaultExpressionTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.20 - Default Expression

    [Test]
    public void Default_ReferenceTypes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("default(string)"), Is.Null);
        Assert.That(engine.Evaluate("default(object)"), Is.Null);
    }

    [Test]
    public void Default_NullableTypes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("default(int?)"), Is.Null);
        Assert.That(engine.Evaluate("default(bool?)"), Is.Null);
    }

    #endregion
}
