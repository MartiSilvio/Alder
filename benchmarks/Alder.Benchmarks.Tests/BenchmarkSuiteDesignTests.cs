using BenchmarkDotNet.Attributes;
using Alder.Compiled;
using NUnit.Framework;

namespace Alder.Benchmarks.Tests;

[TestFixture]
public class BenchmarkSuiteDesignTests
{
    private static readonly string[] ApprovedCategoryPrefixes =
    [
        "HeadToHead/",
        "Capability/",
        "Operational/"
    ];

    [Test]
    public void CrossEngineScenarios_AreBroadEnough_ForMeaningfulResults()
    {
        var scenarios = BenchmarkScenarios.GetCrossEngineScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(6),
            "Need at least 6 cross-engine scenarios to avoid overfitting to trivial expressions.");
    }

    [Test]
    public void AdvancedLanguageScenarios_AreBroadEnough_ForRepresentativeCoverage()
    {
        var scenarios = BenchmarkScenarios.GetAdvancedScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(8),
            "Need at least 8 advanced scenarios covering method chains, switch, null-conditional, LINQ, tuples.");
    }

    [Test]
    public void ScenarioNames_AreUniqueWithinEachSuite()
    {
        var crossEngineNames = BenchmarkScenarios.GetCrossEngineScenarios().Select(x => x.Name).ToList();
        var advancedNames = BenchmarkScenarios.GetAdvancedScenarios().Select(x => x.Name).ToList();
        var extendedNames = BenchmarkScenarios.GetExtendedScenarios().Select(x => x.Name).ToList();
        var linqNames = BenchmarkScenarios.GetLinqScenarios().Select(x => x.Name).ToList();
        var dynLinqNames = BenchmarkScenarios.GetDynamicLinqScenarios().Select(x => x.Name).ToList();

        Assert.That(crossEngineNames, Is.Unique, "Cross-engine scenario names must be unique.");
        Assert.That(advancedNames, Is.Unique, "Advanced scenario names must be unique.");
        Assert.That(extendedNames, Is.Unique, "Extended scenario names must be unique.");
        Assert.That(linqNames, Is.Unique, "LINQ scenario names must be unique.");
        Assert.That(dynLinqNames, Is.Unique, "Dynamic LINQ scenario names must be unique.");
    }

    [Test]
    public void DynamicLinqBenchmarks_IncludeRepresentativeCoverage_ForRecentSurfaceExpansion()
    {
        var queryNames = DynamicLinqBenchmarks.GetDynamicLinqQueries()
            .Select(x => x.Name)
            .ToArray();

        Assert.That(queryNames, Does.Contain("SetOperator+UnionCount"));
        Assert.That(queryNames, Does.Contain("Projection+TypedDtoFirst"));
        Assert.That(queryNames, Does.Contain("Sequence+SequenceEqual"));
    }

    [Test]
    public void CrossEngineScenarios_StaySemanticallyAlignedAcrossEngines()
    {
        var data = BenchmarkData.CreateStandard();

        foreach (var scenario in BenchmarkScenarios.GetCrossEngineScenarios())
        {
            var result = BenchmarkParityVerifier.VerifyCrossEngineScenario(scenario, data);
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
    }

    [Test]
    public void MatrixCatalog_ProducesDeterministicRows()
    {
        var first = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed)
            .Select(x => x.ToString())
            .ToArray();
        var second = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed)
            .Select(x => x.ToString())
            .ToArray();

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void MatrixCatalog_HasCapabilityResolution_ForEveryCaseEvaluatorPair()
    {
        var cases = MatrixCatalogBuilder.GetCrossEngineCases();
        var evaluators = MatrixCatalogBuilder.GetCrossEngineEvaluators();
        var rows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed);

        Assert.That(rows, Has.Count.EqualTo(cases.Count * evaluators.Count));
        Assert.That(rows.All(x => x.Capability.Status is BenchmarkCapabilityStatus.Supported or BenchmarkCapabilityStatus.NotSupported), Is.True);
    }

    [Test]
    public void MatrixCatalog_HasRowsForPreParsedWarmAndColdLanes()
    {
        var preParsedRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed);
        var warmRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Warm);
        var coldRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Cold);

        Assert.That(preParsedRows, Is.Not.Empty);
        Assert.That(warmRows, Is.Not.Empty);
        Assert.That(coldRows, Is.Not.Empty);
    }

    [Test]
    public void EvaluatorCatalog_IncludesExplicitAlderPreParsedEngine()
    {
        var evaluatorIds = MatrixCatalogBuilder.GetCrossEngineEvaluators()
            .Select(x => x.Id)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.That(evaluatorIds, Does.Contain("alder-interpreted-preparsed"),
            "Pre-parsed Alder should be represented as an explicit evaluator for apples-to-apples warm comparisons.");
    }

    [Test]
    public void AlderPreParsedEvaluator_IsSupportedInPreParsed_AndExplicitNAInWarmAndCold()
    {
        var preParsedRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed)
            .Where(x => x.EvaluatorId == "alder-interpreted-preparsed")
            .ToArray();
        var warmRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Warm)
            .Where(x => x.EvaluatorId == "alder-interpreted-preparsed")
            .ToArray();
        var coldRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Cold)
            .Where(x => x.EvaluatorId == "alder-interpreted-preparsed")
            .ToArray();

        Assert.That(preParsedRows, Is.Not.Empty);
        Assert.That(preParsedRows.All(x => x.Capability.IsSupported), Is.True,
            "Pre-parsed evaluator should be available in pre-parsed lane.");

        Assert.That(warmRows, Is.Not.Empty);
        Assert.That(warmRows.All(x => !x.Capability.IsSupported), Is.True,
            "Pre-parsed evaluator should be explicitly N/A for warm lane.");
        Assert.That(coldRows, Is.Not.Empty);
        Assert.That(coldRows.All(x => !x.Capability.IsSupported), Is.True,
            "Pre-parsed evaluator should be explicitly N/A for cold lane.");
        Assert.That(coldRows.All(x => x.Capability.ReasonCode == "n/a-no-cold-path"), Is.True);
    }

    [Test]
    public void MatrixCatalog_ProducesExplicitNA_ForUnsupportedEvaluatorSyntax()
    {
        var rows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.PreParsed);
        var unsupported = rows.Where(x => !x.Capability.IsSupported).ToArray();

        Assert.That(unsupported, Is.Not.Empty, "Expected explicit N/A rows for unsupported evaluator/case pairs.");
        Assert.That(unsupported.All(x => !string.IsNullOrWhiteSpace(x.Capability.ReasonCode)), Is.True,
            "Unsupported rows must include reason codes.");
    }

    [Test]
    public void WarmLane_DeclaresExplicitNA_ForEvaluatorsWithoutNonPreparedApi()
    {
        var fleeRows = MatrixCatalogBuilder.BuildRows(BenchmarkLane.Warm)
            .Where(x => x.EvaluatorId == "flee")
            .ToArray();

        Assert.That(fleeRows, Is.Not.Empty);
        Assert.That(fleeRows.All(x => !x.Capability.IsSupported), Is.True,
            "Flee should be explicitly N/A in Warm lane because it only exposes compile+execute.");
        Assert.That(fleeRows.All(x => !string.IsNullOrWhiteSpace(x.Capability.ReasonCode)), Is.True);
    }

    [Test]
    public void PreParsedLane_IncludesPreparedExecution_ForComparableCompetitors()
    {
        var rows = MatrixCatalogBuilder.BuildSupportedRows(BenchmarkLane.PreParsed)
            .Where(x => x.Category == "HeadToHead")
            .ToArray();
        var evaluatorIds = rows.Select(x => x.EvaluatorId).Distinct().ToArray();

        Assert.That(evaluatorIds, Does.Contain("dynamicexpresso"));
        Assert.That(evaluatorIds, Does.Contain("ncalc"));
        Assert.That(evaluatorIds, Does.Contain("flee"));
        Assert.That(evaluatorIds, Does.Contain("roslyn"));
    }

    [Test]
    public void WarmRunner_PrewarmsExactlyOnceBeforeMeasuredExecution()
    {
        var evaluator = new ProbeEvaluator();
        var benchmarkCase = new BenchmarkCase(
            Id: "Probe/Case",
            Category: "HeadToHead",
            WorkloadType: "Probe",
            DataProfile: "Standard",
            Expected: _ => 42,
            Expressions: new BenchmarkExpressionSet("1", "1", "1", "1", "1"));
        var row = new BenchmarkMatrixRow(
            benchmarkCase,
            evaluator,
            BenchmarkLane.Warm,
            1,
            BenchmarkCapability.Supported());

        using var prepared = WarmRunner.Prepare(row, BenchmarkData.CreateStandard());
        Assert.That(evaluator.ExecutionCount, Is.EqualTo(1), "Prepare must execute one warm-up invocation.");

        _ = WarmRunner.Execute(prepared);
        Assert.That(evaluator.ExecutionCount, Is.EqualTo(2), "Measured execution should run after warm-up.");
    }

    [Test]
    public void PreParsedRunner_PrewarmsExactlyOnceBeforeMeasuredExecution()
    {
        var evaluator = new ProbeEvaluator();
        var benchmarkCase = new BenchmarkCase(
            Id: "Probe/Case",
            Category: "HeadToHead",
            WorkloadType: "Probe",
            DataProfile: "Standard",
            Expected: _ => 42,
            Expressions: new BenchmarkExpressionSet("1", "1", "1", "1", "1"));
        var row = new BenchmarkMatrixRow(
            benchmarkCase,
            evaluator,
            BenchmarkLane.PreParsed,
            1,
            BenchmarkCapability.Supported());

        using var prepared = PreParsedRunner.Prepare(row, BenchmarkData.CreateStandard());
        Assert.That(evaluator.PreParsedExecutionCount, Is.EqualTo(1), "Prepare must execute one warm-up invocation.");

        _ = PreParsedRunner.Execute(prepared);
        Assert.That(evaluator.PreParsedExecutionCount, Is.EqualTo(2), "Measured execution should run after warm-up.");
    }

    [Test]
    public void CrossEngineColdBenchmarks_DoNotUseGlobalSetup()
    {
        var setupMethods = typeof(CrossEngineColdStartBenchmarks)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(GlobalSetupAttribute), inherit: false).Length > 0)
            .ToArray();
        var capabilitySetupMethods = typeof(CapabilityCrossEngineColdStartBenchmarks)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(GlobalSetupAttribute), inherit: false).Length > 0)
            .ToArray();

        Assert.That(setupMethods, Is.Empty,
            "Cold-start benchmarks must not use GlobalSetup because it pre-heats the benchmark process.");
        Assert.That(capabilitySetupMethods, Is.Empty,
            "Capability cold-start benchmarks must not use GlobalSetup because it pre-heats the benchmark process.");
    }

    [Test]
    public void ManifestSchema_ContainsRequiredFields()
    {
        var manifest = BenchmarkManifestWriter.BuildCatalogManifest();

        Assert.That(manifest.SchemaVersion, Is.EqualTo(BenchmarkManifestWriter.SchemaVersion));
        Assert.That(manifest.EnvironmentFingerprint, Is.Not.Empty);
        Assert.That(manifest.Rows, Is.Not.Empty);

        foreach (var row in manifest.Rows.Take(16))
        {
            Assert.That(row.Suite, Is.Not.Empty);
            Assert.That(row.Category, Is.Not.Empty);
            Assert.That(row.Lane, Is.Not.Empty);
            Assert.That(row.CaseId, Is.Not.Empty);
            Assert.That(row.EvaluatorId, Is.Not.Empty);
            Assert.That(row.Scale, Is.GreaterThan(0));
        }
    }

    [Test]
    public void CatalogManifest_DoesNotExecuteParityMatrix()
    {
        var manifest = BenchmarkManifestWriter.BuildCatalogManifest();

        Assert.That(manifest.Parity, Is.Empty,
            "Catalog manifests should be pure metadata. Parity execution belongs to dedicated verification, not schema generation.");
    }

    [Test]
    public void ManifestWriter_ProducesJsonArtifact()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "alder-bench-manifest-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var manifest = BenchmarkManifestWriter.BuildCatalogManifest();
        var path = BenchmarkManifestWriter.WriteManifest(manifest, tempDirectory);

        Assert.That(File.Exists(path), Is.True);
        var json = File.ReadAllText(path);
        Assert.That(json, Does.Contain("\"SchemaVersion\""));
        Assert.That(json, Does.Contain("\"Rows\""));
    }

    [Test]
    public void PublicBenchmarkSuite_UsesApprovedCategoryTaxonomy()
    {
        var benchmarkAssembly = typeof(CrossEngineWarmBenchmarks).Assembly;
        var categories = benchmarkAssembly
            .GetTypes()
            .SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetCustomAttributes(typeof(BenchmarkCategoryAttribute), inherit: false)
                .Cast<BenchmarkCategoryAttribute>())
            .SelectMany(attribute => attribute.Categories)
            .Distinct()
            .OrderBy(category => category)
            .ToArray();

        Assert.That(categories, Is.Not.Empty, "Expected benchmark categories to be declared explicitly.");

        foreach (var category in categories)
        {
            Assert.That(
                ApprovedCategoryPrefixes.Any(category.StartsWith),
                Is.True,
                $"Category '{category}' is outside the approved taxonomy. Use HeadToHead/, Capability/, or Operational/.");
        }
    }

    [Test]
    public void BenchmarkAssembly_DoesNotContainLegacyCompetitorNaming()
    {
        var benchmarkAssembly = typeof(CrossEngineWarmBenchmarks).Assembly;
        var legacyTypes = benchmarkAssembly.GetTypes()
            .Where(type => type.Name.Contains("Competitor", StringComparison.Ordinal))
            .Select(type => type.Name)
            .ToArray();

        Assert.That(legacyTypes, Is.Empty,
            "Legacy 'Competitor*' names must be removed in the unified cross-engine architecture.");
    }

    [Test]
    public void TypedDelegateCompilation_ProducesCorrectResults()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var fn = engine.Compile<Func<int, int, int>>("a + b", "a", "b");

        Assert.That(fn(3, 7), Is.EqualTo(10));
        Assert.That(fn(100, -50), Is.EqualTo(50));
    }

    private sealed class ProbeEvaluator : IBenchmarkEvaluator
    {
        public int ExecutionCount { get; private set; }
        public int PreParsedExecutionCount { get; private set; }

        public string Id => "probe";
        public string DisplayName => "Probe";
        public BenchmarkEngineKind EngineKind => BenchmarkEngineKind.Native;

        public BenchmarkCapability GetCapability(BenchmarkCase benchmarkCase, BenchmarkLane lane) => BenchmarkCapability.Supported();

        public EvaluatorExecutionContext PreparePreParsed(BenchmarkCase benchmarkCase, BenchmarkData data) =>
            new(state: null);

        public object? ExecutePreParsed(EvaluatorExecutionContext context, BenchmarkData data)
        {
            PreParsedExecutionCount++;
            return 42;
        }

        public EvaluatorExecutionContext PrepareWarm(BenchmarkCase benchmarkCase, BenchmarkData data) =>
            new(state: null);

        public object? ExecuteWarm(EvaluatorExecutionContext context, BenchmarkData data)
        {
            ExecutionCount++;
            return 42;
        }

        public object? ExecuteCold(BenchmarkCase benchmarkCase, BenchmarkData data)
        {
            ExecutionCount++;
            return 42;
        }
    }
}
