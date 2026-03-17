namespace CsEval.Test.Runtime;

/// <summary>
/// Tests for new ClassName(args) constructor invocation (ECMA-334 §12.8.16.2 - Object creation expressions).
/// All tests engine-only: reference type identity not value-comparable (new Object(), new Exception()),
/// deterministic Random seed comparison, error assertion (CsEvalException).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConstructorTests(CompilationMode mode)
{
    #region Engine-only: reference type identity (not value-comparable)

    // Engine-only: new Object() creates reference type, not value-comparable with Is.EqualTo
    [Test]
    public void Constructor_Parameterless()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("new Object()");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.GetType(), Is.EqualTo(typeof(object)));
    }

    // Engine-only: Random instance identity, deterministic seed comparison
    [Test]
    public void Constructor_ThenMethodCall()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("new Random(42).Next(1, 100)");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<int>());
        var expected = new Random(42).Next(1, 100);
        Assert.That(result, Is.EqualTo(expected));
    }

    // Engine-only: new Exception() creates reference type, not value-comparable
    [Test]
    public void Constructor_Exception_Parameterless()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("new Exception()");
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Exception>());
    }

    #endregion

    #region Engine-only: error tests (CsEvalException assertions)

    // Engine-only: CsEvalException assertion for non-existent type
    [Test]
    public void Constructor_NonExistentType_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        Assert.Catch<CsEvalException>(() => engine.Evaluate("new NonExistentType123()"));
    }

    // Engine-only: new int() returns default(int) = 0, verifies Activator.CreateInstance
    [Test]
    public void Constructor_BuiltInType_Int()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("new int()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion
}
