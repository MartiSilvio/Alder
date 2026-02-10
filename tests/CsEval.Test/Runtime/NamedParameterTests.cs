namespace CsEval.Test.Runtime;

/// <summary>
/// Named parameter tests for method invocations.
/// Engine-only tests: RegisterModule, SetVariable with non-serializable types,
/// error assertions, LINQ extension method calls.
/// Parity tests migrated to TestData/Runtime/NamedParameter/*.csx
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NamedParameterTests(CompilationMode mode)
{
    #region Engine-only: RegisterModule (CsEval-specific API)

    // Engine-only: RegisterModule with custom TestModule (CsEval-specific API)
    [Test]
    public void Eval_NamedParameter_SkipOptionalWithNamed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        engine.RegisterModule("Test", new TestModule());

        var result = engine.Evaluate("Test.Greet(name: \"World\")");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    #endregion

    #region Engine-only: error tests (CsEvalException assertions)

    // Engine-only: CsEvalException assertion for invalid parameter name
    [Test]
    public void Eval_NamedParameter_InvalidName_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("str.Substring(invalidParam: 0, length: 5)"));
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable types

    // Engine-only: SetVariable with List<string> + LINQ extension method call
    [Test]
    public void Eval_NamedParameter_InLambdaCall()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var items = new List<string> { "Apple", "Banana", "Cherry" };
        engine.SetVariable("items", items);

        var result = engine.Evaluate("items.Where(x => x.StartsWith(value: \"B\")).ToList()");
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
