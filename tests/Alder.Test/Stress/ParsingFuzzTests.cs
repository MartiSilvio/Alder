using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
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
            // Mix of valid syntax and garbage
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
            // We only care about crashes (unhandled) or hangs.
            // If it throws "IndexOutOfRangeException" or "NullReferenceException" inside parser, that's a bug we want to expose.
            // But we can't easily distinguish "valid" managed exceptions from "bugs" in a generic catch.
            // So we'll print unexpected ones.
            TestContext.WriteLine($"Fuzz '{fuzz}' caused {ex.GetType().Name}: {ex.Message}");

            if (ex is NullReferenceException or IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                Assert.Fail($"Parser crashed with internal logic error: {ex.GetType().Name}");
            }
        }
    }
}
