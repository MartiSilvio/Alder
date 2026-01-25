namespace CsEval.Test.Evaluator;

[TestFixture]
public class NamedParameterTests : EvaluatorTestBase
{
    [Test]
    public void Eval_NamedParameter_BasicUsage()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // String.Substring(int startIndex, int length)
        var result = Eval("str.Substring(startIndex: 0, length: 5)", context);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_OutOfOrder()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // Named parameters allow out-of-order specification
        var result = Eval("str.Substring(length: 5, startIndex: 0)", context);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_MixedWithPositional()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // First positional, then named
        var result = Eval("str.Substring(0, length: 5)", context);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_OnMathModule()
    {
        // Math.Round(double value, int digits)
        var result = Eval("Math.Round(value: 3.14159, digits: 2)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_NamedParameter_OnMathModule_OutOfOrder()
    {
        var result = Eval("Math.Round(digits: 2, value: 3.14159)");
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void Eval_NamedParameter_CaseInsensitive()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // Parameter names should match case-insensitively
        var result = Eval("str.Substring(STARTINDEX: 0, LENGTH: 5)", context);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_WithOptionalParams()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // String.PadLeft(int totalWidth, char paddingChar = ' ')
        // Only specify the required param
        var result = Eval("str.PadLeft(totalWidth: 15)", context);
        Assert.That(result, Is.EqualTo("    Hello World"));
    }

    [Test]
    public void Eval_NamedParameter_SkipOptionalWithNamed()
    {
        var engine = new CsEvalEngine();

        // Register a test function with optional parameters
        engine.RegisterModule("Test", new TestModule());

        // Test calling with named parameter to skip defaults
        var result = engine.Evaluate("Test.Greet(name: \"World\")");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Eval_NamedParameter_InvalidName_Fails()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // "invalidParam" is not a valid parameter name for Substring
        Assert.Throws<CsEval.Evaluation.EvalException>(() =>
            Eval("str.Substring(invalidParam: 0, length: 5)", context));
    }

    [Test]
    public void Eval_NamedParameter_InLambdaCall()
    {
        var context = new EvalContext();
        var items = new List<string> { "Apple", "Banana", "Cherry" };
        context.Define("items", items);

        // LINQ methods with named parameters in lambda
        var result = Eval("items.Where(x => x.StartsWith(value: \"B\"))", context);
        Assert.That(result, Is.InstanceOf<List<object?>>());
        var list = (List<object?>)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("Banana"));
    }

    [Test]
    public void Eval_NamedParameter_WithExpressionValue()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");
        context.Define("start", 0);
        context.Define("len", 5);

        // Named parameters can have expression values
        var result = Eval("str.Substring(startIndex: start, length: len)", context);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void Eval_NamedParameter_AllPositional_StillWorks()
    {
        var context = new EvalContext();
        context.Define("str", "Hello World");

        // Ensure regular positional still works after the change
        var result = Eval("str.Substring(0, 5)", context);
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
