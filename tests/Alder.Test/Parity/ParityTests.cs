using System.Runtime.CompilerServices;
using System.Reflection;
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
            AssertNoFallbackInCompiledMode(engine, expression, alderExpr);

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

        Exception? alderEx = null;
        try
        {
            // ReSharper disable once MethodHasAsyncOverload
            engine.Evaluate(expr);
        }
        catch (Exception ex)
        {
            alderEx = ex;
        }

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
            // Roslyn distinguishes static-member-not-found (CS0117) from instance-member-not-found
            // (CS1061) at compile time. Alder is a dynamic environment and reports both as CS1061
            // at runtime, since the receiver shape isn't always known until evaluation.
            case "CS0117" when csEx.ErrorCode is DiagnosticCode.CS1061:
            // Roslyn script mode wraps top-level locals into script-class fields, so referencing
            // a prior `int y = 5` from a `const int x = y;` fires CS0120 (instance field needs
            // an object reference). Alder treats them as true locals and correctly reports the
            // §13.6.3 constant-initializer violation.
            case "CS0120" when csEx.ErrorCode is DiagnosticCode.CS0133:
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

    private void AssertNoFallbackInCompiledMode(AlderEngine engine, AlderExpression expression, string source)
    {
        if (mode != CompilationMode.Compiled)
            return;

        Assert.That(engine.HasCompiledDelegate(expression), Is.True, $"Compiled mode did not produce IL delegate for: {source}");
        Assert.That(engine.GetBoundFallbackCount(expression), Is.EqualTo(0), $"Compiled mode used fallback for: {source}");
    }

    private static void AssertResultEqual(object? result, object? expected, string exprInfo)
    {
        if (TryReadStructuralParityProperties(expected, result, out var expectedProperties, out var actualProperties))
        {
            AssertStructuralObjectEqual(actualProperties, expectedProperties);
            return;
        }

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch.\n{exprInfo}");
        Assert.That(result?.GetType(), Is.EqualTo(expected?.GetType()), $"Type mismatch.\n{exprInfo}");
    }

    private static void AssertStructuralObjectEqual(
        IReadOnlyDictionary<string, object?> actualProperties,
        IReadOnlyDictionary<string, object?> expectedProperties)
    {
        Assert.That(actualProperties.Count, Is.EqualTo(expectedProperties.Count), "Property count mismatch");
        foreach (var (name, expectedValue) in expectedProperties)
        {
            Assert.That(actualProperties.TryGetValue(name, out var actualValue), Is.True, $"Missing property '{name}'");
            Assert.That(actualValue, Is.EqualTo(expectedValue), $"Property '{name}' value mismatch");
        }
    }

    private static bool TryReadStructuralParityProperties(
        object? expected,
        object? result,
        out IReadOnlyDictionary<string, object?> expectedProperties,
        out IReadOnlyDictionary<string, object?> actualProperties)
    {
        expectedProperties = null!;
        actualProperties = null!;

        if (expected == null || !TryReadObjectProperties(result, out actualProperties))
            return false;

        if (IsAnonymousType(expected.GetType()))
        {
            return TryReadObjectProperties(expected, out expectedProperties);
        }

        return false;
    }

    private static bool TryReadObjectProperties(object? value, out IReadOnlyDictionary<string, object?> properties)
    {
        properties = null!;
        if (value == null)
            return false;
        if (value is Type)
            return false;

        if (value is IDictionary<string, object?> dict)
        {
            properties = new Dictionary<string, object?>(dict);
            return true;
        }

        if (value is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            properties = new Dictionary<string, object?>(readOnlyDict);
            return true;
        }

        var readableProperties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray();
        if (readableProperties.Length == 0)
            return false;

        properties = readableProperties.ToDictionary(property => property.Name, property => property.GetValue(value));
        return true;
    }

    private static IEnumerable<TestCaseData> DiscoverExpressions(string relativePath)
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        Assert.That(Directory.Exists(testDataDir), Is.True);

        foreach (var file in Directory.GetFiles(testDataDir, "*.csx", SearchOption.AllDirectories))
        {
            // .ignore.csx files are known limitations, excluded from automated runs
            if (file.EndsWith(".ignore.csx", StringComparison.OrdinalIgnoreCase))
                continue;

            var relativeName = Path.GetRelativePath(testDataDir, file).Replace(Path.DirectorySeparatorChar, '/');
            yield return new TestCaseData(file).SetName(relativeName.Replace(".csx", "").Replace('/', '_'));
        }
    }
}
