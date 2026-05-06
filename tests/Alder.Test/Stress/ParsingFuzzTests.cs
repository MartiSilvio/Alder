using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class ParsingFuzzTests(CompilationMode mode) : StressTestBase(mode)
{
    public static IEnumerable<string> FuzzCases()
    {
        var random = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            yield return GenerateFuzz(random);
        }
    }

    private static string GenerateFuzz(Random random)
    {
        var length = random.Next(10, 100);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            if (random.NextDouble() > 0.5)
            {
                var ops = "+-*/=<>!&|";
                chars[i] = ops[random.Next(ops.Length)];
            }
            else if (random.NextDouble() > 0.5)
            {
                var parts = "(){}[],.;'\"";
                chars[i] = parts[random.Next(parts.Length)];
            }
            else
            {
                chars[i] = (char)random.Next(32, 126);
            }
        }
        return new string(chars);
    }

    [TestCaseSource(nameof(FuzzCases))]
    public void FuzzParsing_ShouldNotCrash(string fuzz)
    {
        try
        {
            Engine.Parse(fuzz);
        }
        catch (AlderException) { }
        catch (Exception ex)
        {
            Assert.Fail($"Parser threw {ex.GetType().Name} for fuzz input: {fuzz}");
        }
    }
}
