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

    [TestCase("default(int)", 0, TestName = "Default_Int")]
    [TestCase("default(long)", 0L, TestName = "Default_Long")]
    [TestCase("default(bool)", false, TestName = "Default_Bool")]
    [TestCase("default(double)", 0.0, TestName = "Default_Double")]
    [TestCase("default(float)", 0.0f, TestName = "Default_Float")]
    [TestCase("default(char)", '\0', TestName = "Default_Char")]
    public async Task Default_ValueTypes(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

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
