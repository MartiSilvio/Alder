using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// Named parameter tests for method invocations.
/// Engine-only tests: RegisterModule, SetVariable with non-serializable types,
/// error assertions, LINQ extension method calls.
/// Parity tests migrated to TestData/Runtime/NamedParameter/*.csx
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class NamedParameterTests(CompilationMode mode)
{
    #region Engine-only: RegisterModule (Alder-specific API)

    // Engine-only: RegisterModule with custom TestModule (Alder-specific API)
    [Test]
    public void Eval_NamedParameter_SkipOptionalWithNamed()
    {
        var engine = new AlderEngine(o =>
        {
            if (mode == CompilationMode.Compiled) o.UseCompiler();
            o.Modules.Register<TestModule>("Test", instance: new TestModule());
        });

        var result = engine.Evaluate("""Test.Greet(name: "World") """);
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    #endregion

    #region Engine-only: Alder named parameter behavior (case-sensitive parameter names)

    // Named argument binding follows C# semantics: parameter names are case-sensitive.
    [Test]
    public void Eval_NamedParameter_CaseMismatch_Fails()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("str", "Hello World");

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("str.Substring(STARTINDEX: 0, LENGTH: 5)"));
        // Overload resolution classifies the named-argument failure as CS1739 ("no parameter
        // named '{name}' on '{method}'") instead of the generic ALDR0304 method-invocation error.
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1739));
    }

    #endregion

    #region Engine-only: error tests (AlderException assertions)

    // Engine-only: AlderException assertion for invalid parameter name
    [Test]
    public void Eval_NamedParameter_InvalidName_Fails()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("str", "Hello World");

        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("str.Substring(invalidParam: 0, length: 5)"));
        // Overload resolution classifies the named-argument failure as CS1739 ("no parameter
        // named '{name}' on '{method}'") instead of the generic ALDR0304 method-invocation error.
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1739));
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable types

    // Engine-only: SetVariable with List<string> + LINQ extension method call
    [Test]
    public void Eval_NamedParameter_InLambdaCall()
    {
        var engine = TestEngineFactory.Create(mode);
        var items = new List<string> { "Apple", "Banana", "Cherry" };
        engine.SetVariable("items", items);

        var result = engine.Evaluate("""items.Where(x => x.StartsWith(value: "B")).ToList() """);
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("Banana"));
    }

    #endregion

    public class TestModule
    {
        public string Greet(string name, string greeting = "Hello")
        {
            return $"{greeting}, {name}!";
        }

        public int Add(int a, int b, int c = 0)
        {
            return a + b + c;
        }
    }
}
