using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Parity;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class ParityTests(CompilationMode mode)
{
    private static readonly Regex RoslynCodeRegex = new(@"\bCS\d{4}\b", RegexOptions.Compiled);

    private AlderEngine CreateEngine() => TestEngineFactory.Create(mode, o =>
    {
        o.Constraints = new ExecutionConstraints { MaxStatements = 1_000_000 };
        o.LanguageMode = LanguageMode.Extended;
        o.Sandbox = SandboxOptions.Trusted() with
        {
            TrustedTypes =
            [
                typeof(FileAttributes),
                typeof(MemoryStream)
            ]
        };
    });

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/ValidExpressions"])]
    public async Task ValidExpressionsShouldPass(string csxPath)
    {
        var (alderExpr, roslynExpr) = await LoadExpressionPair(csxPath);
        var exprInfo = alderExpr == roslynExpr
            ? alderExpr
            : $"Alder: {alderExpr}\nRoslyn: {roslynExpr}";

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(roslynExpr);
            var engine = CreateEngine();
            var expression = engine.Parse(alderExpr);
            // ReSharper disable once MethodHasAsyncOverload
            var syncResult = engine.Evaluate(expression);
            var asyncResult = await engine.EvaluateAsync(expression);
            AssertNoFallbackInCompiledMode(expression, alderExpr);

            AssertResultEqual(syncResult, csharpResult, exprInfo);
            AssertResultEqual(asyncResult, csharpResult, $"[async] {exprInfo}");
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"{ex.GetType().Name}: {ex.Message}\n\n{exprInfo}");
        }
    }

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/ValidAsyncExpressions"])]
    public async Task AsyncExpressionsShouldPass(string csxPath)
    {
        var (alderExpr, roslynExpr) = await LoadExpressionPair(csxPath);
        var exprInfo = alderExpr == roslynExpr
            ? alderExpr
            : $"Alder: {alderExpr}\nRoslyn: {roslynExpr}";

        try
        {
            var csharpResult = await TestHelpers.EvaluateCSharpAsync(roslynExpr);
            var engine = CreateEngine();
            var asyncResult = await engine.EvaluateAsync(alderExpr);

            AssertResultEqual(asyncResult, csharpResult, exprInfo);
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Assert.Fail($"{ex.GetType().Name}: {ex.Message}\n\n{exprInfo}");
        }
    }

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/InvalidAsyncExpressions"])]
    public async Task InvalidAsyncExpressionsShouldThrow(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

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

        var engine = CreateEngine();
        Exception? alderEx = null;
        try
        {
            await engine.EvaluateAsync(expr);
            Assert.Fail($"Alder async did not throw for: {expr}");
        }
        catch (Exception ex)
        {
            alderEx = ex;
        }

        if (alderEx is not AlderException csEx)
        {
            Assert.That(alderEx, Is.InstanceOf<OverflowException>()
                    .Or.InstanceOf<DivideByZeroException>()
                    .Or.InstanceOf<ArgumentOutOfRangeException>()
                    .Or.InstanceOf<IndexOutOfRangeException>()
                    .Or.InstanceOf<InvalidOperationException>()
                    .Or.InstanceOf<InvalidCastException>()
                    .Or.InstanceOf<NullReferenceException>(),
                $"Non-AlderException thrown for '{expr}': {alderEx!.GetType().Name}: {alderEx.Message}");
            return;
        }

        ValidateErrorCodeParity(csEx, roslynEx);
    }

    [TestCaseSource(nameof(DiscoverExpressions), ["TestData/InvalidExpressions"])]
    public async Task InvalidExpressionsShouldThrow(string csxPath)
    {
        var expr = TestHelpers.LoadTestExpression(csxPath);

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

        var engine = CreateEngine();
        var alderEx = Assert.Catch<Exception>(() => engine.Evaluate(expr));
        Assert.That(alderEx, Is.Not.Null, "Alder should throw for invalid expression parity.");

        if (alderEx is not AlderException csEx)
        {
            Assert.That(alderEx, Is.InstanceOf<OverflowException>()
                    .Or.InstanceOf<DivideByZeroException>()
                    .Or.InstanceOf<ArgumentOutOfRangeException>()
                    .Or.InstanceOf<IndexOutOfRangeException>()
                    .Or.InstanceOf<InvalidOperationException>()
                    .Or.InstanceOf<InvalidCastException>()
                    .Or.InstanceOf<NullReferenceException>(),
                $"Non-AlderException thrown for '{expr}': {alderEx!.GetType().Name}: {alderEx.Message}");
            return;
        }

        Assert.That(csEx.Span.IsEmpty && csEx.Line is null, Is.False,
            $"Error for '{expr}' has no position info (Span={csEx.Span}, Line={csEx.Line})");

        ValidateErrorCodeParity(csEx, roslynEx);
    }

    private static async Task<(string AlderExpr, string RoslynExpr)> LoadExpressionPair(string csxPath)
    {
        if (csxPath.EndsWith(".roslyn.csx", StringComparison.OrdinalIgnoreCase))
        {
            var expr = (await File.ReadAllTextAsync(csxPath)).Trim();
            return (expr, expr);
        }

        var alderExpr = TestHelpers.LoadTestExpression(csxPath);
        var roslynSiblingPath = csxPath.Replace(".csx", ".roslyn.csx");

        var roslynExpr = File.Exists(roslynSiblingPath)
            ? (await File.ReadAllTextAsync(roslynSiblingPath)).Trim()
            : alderExpr;

        return (alderExpr, roslynExpr);
    }

    private static void ValidateErrorCodeParity(AlderException csEx, Exception? roslynEx)
    {
        if (csEx.ErrorCode is null)
            return;

        var alderCode = csEx.FormattedCode;
        var roslynCode = ExtractRoslynErrorCode(roslynEx?.Message);

        switch (roslynCode)
        {
            case null: // Roslyn threw a runtime exception — no compiler code to compare
            // Parser-level mismatches: both reject, but specific codes may differ
            case "CS1002" or "CS1525" or "CS8076" or "CS0201" when csEx.ErrorCode is
                DiagnosticCode.CS1003 or DiagnosticCode.CS1525 or DiagnosticCode.CS1733
                or DiagnosticCode.CS0103 or DiagnosticCode.ALDR0300:
            // Roslyn resolves at compile time (CS1061), Alder at invocation time (ALDR0304)
            case "CS1061" when csEx.ErrorCode is DiagnosticCode.ALDR0304:
            // Roslyn reports missing GetAwaiter (CS1061), Alder reports not awaitable (CS4001)
            case "CS1061" when csEx.ErrorCode is DiagnosticCode.CS4001:
                return;
        }

        if (!string.Equals(alderCode, roslynCode, StringComparison.Ordinal))
        {
            var alderKey = TestHelpers.NormalizeExceptionKey(csEx);
            var roslynKey = roslynEx != null ? TestHelpers.NormalizeExceptionKey(roslynEx) : "unknown";
            Assert.Fail(
                $"Error code mismatch: Alder threw {alderCode}, Roslyn threw {roslynCode}. " +
                $"Keys: Alder={alderKey}, Roslyn={roslynKey}. Roslyn error was: {roslynEx?.Message}");
        }
    }

    private static string? ExtractRoslynErrorCode(string? message) =>
        string.IsNullOrWhiteSpace(message) ? null
            : RoslynCodeRegex.Match(message) is { Success: true } m ? m.Value
            : null;

    private static bool IsAnonymousType(Type? type) =>
        type != null && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) &&
        type.Name.Contains("AnonymousType");

    private void AssertNoFallbackInCompiledMode(AlderExpression expression, string source)
    {
        if (mode != CompilationMode.Compiled)
            return;

        Assert.That(expression.IsCompiled, Is.True, $"Compiled mode did not produce IL delegate for: {source}");
        Assert.That(expression.BoundFallbackCount, Is.EqualTo(0), $"Compiled mode used fallback for: {source}");
    }

    private static void AssertResultEqual(object? result, object? expected, string exprInfo)
    {
        if (result is IDictionary<string, object?> dict && IsAnonymousType(expected?.GetType()))
        {
            AssertAnonymousObjectEqual(dict, expected!);
            return;
        }

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch.\n{exprInfo}");
        Assert.That(result?.GetType(), Is.EqualTo(expected?.GetType()), $"Type mismatch.\n{exprInfo}");
    }

    private static void AssertAnonymousObjectEqual(IDictionary<string, object?> dict, object anonymous)
    {
        var props = anonymous.GetType().GetProperties();
        Assert.That(dict.Count, Is.EqualTo(props.Length), "Property count mismatch");
        foreach (var prop in props)
        {
            Assert.That(dict.TryGetValue(prop.Name, out var actual), Is.True, $"Missing property '{prop.Name}'");
            Assert.That(actual, Is.EqualTo(prop.GetValue(anonymous)), $"Property '{prop.Name}' value mismatch");
        }
    }

    private static IEnumerable<TestCaseData> DiscoverExpressions(string relativePath)
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        Assert.That(Directory.Exists(testDataDir), Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            yield return new TestCaseData(file).SetName(relativeName.Replace(".csx", "").Replace('/', '_'));
        }
    }
}
