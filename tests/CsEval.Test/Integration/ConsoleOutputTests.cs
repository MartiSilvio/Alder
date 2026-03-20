using CsEval.Test._Infrastructure;

namespace CsEval.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[NonParallelizable]
public class ConsoleOutputTests(CompilationMode mode)
{
    [Test]
    public void Console_WriteLine_CapturesOutput()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterAssembly(typeof(Console).Assembly);
        engine.RegisterNamespace("System");

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(TestContext.Out);

            engine.Evaluate("""Console.WriteLine("hello from CsEval") """);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void Console_WriteLine_WithInterpolation()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.RegisterAssembly(typeof(Console).Assembly);
        engine.RegisterNamespace("System");

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(TestContext.Out);

            engine.Evaluate("""Console.WriteLine($"2 + 2 = {2 + 2}") """);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
