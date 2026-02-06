namespace CsEval.Test.Runtime;

/// <summary>
/// Tests for new ClassName(args) constructor invocation (ECMA-334 §12.8.16.2 - Object creation expressions).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ConstructorTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.16.2 - Object creation expressions

    // Basic constructor with member access on result
    [TestCase("new Exception(\"test\").Message", "test", TestName = "Constructor_Exception_Message")]
    [TestCase("new ArgumentException(\"msg\", \"param\").ParamName", "param", TestName = "Constructor_ArgumentException_ParamName")]
    [TestCase("new ArgumentException(\"msg\", \"param\").Message", "msg (Parameter 'param')", TestName = "Constructor_ArgumentException_Message")]
    public async Task Constructor_MemberAccess(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Parameterless constructor
    [Test]
    public void Constructor_Parameterless()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new Object()");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType(), Is.EqualTo(typeof(object)));
    }

    // Constructor then method call (chaining)
    [Test]
    public void Constructor_ThenMethodCall()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new Random(42).Next(1, 100)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<int>());
        // Random(42) is deterministic, so same seed gives same result
        var expected = new Random(42).Next(1, 100);
        Assert.That(result, Is.EqualTo(expected));
    }

    // Exception instance creation (parameterless)
    [Test]
    public void Constructor_Exception_Parameterless()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new Exception()");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Exception>());
    }

    // Fully qualified type name
    [TestCase("new System.Exception(\"test\").Message", "test", TestName = "Constructor_FullyQualified")]
    public async Task Constructor_FullyQualified(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Error case: non-existent type
    [Test]
    public void Constructor_NonExistentType_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<CsEvalException>(() => engine.Evaluate("new NonExistentType123()"));
    }

    // Built-in type constructor (e.g., new string)
    [Test]
    public void Constructor_BuiltInType_Int()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // new int() is equivalent to default(int) = 0
        // But Activator.CreateInstance(typeof(int)) returns 0
        var result = engine.Evaluate("new int()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion
}
