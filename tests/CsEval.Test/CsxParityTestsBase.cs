using System.Runtime.CompilerServices;

namespace CsEval.Test;

/// <summary>
/// Base class for CSX parity tests that verify CsEval produces the same results as the C# compiler.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public abstract class CsxParityTestsBase(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        MaxIterations = 1_000_000
    };

    protected async Task RunMatchesCSharp(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

            var engine = new CsEvalEngine(Options);
            var result = engine.Evaluate(expr);

            // CsEval represents anonymous objects as ExpandoObject (IDictionary<string, object?>)
            // while C# uses compiler-generated anonymous types. Compare structurally.
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

    protected async Task RunShouldThrow(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);

        var engine = new CsEvalEngine(Options);
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
    }

    protected static IEnumerable<TestCaseData> DiscoverTests(string relativePath)
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        Assert.That(new DirectoryInfo(testDataDir).Exists, Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.TopDirectoryOnly))
            yield return new TestCaseData(file).SetName(Path.GetFileNameWithoutExtension(file));
    }

    protected static IEnumerable<TestCaseData> DiscoverTestsRecursive(string relativePath)
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        Assert.That(new DirectoryInfo(testDataDir).Exists, Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            var testName = relativeName.Replace(".csx", "").Replace('/', '_');
            yield return new TestCaseData(file).SetName(testName);
        }
    }
}
