using System.Collections;
using System.Runtime.InteropServices;
using DynamicExpresso;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.Scripting;
using NCalc;

namespace Alder.Benchmarks;

public enum BenchmarkCapabilityStatus
{
    Supported,
    NotSupported
}

public readonly record struct BenchmarkCapability(BenchmarkCapabilityStatus Status, string? ReasonCode)
{
    public bool IsSupported => Status == BenchmarkCapabilityStatus.Supported;

    public static BenchmarkCapability Supported() => new(BenchmarkCapabilityStatus.Supported, null);

    public static BenchmarkCapability NotSupported(string reasonCode) =>
        new(BenchmarkCapabilityStatus.NotSupported, reasonCode);
}

public enum BenchmarkLane
{
    PreParsed,
    Warm,
    Cold
}

public enum BenchmarkEngineKind
{
    Native,
    Alder,
    Roslyn,
    NCalc,
    DynamicExpresso,
    Flee
}

public sealed record BenchmarkExpressionSet(
    string? Alder,
    string? Roslyn,
    string? NCalc,
    string? DynamicExpresso,
    string? Flee);

public sealed record BenchmarkCase(
    string Id,
    string Category,
    string WorkloadType,
    string DataProfile,
    Func<BenchmarkData, object?> Expected,
    BenchmarkExpressionSet Expressions)
{
    public override string ToString() => Id;
}

public sealed record BenchmarkMatrixRow(
    BenchmarkCase BenchmarkCase,
    IBenchmarkEvaluator Evaluator,
    BenchmarkLane Lane,
    int ScaleFactor,
    BenchmarkCapability Capability)
{
    public string Suite => "CrossEngine";
    public string Category => BenchmarkCase.Category;
    public string CaseId => BenchmarkCase.Id;
    public string EvaluatorId => Evaluator.Id;
    public string LaneName => Lane.ToString();

    public override string ToString() =>
        $"{CaseId}|{EvaluatorId}|{LaneName}|Scale={ScaleFactor}|{Capability.Status}";
}

public sealed class EvaluatorExecutionContext : IDisposable
{
    private readonly Action? _cleanup;

    public object? State { get; }

    public EvaluatorExecutionContext(object? state, Action? cleanup = null)
    {
        State = state;
        _cleanup = cleanup;
    }

    public void Dispose() => _cleanup?.Invoke();
}

public interface IBenchmarkEvaluator
{
    string Id { get; }
    string DisplayName { get; }
    BenchmarkEngineKind EngineKind { get; }

    BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane);
    EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data);
    object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data);
    EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data);
    object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data);
    object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data);
}

public sealed record PreparedRow(BenchmarkMatrixRow Row, BenchmarkData Data, EvaluatorExecutionContext Context) : IDisposable
{
    public void Dispose() => Context.Dispose();
}

public static class MatrixCatalogBuilder
{
    public static IReadOnlyList<BenchmarkCase> GetCrossEngineCases() =>
        CrossEngineCaseCatalog.GetCases();

    public static IReadOnlyList<IBenchmarkEvaluator> GetCrossEngineEvaluators() =>
        CrossEngineEvaluatorCatalog.GetEvaluators();

    public static IReadOnlyList<BenchmarkMatrixRow> BuildRows(BenchmarkLane lane, int scaleFactor = 1)
    {
        var rows = new List<BenchmarkMatrixRow>();
        var cases = GetCrossEngineCases().OrderBy(x => x.Id, StringComparer.Ordinal);
        var evaluators = GetCrossEngineEvaluators().OrderBy(x => x.Id, StringComparer.Ordinal);

        foreach (var benchmarkCase in cases)
        {
            foreach (var evaluator in evaluators)
            {
                var capability = evaluator.GetCapability(benchmarkCase, lane);
                rows.Add(new BenchmarkMatrixRow(benchmarkCase, evaluator, lane, scaleFactor, capability));
            }
        }

        return rows;
    }

    public static IReadOnlyList<BenchmarkMatrixRow> BuildSupportedRows(BenchmarkLane lane, int scaleFactor = 1) =>
        BuildRows(lane, scaleFactor).Where(x => x.Capability.IsSupported).ToArray();

    public static IReadOnlyList<BenchmarkMatrixRow> BuildUnsupportedRows(BenchmarkLane lane, int scaleFactor = 1) =>
        BuildRows(lane, scaleFactor).Where(x => !x.Capability.IsSupported).ToArray();

