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
    public void BenchmarkProfileParser_NoArgs_ShowsHelp()
    {
        var command = BenchmarkCommand.Parse([]);

        Assert.That(command.Kind, Is.EqualTo(BenchmarkCommandKind.ShowHelp));
        Assert.That(command.Profile, Is.EqualTo(BenchmarkRunProfile.Custom));
        Assert.That(command.BenchmarkDotNetArgs, Is.Empty);
    }

    [Test]
    public void BenchmarkProfileParser_ValidateProfile_RunsValidation()
    {
        var command = BenchmarkCommand.Parse(["--profile", "validate"]);

        Assert.That(command.Kind, Is.EqualTo(BenchmarkCommandKind.Validate));
        Assert.That(command.Profile, Is.EqualTo(BenchmarkRunProfile.Validate));
        Assert.That(command.BenchmarkDotNetArgs, Is.Empty);
    }

    [TestCase("perf-smoke", BenchmarkRunProfile.PerfSmoke)]
    [TestCase("publish", BenchmarkRunProfile.Publish)]
    [TestCase("exhaustive", BenchmarkRunProfile.Exhaustive)]
    public void BenchmarkProfileParser_BenchmarkProfiles_RunBenchmarks(string value, BenchmarkRunProfile profile)
    {
        var command = BenchmarkCommand.Parse(["--profile", value]);

        Assert.That(command.Kind, Is.EqualTo(BenchmarkCommandKind.RunBenchmarks));
        Assert.That(command.Profile, Is.EqualTo(profile));
    }

    [Test]
    public void BenchmarkProfileParser_DirectBenchmarkDotNetArgs_RequireProfile()
    {
        Assert.Throws<ArgumentException>(() => BenchmarkCommand.Parse(["--filter", "*DynamicLinq*"]));
    }

    [TestCase("--job")]
    [TestCase("--launchCount")]
    [TestCase("--iterationCount")]
    [TestCase("--warmupCount")]
    public void BenchmarkProfileParser_RejectsBenchmarkDotNetPolicyArgs(string argument)
    {
        Assert.Throws<ArgumentException>(() => BenchmarkCommand.Parse(["--profile", "publish", argument, "1"]));
    }

    [Test]
    public void BenchmarkProfileParser_RemovesProfileArgsBeforeBenchmarkDotNet()
    {
        var command = BenchmarkCommand.Parse(["--profile", "publish", "--filter", "*DynamicLinq*"]);

        Assert.That(command.Profile, Is.EqualTo(BenchmarkRunProfile.Publish));
        Assert.That(command.BenchmarkDotNetArgs, Is.EqualTo(new[] { "--filter", "*DynamicLinq*" }));
    }

    [Test]
    public void BenchmarkProfileParser_AllowsListArgument()
    {
        var command = BenchmarkCommand.Parse(["--profile", "publish", "--list", "flat"]);

        Assert.That(command.Profile, Is.EqualTo(BenchmarkRunProfile.Publish));
        Assert.That(command.BenchmarkDotNetArgs, Is.EqualTo(new[] { "--list", "flat" }));
    }

    [Test]
    public void BenchmarkProfileParser_ValidateRejectsBenchmarkDotNetArgs()
    {
        Assert.Throws<ArgumentException>(() => BenchmarkCommand.Parse(["--profile", "validate", "--filter", "*"]));
    }

    [Test]
    public void BenchmarkProfileContext_InvalidEnvironmentProfile_Throws()
    {
        WithBenchmarkProfileEnvironment("publsh", () =>
        {
            Assert.Throws<InvalidOperationException>(() => _ = BenchmarkProfileContext.Current);
        });
    }

    [Test]
    public void BenchmarkProfileContext_PublishProfile_IgnoresBdnQuick()
    {
        var previousQuick = Environment.GetEnvironmentVariable("BDN_QUICK");
        try
        {
            Environment.SetEnvironmentVariable("BDN_QUICK", "1");
            WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Publish.ToString(), () =>
            {
                Assert.That(BenchmarkProfileContext.UsesShortRun, Is.False);
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("BDN_QUICK", previousQuick);
        }
    }

    [Test]
    public void BenchmarkProfileContext_LegacyQuickEnvironment_DoesNotChangeCustomPolicy()
    {
        var previousQuick = Environment.GetEnvironmentVariable("BDN_QUICK");
        try
        {
            Environment.SetEnvironmentVariable("BDN_QUICK", "1");
            WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Custom.ToString(), () =>
            {
                Assert.That(BenchmarkProfileContext.UsesShortRun, Is.False);
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("BDN_QUICK", previousQuick);
        }
    }

    [Test]
    public void BenchmarkProfiles_DefineCentralRunPolicy()
    {
        var smoke = BenchmarkProfileDefinition.For(BenchmarkRunProfile.PerfSmoke);
        var publish = BenchmarkProfileDefinition.For(BenchmarkRunProfile.Publish);
        var exhaustive = BenchmarkProfileDefinition.For(BenchmarkRunProfile.Exhaustive);

        Assert.That(smoke.MeasurementMode, Is.EqualTo(BenchmarkMeasurementMode.ShortRun));
        Assert.That(publish.MeasurementMode, Is.EqualTo(BenchmarkMeasurementMode.Default));
        Assert.That(exhaustive.MeasurementMode, Is.EqualTo(BenchmarkMeasurementMode.Default));
        Assert.That(publish.DynamicLinqQueryScope, Is.EqualTo(BenchmarkMatrixScope.Default));
        Assert.That(exhaustive.DynamicLinqQueryScope, Is.EqualTo(BenchmarkMatrixScope.Exhaustive));
        Assert.That(publish.DynamicLinqScaleFactors, Is.EqualTo(new[] { 10_000 }));
        Assert.That(exhaustive.DynamicLinqScaleFactors, Is.EqualTo(new[] { 100, 1_000, 10_000, 100_000 }));
    }

    [Test]
    public void BenchmarkProfiles_OwnOperationalScaleMatrices()
    {
        var smoke = BenchmarkProfileDefinition.For(BenchmarkRunProfile.PerfSmoke);
        var publish = BenchmarkProfileDefinition.For(BenchmarkRunProfile.Publish);
        var exhaustive = BenchmarkProfileDefinition.For(BenchmarkRunProfile.Exhaustive);

        Assert.That(smoke.CollectionPipelineScaleFactors, Is.EqualTo(new[] { 1_000 }));
        Assert.That(publish.CollectionPipelineScaleFactors, Is.EqualTo(new[] { 1_000, 10_000 }));
        Assert.That(exhaustive.CollectionPipelineScaleFactors, Is.EqualTo(new[] { 100, 1_000, 10_000, 100_000 }));
        Assert.That(smoke.CompilationReuseCounts, Is.EqualTo(new[] { 1, 100 }));
        Assert.That(publish.CompilationReuseCounts, Is.EqualTo(new[] { 1, 10, 100, 1_000 }));
        Assert.That(exhaustive.CompilationReuseCounts, Is.EqualTo(new[] { 1, 5, 10, 50, 100, 500, 1_000 }));
        Assert.That(smoke.ThroughputThreadCounts, Is.EqualTo(new[] { 1, 4 }));
        Assert.That(publish.ThroughputThreadCounts, Is.EqualTo(new[] { 1, 4, 8 }));
        Assert.That(exhaustive.ThroughputThreadCounts, Is.EqualTo(new[] { 1, 2, 4, 8 }));
    }

    [Test]
    public void DynamicLinqBenchmarks_ExhaustiveProfile_ExpandsRunMatrix()
    {
        WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Exhaustive.ToString(), () =>
        {
            Assert.That(DynamicLinqBenchmarks.GetBenchmarkQueries().Count,
                Is.EqualTo(DynamicLinqBenchmarks.GetDynamicLinqQueries().Count));
            Assert.That(DynamicLinqBenchmarks.GetBenchmarkScaleFactors().Count, Is.EqualTo(4));
        });
    }

    [Test]
    public void DynamicLinqBenchmarks_LegacyExhaustiveEnvironment_DoesNotExpandRunMatrix()
    {
        var previous = Environment.GetEnvironmentVariable("ALDER_DYNAMIC_LINQ_EXHAUSTIVE");
        try
        {
            Environment.SetEnvironmentVariable("ALDER_DYNAMIC_LINQ_EXHAUSTIVE", "1");
            WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Custom.ToString(), () =>
            {
                Assert.That(DynamicLinqBenchmarks.GetBenchmarkQueries().Count, Is.LessThan(DynamicLinqBenchmarks.GetDynamicLinqQueries().Count));
                Assert.That(DynamicLinqBenchmarks.GetBenchmarkScaleFactors(), Is.EqualTo(new[] { 10_000 }));
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable("ALDER_DYNAMIC_LINQ_EXHAUSTIVE", previous);
        }
    }

    [Test]
    public void RunManifest_IncludesPublishEvidenceMetadata()
    {
        var manifest = BenchmarkManifestWriter.BuildRunManifest(
            [],
            BenchmarkRunProfile.Publish,
            ["--profile", "publish", "--filter", "*DynamicLinq*"]);

        Assert.That(manifest.Profile, Is.EqualTo("Publish"));
        Assert.That(manifest.CommandLine, Does.Contain("--profile publish --filter *DynamicLinq*"));
        Assert.That(manifest.RepositoryCommit, Is.Not.Empty);
        Assert.That(manifest.DotNetInfo, Is.Not.Empty);
        Assert.That(manifest.CpuModel, Is.Not.Empty);
    }

    [Test]
    public void BenchmarkSmokeValidator_ValidationMatrix_IncludesSupportedFecRows()
    {
        foreach (var lane in new[] { BenchmarkLane.PreParsed, BenchmarkLane.Warm, BenchmarkLane.Cold })
        {
            var rows = BenchmarkSmokeValidator.BuildValidationRows(lane);

            Assert.That(rows, Is.Not.Empty);
            Assert.That(rows.Where(row => row.EvaluatorId == "alder-compiled-fec"), Is.Not.Empty);
        }
    }

    [Test]
    public void MatrixCatalog_FecDoesNotAdvertiseUnsupportedExecution()
    {
        var rows = Enum.GetValues<BenchmarkLane>()
            .SelectMany(lane => MatrixCatalogBuilder.BuildRows(lane))
            .Where(row => row.EvaluatorId == "alder-compiled-fec"
                && BenchmarkFecPolicy.IsUnsupportedExpression(row.BenchmarkCase.Id, row.BenchmarkCase.Expressions.Alder))
            .ToArray();

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.All(row => !row.Capability.IsSupported), Is.True);
        Assert.That(rows.All(row => row.Capability.ReasonCode == BenchmarkFecPolicy.UnsupportedReasonCode), Is.True);
    }

    [Test]
    public void BenchmarkParityVerifier_UnsupportedFecScenario_ReportsExplicitNA()
    {
        var scenario = new CrossEngineScenario(
            "Unsupported/Fec",
            "1 + 2",
            "1 + 2",
            "1 + 2",
            "1 + 2",
            "1 + 2",
            _ => 3);

        var result = BenchmarkParityVerifier.VerifyCrossEngineScenario(
            scenario,
            BenchmarkData.CreateStandard());

        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(result.Message, Does.Contain(BenchmarkFecPolicy.UnsupportedReasonCode));
    }

    [Test]
    public void CatalogManifest_IncludesDynamicLinqParsedPlanLane()
    {
        var manifest = BenchmarkManifestWriter.BuildCatalogManifest();

        Assert.That(manifest.Rows.Any(row =>
            row.Suite == "DynamicLinq" &&
            row.Lane == BenchmarkLane.PreParsed.ToString() &&
            row.EvaluatorId == "Alder_DynamicLinq_ParsedPlan"), Is.True);
    }

    [Test]
    public void RunManifest_ExtractsNonDynamicQueryBenchmarks()
    {
        var query = CollectionPipelineBenchmarks.GetPipelineQueries()
            .Single(x => x.Name == "Filter+Count");
        var method = typeof(CollectionPipelineBenchmarks).GetMethod(nameof(CollectionPipelineBenchmarks.Native))!;
        var summary = new FakeSummary(
        [
            new FakeReport(new FakeBenchmarkCase(
                new FakeParameters(
                [
                    new FakeParameter("Query", query),
                    new FakeParameter("ScaleFactor", 1_000)
                ]),
                new FakeDescriptor(typeof(CollectionPipelineBenchmarks), method)))
        ]);

        var manifest = BenchmarkManifestWriter.BuildRunManifest(
            [summary],
            BenchmarkRunProfile.Publish,
            ["--profile", "publish", "--filter", "*CollectionPipeline*"]);

        Assert.That(manifest.Rows, Has.Count.EqualTo(1));
        Assert.That(manifest.Rows[0].Suite, Is.EqualTo("CollectionPipeline"));
        Assert.That(manifest.Rows[0].Category, Is.EqualTo("Operational"));
        Assert.That(manifest.Rows[0].Lane, Is.EqualTo(BenchmarkLane.Warm.ToString()));
        Assert.That(manifest.Rows[0].CaseId, Is.EqualTo("Filter+Count"));
        Assert.That(manifest.Rows[0].EvaluatorId, Is.EqualTo(nameof(CollectionPipelineBenchmarks.Native)));
        Assert.That(manifest.Rows[0].Scale, Is.EqualTo(1_000));
    }

    [Test]
    public void RunManifest_ExtractsParameterOnlyBenchmarks()
    {
        var method = typeof(BusinessRulesBenchmarks).GetMethod(nameof(BusinessRulesBenchmarks.Alder_CompiledFec))!;
        var summary = new FakeSummary(
        [
            new FakeReport(new FakeBenchmarkCase(
                new FakeParameters(
                [
                    new FakeParameter("RuleCount", 25),
                    new FakeParameter("EntityCount", 1_000)
                ]),
                new FakeDescriptor(typeof(BusinessRulesBenchmarks), method)))
        ]);

        var manifest = BenchmarkManifestWriter.BuildRunManifest(
            [summary],
            BenchmarkRunProfile.Publish,
            ["--profile", "publish", "--filter", "*BusinessRules*"]);

        Assert.That(manifest.Rows, Has.Count.EqualTo(1));
        Assert.That(manifest.Rows[0].Suite, Is.EqualTo("BusinessRules"));
        Assert.That(manifest.Rows[0].Category, Is.EqualTo("Operational"));
        Assert.That(manifest.Rows[0].Lane, Is.EqualTo(BenchmarkLane.Warm.ToString()));
        Assert.That(manifest.Rows[0].CaseId, Is.EqualTo("RuleCount=25;EntityCount=1000"));
        Assert.That(manifest.Rows[0].EvaluatorId, Is.EqualTo(nameof(BusinessRulesBenchmarks.Alder_CompiledFec)));
        Assert.That(manifest.Rows[0].Scale, Is.EqualTo(1_000));
    }

    [Test]
    public void BenchmarkClasses_DoNotExposeKnownCrashingFecMethods()
    {
        Assert.That(GetBenchmarkMethodNames<CollectionPipelineBenchmarks>(), Does.Not.Contain("Alder_CompiledFec"));
        Assert.That(GetBenchmarkMethodNames<AdvancedLanguageBenchmarks>(), Does.Not.Contain("Alder_CompiledFec"));
        Assert.That(GetBenchmarkMethodNames<ExtendedSyntaxBenchmarks>(), Does.Not.Contain("Standard_CompiledFec"));
        Assert.That(GetBenchmarkMethodNames<ExtendedSyntaxBenchmarks>(), Does.Not.Contain("Extended_CompiledFec"));
        Assert.That(GetBenchmarkMethodNames<ThroughputBenchmarks>(), Does.Not.Contain("CompiledFec_LINQ"));
        Assert.That(GetBenchmarkMethodNames<CompilationAmortizationBenchmarks>(), Does.Not.Contain("CompiledFec_LINQ"));
    }

    [Test]
    public void BenchmarkParityVerifier_ExtendedScenario_CanSkipFecForValidation()
    {
        var scenario = BenchmarkScenarios.GetExtendedScenarios()
            .Single(x => x.Name == "Extended/ChainedComparison");

        var result = BenchmarkParityVerifier.VerifyExtendedScenario(
            scenario,
            BenchmarkData.CreateStandard(),
            includeFec: false);

        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    [Test]
    public void BenchmarkParityVerifier_CrossEngineScenario_CanSkipFecForValidation()
    {
        var scenario = BenchmarkScenarios.GetCrossEngineScenarios()
            .Single(x => x.Name == "Arithmetic/Constant");

        var result = BenchmarkParityVerifier.VerifyCrossEngineScenario(
            scenario,
            BenchmarkData.CreateStandard(),
            includeFec: false);

        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    [Test]
    public void BenchmarkParityVerifier_DynamicLinqTypedProjectionScenario_Aligns()
    {
        var scenario = BenchmarkScenarios.GetDynamicLinqScenarios()
            .Single(x => x.Name == "DynLINQ/ProjectionTypedDtoFirst");
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        var result = BenchmarkParityVerifier.VerifyDynamicLinqScenario(
            scenario,
            BenchmarkData.CreateStandard());

        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

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

        Assert.That(queryNames, Has.Length.GreaterThanOrEqualTo(20));
        Assert.That(queryNames, Does.Contain("SetOperator+UnionCount"));
        Assert.That(queryNames, Does.Contain("SetOperator+IntersectCount"));
        Assert.That(queryNames, Does.Contain("SetOperator+ExceptCount"));
        Assert.That(queryNames, Does.Contain("Projection+TypedDtoFirst"));
        Assert.That(queryNames, Does.Contain("Sequence+SequenceEqual"));
        Assert.That(queryNames, Does.Contain("SelectMany+FlattenNameChars"));
        Assert.That(queryNames, Does.Contain("GroupBy+CategoryCount"));
        Assert.That(queryNames, Does.Contain("Join+SelfCategoryCount"));
        Assert.That(queryNames, Does.Contain("Paging+SkipTakeElementAt"));
        Assert.That(queryNames, Does.Contain("Aggregate+MinMaxAverage"));
    }

    [Test]
    public void DynamicLinqBenchmarks_DefaultRunMatrix_IsBounded()
    {
        WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Custom.ToString(), () =>
        {
            var warmBenchmarks = new DynamicLinqBenchmarks();
            var parsedBenchmarks = new DynamicLinqParsedLambdaBenchmarks();
            var coldBenchmarks = new DynamicLinqBenchmarksColdStart();

            Assert.That(warmBenchmarks.Queries().Count(), Is.LessThanOrEqualTo(8));
            Assert.That(parsedBenchmarks.Queries().Count(), Is.LessThanOrEqualTo(8));
            Assert.That(coldBenchmarks.Queries().Count(), Is.LessThanOrEqualTo(6));
            Assert.That(warmBenchmarks.ScaleFactors().Count(), Is.LessThanOrEqualTo(2));
            Assert.That(parsedBenchmarks.ScaleFactors().Count(), Is.LessThanOrEqualTo(2));
            Assert.That(coldBenchmarks.ScaleFactors().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public void DynamicLinqBenchmarks_ExhaustiveProfile_RunMatrix_RemainsAvailable()
    {
        WithBenchmarkProfileEnvironment(BenchmarkRunProfile.Exhaustive.ToString(), () =>
        {
            var warmBenchmarks = new DynamicLinqBenchmarks();

            Assert.That(warmBenchmarks.Queries().Count(), Is.EqualTo(DynamicLinqBenchmarks.GetDynamicLinqQueries().Count));
            Assert.That(warmBenchmarks.ScaleFactors().Count(), Is.EqualTo(4));
        });
    }

    [Test]
    public void DynamicLinqBenchmarks_SeparateGenericAndNonGenericComparisonLanes()
    {
        var warmMethods = typeof(DynamicLinqBenchmarks)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(BenchmarkAttribute), inherit: false).Length > 0)
            .Select(method => method.Name)
            .ToArray();
        var coldMethods = typeof(DynamicLinqBenchmarksColdStart)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(BenchmarkAttribute), inherit: false).Length > 0)
            .Select(method => method.Name)
            .ToArray();

        Assert.That(warmMethods, Does.Contain("Alder_DynamicLinq_NonGeneric"));
        Assert.That(warmMethods, Does.Contain("Alder_DynamicLinq_Generic"));
        Assert.That(warmMethods, Does.Contain("SystemDynamicLinqCore_String"));
        Assert.That(coldMethods, Does.Contain("Alder_DynamicLinq_NonGeneric"));
        Assert.That(coldMethods, Does.Contain("Alder_DynamicLinq_Generic"));
        Assert.That(coldMethods, Does.Contain("SystemDynamicLinqCore_String"));
    }

    [Test]
    public void DynamicLinqParsedLambdaBenchmarks_UseDedicatedParsedLambdaLane()
    {
        var parsedLambdaBenchmarkType = typeof(DynamicLinqBenchmarks).Assembly
            .GetType("Alder.Benchmarks.DynamicLinqParsedLambdaBenchmarks");
        Assert.That(parsedLambdaBenchmarkType, Is.Not.Null);

        var parsedMethods = parsedLambdaBenchmarkType!
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(BenchmarkAttribute), inherit: false).Length > 0)
            .Select(method => method.Name)
            .ToArray();

        Assert.That(parsedMethods, Does.Contain("Native"));
        Assert.That(parsedMethods, Does.Contain("Alder_DynamicLinq_ParsedLambda"));
        Assert.That(parsedMethods, Does.Contain("SystemDynamicLinqCore_ParsedLambda"));
    }

    [Test]
    public void DynamicLinqParsedLambdaBenchmarks_ExcludeNativeExpressionFallbacks()
    {
        var parsedQueries = DynamicLinqBenchmarks.GetParsedLambdaQueries()
            .Select(x => x.Name)
            .ToArray();

        Assert.That(parsedQueries, Is.Not.Empty);
        Assert.That(parsedQueries, Does.Not.Contain("Projection+AnonymousMaterialization"));
        Assert.That(parsedQueries, Does.Not.Contain("Projection+TypedDtoFirst"));
        Assert.That(parsedQueries, Has.Length.LessThan(DynamicLinqBenchmarks.GetDynamicLinqQueries().Count));
    }

    [Test]
    public void DynamicLinqBenchmarks_AllComparisonLanes_AreSemanticallyAligned()
    {
        var data = BenchmarkData.Create(productCount: 256);
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var products = data.Products.AsQueryable();

        foreach (var query in DynamicLinqBenchmarks.GetDynamicLinqQueries())
        {
            var expected = query.Native(products);

            Assert.That(AreEquivalent(expected, query.AlderNonGeneric(products, engine)),
                Is.True,
                query.Name + " Alder non-generic");
            Assert.That(AreEquivalent(expected, query.AlderGeneric(products, engine)),
                Is.True,
                query.Name + " Alder generic");
            Assert.That(AreEquivalent(expected, query.DynamicCoreString(products)),
                Is.True,
                query.Name + " System.Linq.Dynamic.Core string");
        }

        foreach (var query in DynamicLinqBenchmarks.GetParsedLambdaQueries())
        {
            var expected = query.Native(products);

            Assert.That(AreEquivalent(expected, query.AlderParsedLambda!(engine)(products)),
                Is.True,
                query.Name + " Alder parsed lambda");
            Assert.That(AreEquivalent(expected, query.DynamicCoreParsedLambda!()(products)),
                Is.True,
                query.Name + " System.Linq.Dynamic.Core parsed lambda");
        }
    }

    private static bool AreEquivalent(object? expected, object? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        if (expected is decimal expectedDecimal && actual is decimal actualDecimal)
            return Math.Abs(expectedDecimal - actualDecimal) < 0.0001m;

        if (expected is double expectedDouble && actual is double actualDouble)
            return Math.Abs(expectedDouble - actualDouble) < 0.0001d;

        return Equals(expected, actual);
    }

    private static string[] GetBenchmarkMethodNames<T>() =>
        typeof(T).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(BenchmarkAttribute), inherit: false).Length > 0)
            .Select(method => method.Name)
            .ToArray();

    private static void WithBenchmarkProfileEnvironment(string? value, Action action)
    {
        var previousProfile = Environment.GetEnvironmentVariable(BenchmarkProfileContext.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(BenchmarkProfileContext.EnvironmentVariable, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(BenchmarkProfileContext.EnvironmentVariable, previousProfile);
        }
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

    private sealed class FakeSummary(IReadOnlyList<FakeReport> reports)
    {
        public IReadOnlyList<FakeReport> Reports { get; } = reports;
    }

    private sealed class FakeReport(FakeBenchmarkCase benchmarkCase)
    {
        public FakeBenchmarkCase BenchmarkCase { get; } = benchmarkCase;
    }

    private sealed class FakeBenchmarkCase(FakeParameters parameters, FakeDescriptor descriptor)
    {
        public FakeParameters Parameters { get; } = parameters;
        public FakeDescriptor Descriptor { get; } = descriptor;
    }

    private sealed class FakeParameters(IReadOnlyList<FakeParameter> parameters) : List<FakeParameter>(parameters);

    private sealed class FakeParameter(string name, object? value)
    {
        public string Name { get; } = name;
        public object? Value { get; } = value;
    }

    private sealed class FakeDescriptor(Type type, System.Reflection.MethodInfo workloadMethod)
    {
        public Type Type { get; } = type;
        public System.Reflection.MethodInfo WorkloadMethod { get; } = workloadMethod;
    }
}
