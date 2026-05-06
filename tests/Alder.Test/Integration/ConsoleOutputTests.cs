using Alder.Test._Infrastructure;

namespace Alder.Test.Integration;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
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
            o.Security = SecurityOptions.Trusted() with
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
            o.Security = SecurityOptions.Trusted() with
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
