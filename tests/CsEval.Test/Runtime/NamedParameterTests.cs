namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NamedParameterTests(CompilationMode mode) 
{
    [Test]
    public void Eval_NamedParameter_BasicUsage()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // String.Substring(int startIndex, int length)
        var result = engine.Evaluate("str.Substring(startIndex: 0, length: 5)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_OutOfOrder()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // Named parameters allow out-of-order specification
        var result = engine.Evaluate("str.Substring(length: 5, startIndex: 0)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_MixedWithPositional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // First positional, then named
        var result = engine.Evaluate("str.Substring(0, length: 5)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_OnMathModule()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // Math.Round(double value, int digits)
        var result = engine.Evaluate("Math.Round(value: 3.14159, digits: 2)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_NamedParameter_OnMathModule_OutOfOrder()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("Math.Round(digits: 2, value: 3.14159)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_NamedParameter_CaseInsensitive()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // Parameter names should match case-insensitively
        var result = engine.Evaluate("str.Substring(STARTINDEX: 0, LENGTH: 5)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_WithOptionalParams()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // String.PadLeft(int totalWidth, char paddingChar = ' ')
        // Only specify the required param
        var result = engine.Evaluate("str.PadLeft(totalWidth: 15)");
        Assert.That(result, Is.EqualTo("    Hello World"));
    }

    [Test]
    public void Eval_NamedParameter_SkipOptionalWithNamed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        // Register a test function with optional parameters
        engine.RegisterModule("Test", new TestModule());

        // Test calling with named parameter to skip defaults
        var result = engine.Evaluate("Test.Greet(name: \"World\")");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Eval_NamedParameter_InvalidName_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // "invalidParam" is not a valid parameter name for Substring
        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("str.Substring(invalidParam: 0, length: 5)"));
    }

    [Test]
    public void Eval_NamedParameter_InLambdaCall()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var items = new List<string> { "Apple", "Banana", "Cherry" };
        engine.SetVariable("items", items);

        // LINQ methods with named parameters in lambda
        var result = engine.Evaluate("items.Where(x => x.StartsWith(value: \"B\")).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("Banana"));
    }

    [Test]
    public void Eval_NamedParameter_WithExpressionValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");
        engine.SetVariable("start", 0);
        engine.SetVariable("len", 5);

        // Named parameters can have expression values
        var result = engine.Evaluate("str.Substring(startIndex: start, length: len)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_AllPositional_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("str", "Hello World");

        // Ensure regular positional still works after the change
        var result = engine.Evaluate("str.Substring(0, 5)");
        Assert.That(result, Is.EqualTo("Hello"));
    }

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
