using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// Tests for new ClassName(args) constructor invocation (ECMA-334 §12.8.16.2 - Object creation expressions).
/// All tests engine-only: reference type identity not value-comparable (new Object(), new Exception()),
/// deterministic Random seed comparison, error assertion (AlderException).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConstructorTests(CompilationMode mode)
{

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



    // Engine-only: AlderException assertion for non-existent type
    [Test]
    public void Constructor_NonExistentType_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Catch<AlderException>(() => engine.Evaluate("new NonExistentType123()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0246));
    }

    // Engine-only: new int() returns default(int) = 0, verifies Activator.CreateInstance
    [Test]
    public void Constructor_BuiltInType_Int()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("new int()");
        Assert.That(result, Is.EqualTo(0));
    }

}
