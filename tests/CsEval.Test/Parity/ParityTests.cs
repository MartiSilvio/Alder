using System.Runtime.CompilerServices;

namespace CsEval.Test.Parity;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ParityTests(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        MaxIterations = 1_000_000
    };

    [TestCaseSource(nameof(DiscoverValidTests))]
    public async Task ValidExpressions(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
            var engine = new CsEvalEngine(Options);
            var result = engine.Evaluate(expr);

            if (result is IDictionary<string, object?> dict && IsAnonymousType(csharpResult?.GetType()))
            {
                AssertAnonymousObjectEqual(dict, csharpResult!);
            }
            else
            {
                Assert.That(result, Is.EqualTo(csharpResult));
                Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
            }
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"{expr}\n{ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestCaseSource(nameof(DiscoverInvalidTests))]
    public async Task InvalidExpressions(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);

        var engine = new CsEvalEngine(Options);
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
    }

    private static bool IsAnonymousType(Type? type) =>
        type != null && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) && type.Name.Contains("AnonymousType");

    private static void AssertAnonymousObjectEqual(IDictionary<string, object?> dict, object anonymous)
    {
        var props = anonymous.GetType().GetProperties();
        Assert.That(dict.Count, Is.EqualTo(props.Length), "Property count mismatch");
        foreach (var prop in props)
        {
            Assert.That(dict.ContainsKey(prop.Name), Is.True, $"Missing property '{prop.Name}'");
            Assert.That(dict[prop.Name], Is.EqualTo(prop.GetValue(anonymous)), $"Property '{prop.Name}' value mismatch");
        }
    }

    private static IEnumerable<TestCaseData> DiscoverValidTests()
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData/ValidExpressions");
        Assert.That(new DirectoryInfo(testDataDir).Exists, Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            var testName = relativeName.Replace(".csx", "").Replace('/', '_');
            yield return new TestCaseData(file).SetName(testName);
        }
    }

    private static IEnumerable<TestCaseData> DiscoverInvalidTests()
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData/InvalidExpressions");
        Assert.That(new DirectoryInfo(testDataDir).Exists, Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            var testName = relativeName.Replace(".csx", "").Replace('/', '_');
            yield return new TestCaseData(file).SetName(testName);
        }
    }
}