    public static IReadOnlyList<BenchmarkMatrixRow> BuildSupportedRowsByCategory(
        BenchmarkLane lane,
        string category,
        int scaleFactor = 1) =>
        BuildRows(lane, scaleFactor)
            .Where(x => x.Capability.IsSupported && string.Equals(x.Category, category, StringComparison.Ordinal))
            .ToArray();
}

public static class PreParsedRunner
{
    public static PreparedRow Prepare(BenchmarkMatrixRow row, BenchmarkData data)
    {
        if (row.Lane != BenchmarkLane.PreParsed)
            throw new InvalidOperationException($"Pre-parsed runner cannot execute lane '{row.Lane}'.");

        if (!row.Capability.IsSupported)
            throw new InvalidOperationException(
                $"Row {row} is not supported: {row.Capability.ReasonCode ?? "n/a"}.");

        var context = row.Evaluator.PreparePreParsed(row.BenchmarkCase, data);

        // Enforce steady-state semantics: one unmeasured execution before measured iterations.
        _ = row.Evaluator.ExecutePreParsed(context, data);

        return new PreparedRow(row, data, context);
    }

    public static object? Execute(PreparedRow prepared) =>
        prepared.Row.Evaluator.ExecutePreParsed(prepared.Context, prepared.Data);
}

public static class WarmRunner
{
    public static PreparedRow Prepare(BenchmarkMatrixRow row, BenchmarkData data)
    {
        if (row.Lane != BenchmarkLane.Warm)
            throw new InvalidOperationException($"Warm runner cannot execute lane '{row.Lane}'.");

        if (!row.Capability.IsSupported)
            throw new InvalidOperationException(
                $"Row {row} is not supported: {row.Capability.ReasonCode ?? "n/a"}.");

        var context = row.Evaluator.PrepareWarm(row.BenchmarkCase, data);

        // Enforce warm semantics: one unmeasured execution before measured iterations.
        _ = row.Evaluator.ExecuteWarm(context, data);

        return new PreparedRow(row, data, context);
    }

    public static object? Execute(PreparedRow prepared) =>
        prepared.Row.Evaluator.ExecuteWarm(prepared.Context, prepared.Data);
}

public static class ColdRunner
{
    public static object? Execute(BenchmarkMatrixRow row, BenchmarkData data)
    {
        if (row.Lane != BenchmarkLane.Cold)
            throw new InvalidOperationException($"Cold runner cannot execute lane '{row.Lane}'.");

        if (!row.Capability.IsSupported)
            throw new InvalidOperationException(
                $"Row {row} is not supported: {row.Capability.ReasonCode ?? "n/a"}.");

        return row.Evaluator.ExecuteCold(row.BenchmarkCase, data);
    }
}

public static class ParityRunner
{
    public static ParityResult VerifyRow(BenchmarkMatrixRow row, BenchmarkData data)
    {
        if (!row.Capability.IsSupported)
            return new ParityResult(true, $"{row.CaseId}/{row.EvaluatorId}: skipped ({row.Capability.ReasonCode})");

        try
        {
            var expected = row.BenchmarkCase.Expected(data);
            object? actual;

            if (row.Lane == BenchmarkLane.Cold)
            {
                actual = row.Evaluator.ExecuteCold(row.BenchmarkCase, data);
            }
            else if (row.Lane == BenchmarkLane.PreParsed)
            {
                using var context = row.Evaluator.PreparePreParsed(row.BenchmarkCase, data);
                actual = row.Evaluator.ExecutePreParsed(context, data);
            }
            else
            {
                using var context = row.Evaluator.PrepareWarm(row.BenchmarkCase, data);
                actual = row.Evaluator.ExecuteWarm(context, data);
            }

            if (!BenchmarkParityVerifier.AreEquivalent(expected, actual))
            {
                return new ParityResult(
                    false,
                    $"{row.CaseId}/{row.EvaluatorId}: expected={Format(expected)}, actual={Format(actual)}");
            }

            return new ParityResult(true, $"{row.CaseId}/{row.EvaluatorId}: parity ok");
        }
        catch (Exception ex)
        {
            return new ParityResult(false, $"{row.CaseId}/{row.EvaluatorId}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    public static IReadOnlyList<ParityResult> VerifySupportedRows(
        IEnumerable<BenchmarkMatrixRow> rows,
        BenchmarkData data) =>
        rows.Where(x => x.Capability.IsSupported).Select(x => VerifyRow(x, data)).ToArray();

    private static string Format(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => $"{value} ({value.GetType().Name})"
        };
    }
}

public static class BenchmarkManifestWriter
{
    public const string SchemaVersion = "1.0.0";

