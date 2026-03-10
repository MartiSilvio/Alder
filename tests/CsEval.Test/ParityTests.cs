using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CsEval.Test;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ParityTests(CompilationMode mode)
{
    private static readonly Regex RoslynCodeRegex = new(@"\bCS\d{4}\b", RegexOptions.Compiled);

    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        Constraints = new ExecutionConstraints { MaxStatements = 1_000_000 },
        LanguageMode = LanguageMode.Extended
    };

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/ValidExpressions"])]
    public async Task ValidExpressionsShouldPass(string csxPath)
    {
        // Determine which expressions to evaluate
        string csEvalExpr, roslynExpr;

        if (csxPath.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
        {
            // For .roslyn.csx files, both CsEval and Roslyn evaluate the same expression
            // CsEval should handle standard C# syntax - if it fails, that's a bug to fix
            csEvalExpr = roslynExpr = (await File.ReadAllTextAsync(csxPath)).Trim();
        }
        else
        {
            // For .csx files, check for .roslyn.csx sibling
            csEvalExpr = TestHelpers.LoadTestExpression(csxPath);

            var roslynSiblingPath = csxPath.Replace(".csx", ".roslyn.csx");
            if (File.Exists(roslynSiblingPath))
            {
                roslynExpr = (await File.ReadAllTextAsync(roslynSiblingPath)).Trim();
            }
            else
            {
                roslynExpr = csEvalExpr;
            }
        }
        
        var exprInfo = csEvalExpr == roslynExpr
            ? csEvalExpr
            : $"CsEval: {csEvalExpr}\nRoslyn: {roslynExpr}";

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(roslynExpr);
            var engine = new CsEvalEngine(Options);
            var expression = engine.Parse(csEvalExpr);
            var result = engine.Evaluate(expression);
            AssertNoFallbackInCompiledMode(expression, csEvalExpr);

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
            var csEvalCode = csEx.FormattedCode; // e.g., "CS0029"
            var roslynCode = ExtractRoslynErrorCode(roslynEx?.Message);

            // If Roslyn threw a runtime exception (no compiler code), only require both sides to throw.
            if (roslynCode == null)
                return;

            // Check exact code parity for compiler diagnostics.
            if (!string.Equals(csEvalCode, roslynCode, StringComparison.Ordinal))
            {
                Assert.Fail($"Error code mismatch: CsEval threw {csEvalCode}, Roslyn threw {roslynCode}. Roslyn error was: {roslynEx?.Message}");
            }
        }
    }

    private static string? ExtractRoslynErrorCode(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var match = RoslynCodeRegex.Match(message);
        return match.Success ? match.Value : null;
    }

    private static bool IsAnonymousType(Type? type) =>
        type != null && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) && type.Name.Contains("AnonymousType");

    private void AssertNoFallbackInCompiledMode(CsEvalExpression expression, string source)
    {
        if (mode != CompilationMode.Compiled)
            return;

        Assert.That(expression.IsCompiled, Is.True, $"Compiled mode did not produce IL delegate for: {source}");
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0), $"Compiled mode used fallback for: {source}");
    }

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
