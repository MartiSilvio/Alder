using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[NonParallelizable]
public class ConsoleOutputTests(CompilationMode mode)
{
    [Test]
    public void Console_WriteLine_CapturesOutput()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Types.AddAssembly(typeof(Console).Assembly);
            o.Types.AddNamespace("System");
            o.Sandbox = SandboxOptions.Trusted() with
            {
                TrustedTypes = [typeof(Console)]
            };
        });

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(TestContext.Out);

            engine.Evaluate("""Console.WriteLine("hello from Alder") """);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void Console_WriteLine_WithInterpolation()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Types.AddAssembly(typeof(Console).Assembly);
            o.Types.AddNamespace("System");
            o.Sandbox = SandboxOptions.Trusted() with
            {
                TrustedTypes = [typeof(Console)]
            };
        });

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