    public static BenchmarkRunManifest BuildCatalogManifest()
    {
        var preParsedRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed);
        var warmRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Warm);
        var coldRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Cold);
        var dynamicRows = BuildDynamicLinqCatalogRows();
        var allRows = preParsedRows.Concat(warmRows).Concat(coldRows).Concat(dynamicRows).ToArray();

        return new BenchmarkRunManifest
        {
            SchemaVersion = SchemaVersion,
            GeneratedAtUtc = DateTime.UtcNow,
            EnvironmentFingerprint = BuildEnvironmentFingerprint(),
            Rows = allRows.Where(x => x.Capability.IsSupported)
                .Select(ToManifestRow)
                .ToArray(),
            UnsupportedRows = allRows.Where(x => !x.Capability.IsSupported)
                .Select(ToManifestRow)
                .ToArray()
        };
    }

    public static BenchmarkRunManifest BuildRunManifest(IEnumerable<object> summaries)
    {
        var manifest = BuildCatalogManifest();
        var executed = ExtractExecutedRows(summaries);
        if (executed.Count > 0)
            manifest.Rows = executed;
        return manifest;
    }

    public static string WriteManifest(BenchmarkRunManifest manifest, string? outputDirectory = null)
    {
        var directory = outputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "benchmark-run-manifest.json");

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(manifest, options);
        File.WriteAllText(path, json);
        return path;
    }

    private static string BuildEnvironmentFingerprint()
    {
        var runtime = RuntimeInformation.FrameworkDescription;
        var os = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.ProcessArchitecture.ToString();
        var cores = Environment.ProcessorCount;
        return $"Runtime={runtime}; OS={os}; Arch={arch}; Cores={cores}";
    }

    private static BenchmarkManifestRow ToManifestRow(BenchmarkMatrixRow row)
    {
        return new BenchmarkManifestRow
        {
            Suite = row.Suite,
            Category = row.Category,
            Lane = row.LaneName,
            CaseId = row.CaseId,
            EvaluatorId = row.EvaluatorId,
            Scale = row.ScaleFactor,
            Capability = row.Capability.Status.ToString(),
            ReasonCode = row.Capability.ReasonCode
        };
    }

    private static List<BenchmarkManifestRow> ExtractExecutedRows(IEnumerable<object> summaries)
    {
        var rows = new List<BenchmarkManifestRow>();

        foreach (var summary in summaries)
        {
            var reports = ReflectProperty(summary, "Reports") as IEnumerable;
            if (reports is null)
                continue;

            foreach (var report in reports)
            {
                var benchmarkCase = ReflectProperty(report, "BenchmarkCase");
                if (benchmarkCase is null)
                    continue;

                var parameters = ReflectProperty(benchmarkCase, "Parameters") as IEnumerable;
                if (parameters is null)
                    continue;

                var foundMatrixRowForReport = false;

                foreach (var parameter in parameters)
                {
                    var name = ReflectProperty(parameter, "Name") as string;
                    if (!string.Equals(name, "Row", StringComparison.Ordinal))
                        continue;

                    var value = ReflectProperty(parameter, "Value");
                    if (value is BenchmarkMatrixRow row)
                    {
                        rows.Add(ToManifestRow(row));
                        foundMatrixRowForReport = true;
                    }
                }

                if (foundMatrixRowForReport)
                    continue;

                var queryCase = TryGetParameterValue(parameters, "Query");
                if (queryCase is not null)
                {
                    var caseName = ReflectProperty(queryCase, "Name") as string ?? "unknown";
                    var scaleValue = TryGetParameterValue(parameters, "ScaleFactor");
                    var scale = scaleValue is int i ? i : 1;
                    var descriptor = ReflectProperty(benchmarkCase, "Descriptor");
                    var workloadMethod = descriptor is null ? null : ReflectProperty(descriptor, "WorkloadMethod");
                    var methodName = workloadMethod is null ? "unknown" : ReflectProperty(workloadMethod, "Name") as string ?? "unknown";
                    var typeValue = descriptor is null ? null : ReflectProperty(descriptor, "Type");
                    var typeName = typeValue is Type t ? t.Name : string.Empty;
                    var lane = ResolveExecutedLane(typeName, methodName);
                    rows.Add(new BenchmarkManifestRow
                    {
                        Suite = "DynamicLinq",
                        Category = "Operational",
                        Lane = lane,
                        CaseId = caseName,
                        EvaluatorId = methodName,
                        Scale = scale,
                        Capability = BenchmarkCapabilityStatus.Supported.ToString()
                    });
                }
            }
        }

        return rows
            .DistinctBy(x => $"{x.Suite}|{x.Category}|{x.Lane}|{x.CaseId}|{x.EvaluatorId}|{x.Scale}")
            .OrderBy(x => x.Suite, StringComparer.Ordinal)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ThenBy(x => x.Lane, StringComparer.Ordinal)
            .ThenBy(x => x.CaseId, StringComparer.Ordinal)
            .ThenBy(x => x.EvaluatorId, StringComparer.Ordinal)
            .ToList();
    }

    private static object? ReflectProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name);
        return property?.GetValue(instance);
    }

    private static string ExtractCaseId(string message)
    {
        var colon = message.IndexOf(':', StringComparison.Ordinal);
        var head = colon > 0 ? message[..colon] : message;
        var split = head.LastIndexOf('/');
        return split > 0 ? head[..split] : "unknown";
    }

    private static string ExtractEvaluatorId(string message)
    {
        var colon = message.IndexOf(':', StringComparison.Ordinal);
        var head = colon > 0 ? message[..colon] : message;
        var split = head.LastIndexOf('/');
        if (split < 0 || split >= head.Length - 1)
            return "unknown";
        return head[(split + 1)..].Trim();
    }

    private static object? TryGetParameterValue(IEnumerable parameters, string parameterName)
    {
        foreach (var parameter in parameters)
        {
            var name = ReflectProperty(parameter, "Name") as string;
            if (string.Equals(name, parameterName, StringComparison.Ordinal))
                return ReflectProperty(parameter, "Value");
        }

        return null;
    }

    private static IReadOnlyList<BenchmarkMatrixRow> BuildDynamicLinqCatalogRows()
    {
        var rows = new List<BenchmarkMatrixRow>();
        var scaleFactors = new[] { 100, 1_000, 10_000, 100_000 };
        var cases = DynamicLinqBenchmarks.GetDynamicLinqQueries();
        var dynamicPreParsedEvaluators = new[] { "SystemDynamicLinqCore_CachedLambda" };
        var dynamicWarmEvaluators = new[] { "Native", "Alder_DynamicLinq", "SystemDynamicLinqCore" };
        var dynamicColdEvaluators = new[] { "Native", "Alder_DynamicLinq", "SystemDynamicLinqCore" };

        foreach (var scale in scaleFactors)
        {
            foreach (var query in cases)
            {
                var benchCase = new BenchmarkCase(
                    query.Name,
                    "Operational",
                    "DynamicLinq",
                    "Scale",
                    _ => null,
                    new BenchmarkExpressionSet(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

                foreach (var evaluator in dynamicPreParsedEvaluators)
                {
                    rows.Add(new BenchmarkMatrixRow(
                        benchCase,
                        new MetadataOnlyEvaluator(evaluator),
                        BenchmarkLane.PreParsed,
                        scale,
                        BenchmarkCapability.Supported()));
                }

                foreach (var evaluator in dynamicWarmEvaluators)
                {
                    rows.Add(new BenchmarkMatrixRow(
                        benchCase,
                        new MetadataOnlyEvaluator(evaluator),
                        BenchmarkLane.Warm,
                        scale,
                        BenchmarkCapability.Supported()));
                }

                foreach (var evaluator in dynamicColdEvaluators)
                {
                    rows.Add(new BenchmarkMatrixRow(
                        benchCase,
                        new MetadataOnlyEvaluator(evaluator),
                        BenchmarkLane.Cold,
                        scale,
                        BenchmarkCapability.Supported()));
                }
            }
        }

        return rows;
    }

    private static string ResolveExecutedLane(string typeName, string methodName)
    {
        if (typeName.Contains("Cold", StringComparison.Ordinal))
            return BenchmarkLane.Cold.ToString();
        if (typeName.Contains("PreParsed", StringComparison.Ordinal))
            return BenchmarkLane.PreParsed.ToString();
        if (typeName.Contains("DynamicLinqBenchmarks", StringComparison.Ordinal) &&
            methodName.Contains("CachedLambda", StringComparison.Ordinal))
            return BenchmarkLane.PreParsed.ToString();
        return BenchmarkLane.Warm.ToString();
    }
}

public sealed class BenchmarkRunManifest
{
    public string SchemaVersion { get; set; } = BenchmarkManifestWriter.SchemaVersion;
    public DateTime GeneratedAtUtc { get; set; }
    public string EnvironmentFingerprint { get; set; } = string.Empty;
    public IReadOnlyList<BenchmarkManifestRow> Rows { get; set; } = [];
    public IReadOnlyList<BenchmarkManifestRow> UnsupportedRows { get; set; } = [];
    public IReadOnlyList<ParityManifestRow> Parity { get; set; } = [];
}

public sealed class BenchmarkManifestRow
{
    public string Suite { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Lane { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string EvaluatorId { get; set; } = string.Empty;
    public int Scale { get; set; }
    public string Capability { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
}

public sealed class ParityManifestRow
{
    public string CaseId { get; set; } = string.Empty;
    public string Lane { get; set; } = string.Empty;
    public string EvaluatorId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public static class CrossEngineCaseCatalog
{
    public static IReadOnlyList<BenchmarkCase> GetCases()
    {
        return BenchmarkScenarios.GetCrossEngineScenarios()
            .Select(ToHeadToHeadCase)
            .Concat(BenchmarkScenarios.GetAdvancedScenarios().Select(ToCapabilityCase))
            .Concat(BenchmarkScenarios.GetLinqScenarios().Select(ToCapabilityCase))
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static BenchmarkCase ToHeadToHeadCase(CrossEngineScenario scenario)
    {
        var workloadType = scenario.Name.Split('/')[0];
        return new BenchmarkCase(
            Id: scenario.Name,
            Category: "HeadToHead",
            WorkloadType: workloadType,
            DataProfile: "Standard",
            Expected: scenario.Native,
            Expressions: new BenchmarkExpressionSet(
                scenario.AlderExpr,
                scenario.RoslynExpr,
                scenario.NCalcExpr,
                scenario.DExpressoExpr,
                scenario.FleeExpr));
    }

    private static BenchmarkCase ToCapabilityCase(AlderScenario scenario)
    {
        var workloadType = scenario.Name.Split('/')[0];
        return new BenchmarkCase(
            Id: scenario.Name,
            Category: "Capability",
            WorkloadType: workloadType,
            DataProfile: "Standard",
            Expected: scenario.Native,
            Expressions: new BenchmarkExpressionSet(
                scenario.AlderExpr,
                scenario.RoslynExpr,
                null,
                null,
                null));
    }
}

public static class CrossEngineEvaluatorCatalog
{
    private static readonly IBenchmarkEvaluator[] Evaluators =
    [
        new NativeEvaluator(),
        new AlderInterpretedEvaluator(),
        new AlderInterpretedPreParsedEvaluator(),
        new AlderCompiledEvaluator(),
        new AlderCompiledFecEvaluator(),
        new RoslynEvaluator(),
        new NCalcEvaluator(),
        new DynamicExpressoEvaluator(),
        new FleeEvaluator()
    ];

    public static IReadOnlyList<IBenchmarkEvaluator> GetEvaluators() => Evaluators;

    private sealed class NativeEvaluator : IBenchmarkEvaluator
    {
        public string Id => "native";
        public string DisplayName => "Native";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Native;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) => BenchmarkCapability.Supported();

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data) =>
            new(state: new BenchmarkCaseExpectedState(benchmarkCase.Expected));

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data) =>
            ((BenchmarkCaseExpectedState?)context.State)?.Expected(data) ?? throw new InvalidOperationException(
                "Native pre-parsed evaluator state was not initialized.");

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data) =>
            new(state: new BenchmarkCaseExpectedState(benchmarkCase.Expected));

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data) =>
            ((BenchmarkCaseExpectedState?)context.State)?.Expected(data) ?? throw new InvalidOperationException(
                "Native warm evaluator state was not initialized.");

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data) => benchmarkCase.Expected(data);
    }

    private abstract class AlderEvaluatorBase : IBenchmarkEvaluator
    {
        private readonly CompilationMode _mode;
        private readonly bool _supportsPreParsedLane;
        private readonly bool _supportsWarmLane;
        private readonly bool _supportsColdLane;

        protected AlderEvaluatorBase(
            string id,
            string displayName,
            CompilationMode mode,
            bool supportsPreParsedLane,
            bool supportsWarmLane,
            bool supportsColdLane)
        {
            Id = id;
            DisplayName = displayName;
            _mode = mode;
            _supportsPreParsedLane = supportsPreParsedLane;
            _supportsWarmLane = supportsWarmLane;
            _supportsColdLane = supportsColdLane;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Alder;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane)
        {
            if (string.IsNullOrWhiteSpace(benchmarkCase.Expressions.Alder))
                return BenchmarkCapability.NotSupported("n/a-use-native-equivalent");

            return lane switch
            {
                BenchmarkLane.PreParsed when !_supportsPreParsedLane => BenchmarkCapability.NotSupported("n/a-no-preparsed-path"),
                BenchmarkLane.Warm when !_supportsWarmLane => BenchmarkCapability.NotSupported("n/a-no-warm-path"),
                BenchmarkLane.Cold when !_supportsColdLane => BenchmarkCapability.NotSupported("n/a-no-cold-path"),
                _ => BenchmarkCapability.Supported()
            };
        }

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            EnsureLaneSupported(BenchmarkLane.PreParsed);
            var expression = benchmarkCase.Expressions.Alder
                ?? throw new InvalidOperationException($"Missing Alder expression for case '{benchmarkCase.Id}'.");
            var engine = BenchmarkBase.CreateEngine(_mode, data);
            var parsed = engine.Parse(expression);

            // PreParsed lane must isolate execution cost from first-hit compile cost.
            // For compiled backends, compile eagerly during setup.
            if (_mode is CompilationMode.Compiled or CompilationMode.CompiledFec)
                engine.Compile(parsed);

            return new EvaluatorExecutionContext(new AlderPreParsedState(engine, parsed), () => engine.Dispose());
        }

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (AlderPreParsedState)context.State!;
            return state.Engine.Evaluate(state.Expression);
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            EnsureLaneSupported(BenchmarkLane.Warm);
            var expression = benchmarkCase.Expressions.Alder
                ?? throw new InvalidOperationException($"Missing Alder expression for case '{benchmarkCase.Id}'.");
            var engine = BenchmarkBase.CreateEngine(_mode, data);
            return new EvaluatorExecutionContext(new AlderWarmState(engine, expression), () => engine.Dispose());
        }

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (AlderWarmState)context.State!;
            return state.Engine.Evaluate(state.Source);
        }

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            EnsureLaneSupported(BenchmarkLane.Cold);
            var expression = benchmarkCase.Expressions.Alder
                ?? throw new InvalidOperationException($"Missing Alder expression for case '{benchmarkCase.Id}'.");
            using var engine = BenchmarkBase.CreateEngine(_mode, data);
            return engine.Evaluate(expression);
        }

        private void EnsureLaneSupported(BenchmarkLane lane)
        {
            if (lane == BenchmarkLane.PreParsed && !_supportsPreParsedLane)
                throw new InvalidOperationException($"{Id} does not support pre-parsed lane.");
            if (lane == BenchmarkLane.Warm && !_supportsWarmLane)
                throw new InvalidOperationException($"{Id} does not support warm lane.");
            if (lane == BenchmarkLane.Cold && !_supportsColdLane)
                throw new InvalidOperationException($"{Id} does not support cold lane.");
        }

        protected sealed record AlderPreParsedState(AlderEngine Engine, AlderExpression Expression);
        protected sealed record AlderWarmState(AlderEngine Engine, string Source);
    }

    private sealed class AlderInterpretedEvaluator : AlderEvaluatorBase
    {
        public AlderInterpretedEvaluator()
            : base(
                "alder-interpreted",
                "Alder Interpreted (Parse+Execute)",
                CompilationMode.Interpreted,
                supportsPreParsedLane: false,
                supportsWarmLane: true,
                supportsColdLane: true)
        {
        }
    }

    private sealed class AlderInterpretedPreParsedEvaluator : AlderEvaluatorBase
    {
        public AlderInterpretedPreParsedEvaluator()
            : base(
                "alder-interpreted-preparsed",
                "Alder Interpreted (PreParsed)",
                CompilationMode.Interpreted,
                supportsPreParsedLane: true,
                supportsWarmLane: false,
                supportsColdLane: false)
        {
        }
    }

    private sealed class AlderCompiledEvaluator : AlderEvaluatorBase
    {
        public AlderCompiledEvaluator()
            : base(
                "alder-compiled",
                "Alder Compiled",
                CompilationMode.Compiled,
                supportsPreParsedLane: true,
                supportsWarmLane: true,
                supportsColdLane: true)
        {
        }
    }

    private sealed class AlderCompiledFecEvaluator : AlderEvaluatorBase
    {
        public AlderCompiledFecEvaluator()
            : base(
                "alder-compiled-fec",
                "Alder Compiled FEC",
                CompilationMode.CompiledFec,
                supportsPreParsedLane: true,
                supportsWarmLane: true,
                supportsColdLane: true)
        {
        }
    }

    private sealed class RoslynEvaluator : IBenchmarkEvaluator
    {
        public string Id => "roslyn";
        public string DisplayName => "Roslyn";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Roslyn;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) =>
            string.IsNullOrWhiteSpace(benchmarkCase.Expressions.Roslyn)
                ? BenchmarkCapability.NotSupported("n/a-use-native-equivalent")
                : BenchmarkCapability.Supported();

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expression = benchmarkCase.Expressions.Roslyn
                ?? throw new InvalidOperationException($"Missing Roslyn expression for case '{benchmarkCase.Id}'.");
            var script = BenchmarkBase.CreateRoslynScript(expression);
            script.Compile();
            ScriptRunner<object> runner = script.CreateDelegate();
            return new EvaluatorExecutionContext(new RoslynPreParsedState(runner));
        }

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (RoslynPreParsedState)context.State!;
            return state.Runner(data).GetAwaiter().GetResult();
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expression = benchmarkCase.Expressions.Roslyn
                ?? throw new InvalidOperationException($"Missing Roslyn expression for case '{benchmarkCase.Id}'.");
            return new EvaluatorExecutionContext(new RoslynWarmState(expression));
        }

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (RoslynWarmState)context.State!;
            return BenchmarkBase.EvaluateRoslynAsync(state.Expression, data).GetAwaiter().GetResult();
        }

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data) =>
            BenchmarkBase.EvaluateRoslynAsync(
                benchmarkCase.Expressions.Roslyn
                    ?? throw new InvalidOperationException($"Missing Roslyn expression for case '{benchmarkCase.Id}'."),
                data).GetAwaiter().GetResult();

        private sealed record RoslynPreParsedState(ScriptRunner<object> Runner);
        private sealed record RoslynWarmState(string Expression);
    }

    private sealed class NCalcEvaluator : IBenchmarkEvaluator
    {
        public string Id => "ncalc";
        public string DisplayName => "NCalc";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.NCalc;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) =>
            string.IsNullOrWhiteSpace(benchmarkCase.Expressions.NCalc)
                ? BenchmarkCapability.NotSupported("n/a-use-native-equivalent")
                : BenchmarkCapability.Supported();

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expressionText = benchmarkCase.Expressions.NCalc
                ?? throw new InvalidOperationException($"Missing NCalc expression for case '{benchmarkCase.Id}'.");
            var expression = new Expression(expressionText);
            BenchmarkParityVerifier.ApplyNCalcParameters(expression, data);
            return new EvaluatorExecutionContext(new NCalcPreParsedState(expression));
        }

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (NCalcPreParsedState)context.State!;
            return state.Expression.Evaluate();
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expressionText = benchmarkCase.Expressions.NCalc
                ?? throw new InvalidOperationException($"Missing NCalc expression for case '{benchmarkCase.Id}'.");
            return new EvaluatorExecutionContext(new NCalcWarmState(expressionText));
        }

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (NCalcWarmState)context.State!;
            var expression = new Expression(state.ExpressionText);
            BenchmarkParityVerifier.ApplyNCalcParameters(expression, data);
            return expression.Evaluate();
        }

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expressionText = benchmarkCase.Expressions.NCalc
                ?? throw new InvalidOperationException($"Missing NCalc expression for case '{benchmarkCase.Id}'.");
            var expression = new Expression(expressionText);
            BenchmarkParityVerifier.ApplyNCalcParameters(expression, data);
            return expression.Evaluate();
        }

        private sealed record NCalcPreParsedState(Expression Expression);
        private sealed record NCalcWarmState(string ExpressionText);
    }

    private sealed class DynamicExpressoEvaluator : IBenchmarkEvaluator
    {
        public string Id => "dynamicexpresso";
        public string DisplayName => "DynamicExpresso";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.DynamicExpresso;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) =>
            string.IsNullOrWhiteSpace(benchmarkCase.Expressions.DynamicExpresso)
                ? BenchmarkCapability.NotSupported("n/a-use-native-equivalent")
                : BenchmarkCapability.Supported();

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expression = benchmarkCase.Expressions.DynamicExpresso
                ?? throw new InvalidOperationException($"Missing DynamicExpresso expression for case '{benchmarkCase.Id}'.");
            var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(data);
            var lambda = interpreter.Parse(expression);
            return new EvaluatorExecutionContext(new DynamicExpressoPreParsedState(lambda));
        }

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (DynamicExpressoPreParsedState)context.State!;
            return state.Lambda.Invoke();
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expression = benchmarkCase.Expressions.DynamicExpresso
                ?? throw new InvalidOperationException($"Missing DynamicExpresso expression for case '{benchmarkCase.Id}'.");
            var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(data);
            return new EvaluatorExecutionContext(new DynamicExpressoWarmState(interpreter, expression));
        }

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (DynamicExpressoWarmState)context.State!;
            return state.Interpreter.Eval(state.Expression);
        }

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expression = benchmarkCase.Expressions.DynamicExpresso
                ?? throw new InvalidOperationException($"Missing DynamicExpresso expression for case '{benchmarkCase.Id}'.");
            var interpreter = BenchmarkBase.CreateDynamicExpressoInterpreter(data);
            var lambda = interpreter.Parse(expression);
            return lambda.Invoke();
        }

        private sealed record DynamicExpressoPreParsedState(Lambda Lambda);
        private sealed record DynamicExpressoWarmState(Interpreter Interpreter, string Expression);
    }

    private sealed class FleeEvaluator : IBenchmarkEvaluator
    {
        public string Id => "flee";
        public string DisplayName => "Flee";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Flee;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane)
        {
            if (string.IsNullOrWhiteSpace(benchmarkCase.Expressions.Flee))
                return BenchmarkCapability.NotSupported("n/a-use-native-equivalent");

            return lane == BenchmarkLane.Warm
                ? BenchmarkCapability.NotSupported("n/a-no-warm-eval-api")
                : BenchmarkCapability.Supported();
        }

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expressionText = benchmarkCase.Expressions.Flee
                ?? throw new InvalidOperationException($"Missing Flee expression for case '{benchmarkCase.Id}'.");
            var context = BenchmarkBase.CreateFleeContext(data);
            var expression = context.CompileDynamic(expressionText);
            return new EvaluatorExecutionContext(new FleePreParsedState(expression));
        }

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            var state = (FleePreParsedState)context.State!;
            return state.Expression.Evaluate();
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data)
            => throw new NotSupportedException("Flee warm lane is not supported because there is no non-prepared eval API.");

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
            => throw new NotSupportedException("Flee warm lane is not supported because there is no non-prepared eval API.");

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            var expressionText = benchmarkCase.Expressions.Flee
                ?? throw new InvalidOperationException($"Missing Flee expression for case '{benchmarkCase.Id}'.");
            var context = BenchmarkBase.CreateFleeContext(data);
            var expression = context.CompileDynamic(expressionText);
            return expression.Evaluate();
        }

        private sealed record FleePreParsedState(IDynamicExpression Expression);
    }

    private sealed record BenchmarkCaseExpectedState(Func<BenchmarkData, object?> Expected);
}

internal sealed class MetadataOnlyEvaluator : IBenchmarkEvaluator
{
    public MetadataOnlyEvaluator(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string DisplayName => Id;
    public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Native;

    public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) =>
        BenchmarkCapability.NotSupported("metadata-only");

    public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data) =>
        throw new NotSupportedException("Metadata-only evaluator cannot execute.");

    public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data) =>
        throw new NotSupportedException("Metadata-only evaluator cannot execute.");

    public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data) =>
        throw new NotSupportedException("Metadata-only evaluator cannot execute.");

    public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data) =>
        throw new NotSupportedException("Metadata-only evaluator cannot execute.");

    public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data) =>
        throw new NotSupportedException("Metadata-only evaluator cannot execute.");
}
