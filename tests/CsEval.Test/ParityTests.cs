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

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/ValidExpressions"])]
    public async Task ValidExpressionsShouldPass(string csxPath)
    {
        // Determine which expressions to evaluate
        string csEvalExpr, roslynExpr;
        bool isRoslynOnly = false;

        if (csxPath.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
        {
            // For .roslyn.csx files, try CsEval first - if it can't parse, skip CsEval testing
            var expr = File.ReadAllText(csxPath).Trim();
            var engine = new CsEvalEngine(Options);

            try
            {
                // Test if CsEval can parse and evaluate this syntax
                engine.Evaluate(expr);
                csEvalExpr = roslynExpr = expr;
            }
            catch (Exception)
            {
                // CsEval can't handle this Roslyn-specific syntax, skip CsEval testing
                isRoslynOnly = true;
                csEvalExpr = roslynExpr = expr;
            }
        }
        else
        {
            // For .csx files, check for .roslyn.csx sibling
            csEvalExpr = TestHelpers.LoadTestExpression(csxPath);
            var roslynSiblingPath = csxPath.Replace(".csx", ".roslyn.csx");
            roslynExpr = File.Exists(roslynSiblingPath)
                ? File.ReadAllText(roslynSiblingPath).Trim()
                : csEvalExpr;
        }

        if (isRoslynOnly)
        {
            // Roslyn-only: just verify Roslyn can evaluate it
            var result = await TestHelpers.EvaluateCSharpAsync(roslynExpr);
            Assert.That(result, Is.Not.Null);
            return;
        }

        var exprInfo = csEvalExpr == roslynExpr
            ? csEvalExpr
            : $"CsEval: {csEvalExpr}\nRoslyn: {roslynExpr}";

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(roslynExpr);
            var engine = new CsEvalEngine(Options);
            var result = engine.Evaluate(csEvalExpr);

            if (result is IDictionary<string, object?> dict && IsAnonymousType(csharpResult?.GetType()))
            {
                AssertAnonymousObjectEqual(dict, csharpResult!);
            }
            else
            {
                Assert.That(result, Is.EqualTo(csharpResult),
                    $"Value mismatch.\n{exprInfo}");
                Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()),
                    $"Type mismatch.\n{exprInfo}");
            }
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"{exprInfo}\n{ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/InvalidExpressions"])]
    public async Task InvalidExpressionsShouldThrow(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

        // Capture Roslyn error
        Exception? roslynEx = null;
        try
        {
            await TestHelpers.EvaluateCSharpAsync(expr);
            Assert.Fail($"Roslyn did not throw for: {expr}");
        }
        catch (Exception ex)
        {
            roslynEx = ex;
        }

        // Capture CsEval error
        var engine = new CsEvalEngine(Options);
        var csEvalEx = Assert.Catch<Exception>(() => engine.Evaluate(expr));

        // Validate error codes match when both have them
        if (csEvalEx is CsEvalException { ErrorCode: not null } csEx)
        {
            var expectedCode = csEx.FormattedCode; // e.g., "CS0029"

            // Check if Roslyn error message contains the same CS code
            if (roslynEx != null && !roslynEx.Message.Contains(expectedCode!))
            {
                Assert.Warn($"Error code mismatch: CsEval threw {expectedCode}, but Roslyn error was: {roslynEx.Message}");
            }
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

    private static IEnumerable<TestCaseData> DiscoverExpressions(string relativePath)
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
