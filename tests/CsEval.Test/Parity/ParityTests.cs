using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CsEval.Test.Parity;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class ParityTests(CompilationMode mode)
{
    private static readonly Regex RoslynCodeRegex = new(@"\bCS\d{4}\b", RegexOptions.Compiled);

    private CsEvalEngine CreateEngine() => TestEngineFactory.Create(mode, CsEvalOptions.Default with
    {
        Constraints = new ExecutionConstraints { MaxStatements = 1_000_000 },
        LanguageMode = LanguageMode.Extended
    });

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
            var engine = CreateEngine();
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
        var engine = CreateEngine();
        var csEvalEx = Assert.Catch<Exception>(() => engine.Evaluate(expr));
        Assert.That(csEvalEx, Is.Not.Null, "CsEval should throw for invalid expression parity.");

        // CLR runtime exceptions (OverflowException, DivideByZeroException) are valid —
        // real C# throws these too. Only CsEval's own errors must be CsEvalException.
        if (csEvalEx is not CsEvalException csEx)
        {
            Assert.That(csEvalEx, Is.InstanceOf<OverflowException>()
                .Or.InstanceOf<DivideByZeroException>(),
                $"Non-CsEvalException thrown for '{expr}': {csEvalEx!.GetType().Name}: {csEvalEx.Message}");
            return;
        }

        // Every CsEvalException must carry position information
        Assert.That(csEx.Span.IsEmpty && csEx.Line is null, Is.False,
            $"Error for '{expr}' has no position info (Span={csEx.Span}, Line={csEx.Line})");

        var csEvalKey = TestHelpers.NormalizeExceptionKey(csEx);
        var roslynKey = roslynEx != null ? TestHelpers.NormalizeExceptionKey(roslynEx) : "unknown";

        // Validate error codes match when both have them
        if (csEx.ErrorCode is not null)
        {
            var csEvalCode = csEx.FormattedCode;
            var roslynCode = ExtractRoslynErrorCode(roslynEx?.Message);

            // If Roslyn threw a runtime exception (no compiler code), only require both sides to throw.
            if (roslynCode == null)
                return;

            // Skip code parity for parser-level mismatches where Roslyn can't parse Extended syntax.
            // Roslyn gives generic errors (CS1002 etc.) for syntax it doesn't understand;
            // CsEval gives meaningful errors (CS1003 etc.) for its own grammar.
            if (roslynCode == "CS1002" && csEx.ErrorCode is CsEval.Diagnostics.DiagnosticCode.CS1003 or CsEval.Diagnostics.DiagnosticCode.CS1525 or CsEval.Diagnostics.DiagnosticCode.CS1733)
                return;

            // Check exact code parity for compiler diagnostics.
            if (!string.Equals(csEvalCode, roslynCode, StringComparison.Ordinal))
            {
                Assert.Fail(
                    $"Error code mismatch: CsEval threw {csEvalCode}, Roslyn threw {roslynCode}. " +
                    $"Keys: CsEval={csEvalKey}, Roslyn={roslynKey}. Roslyn error was: {roslynEx?.Message}");
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

    // Out-of-scope parity tests: statement-level features not relevant for expression evaluation
    private static readonly HashSet<string> SkippedParityTests = new(StringComparer.OrdinalIgnoreCase)
    {
        "ControlFlow/Goto_ForwardLabel",
        "ControlFlow/Goto_SkipStatement",
    };

    private static IEnumerable<TestCaseData> DiscoverExpressions(string relativePath)
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        Assert.That(new DirectoryInfo(testDataDir).Exists, Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            var testName = relativeName.Replace(".csx", "").Replace('/', '_');
            var keyWithoutExt = relativeName.Replace(".csx", "");
            if (SkippedParityTests.Any(s => keyWithoutExt.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                continue;
            yield return new TestCaseData(file).SetName(testName);
        }
    }
}
