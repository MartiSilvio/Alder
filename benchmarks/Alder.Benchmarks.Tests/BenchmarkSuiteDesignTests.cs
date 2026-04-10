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

    private static readonly string[] RejectedBenchmarkTypes =
    [
        "AsyncEvaluationBenchmarks",
        "ExpressionComplexityBenchmarks",
        "ExpressionTreeScalingBenchmarks",
        "ParsingBenchmarks",
        "SingleRuleBenchmarks",
        "TypedDelegateScalingBenchmarks",
        "VariableScalingBenchmarks"
    ];

    [Test]
    public void CompetitorScenarios_AreBroadEnough_ForMeaningfulResults()
    {
        var scenarios = BenchmarkScenarios.GetCompetitorScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(6),
            "Need at least 6 competitor scenarios to avoid overfitting to trivial expressions.");
    }

    [Test]
    public void AdvancedLanguageScenarios_AreBroadEnough_ForRepresentativeCoverage()
    {
        var scenarios = BenchmarkScenarios.GetAdvancedScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(8),
            "Need at least 8 advanced scenarios covering method chains, switch, null-conditional, LINQ, tuples.");
    }

    [Test]
    public void AdvancedScenarios_CoverKeyFeatures()
    {
        var names = BenchmarkScenarios.GetAdvancedScenarios().Select(x => x.Name).ToList();

        Assert.That(names, Does.Contain("Advanced/SwitchExpression"),
            "Advanced scenarios must benchmark switch expressions.");
        Assert.That(names, Does.Contain("Advanced/NullConditional"),
            "Advanced scenarios must benchmark null-conditional operators.");
        Assert.That(names, Does.Contain("Advanced/StringInterpolation"),
            "Advanced scenarios must benchmark string interpolation.");
        Assert.That(names, Does.Contain("Advanced/LinqAggregate"),
            "Advanced scenarios must benchmark LINQ aggregate pipelines.");
    }

    [Test]
    public void ScenarioNames_AreUniqueWithinEachSuite()
    {
        var competitorNames = BenchmarkScenarios.GetCompetitorScenarios().Select(x => x.Name).ToList();
        var advancedNames = BenchmarkScenarios.GetAdvancedScenarios().Select(x => x.Name).ToList();
        var extendedNames = BenchmarkScenarios.GetExtendedScenarios().Select(x => x.Name).ToList();
        var linqNames = BenchmarkScenarios.GetLinqScenarios().Select(x => x.Name).ToList();
        var dynLinqNames = BenchmarkScenarios.GetDynamicLinqScenarios().Select(x => x.Name).ToList();

        Assert.That(competitorNames, Is.Unique, "Competitor scenario names must be unique.");
        Assert.That(advancedNames, Is.Unique, "Advanced scenario names must be unique.");
        Assert.That(extendedNames, Is.Unique, "Extended scenario names must be unique.");
        Assert.That(linqNames, Is.Unique, "LINQ scenario names must be unique.");
        Assert.That(dynLinqNames, Is.Unique, "Dynamic LINQ scenario names must be unique.");
    }

    [Test]
    public void ExtendedScenarios_CoverKeyFeatures()
    {
        var scenarios = BenchmarkScenarios.GetExtendedScenarios();
        var names = scenarios.Select(x => x.Name).ToList();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(8),
            "Need at least 8 extended scenarios to cover aliases and symbols.");
        Assert.That(names, Does.Contain("Extended/NotIn"),
            "Extended scenarios must benchmark the 'not in' operator.");
        Assert.That(names, Does.Contain("Extended/PowerOperator"),
            "Extended scenarios must benchmark the '**' power operator.");
    }

    [Test]
    public void CompetitorScenarios_StaySemanticallyAlignedAcrossEngines()
    {
        var data = BenchmarkData.CreateStandard();

        foreach (var scenario in BenchmarkScenarios.GetCompetitorScenarios())
        {
            var result = BenchmarkParityVerifier.VerifyCompetitorScenario(scenario, data);
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
    }

    [Test]
    public void AdvancedScenarios_StaySemanticallyAlignedBetweenAlderAndRoslyn()
    {
        var data = BenchmarkData.CreateStandard();

        foreach (var scenario in BenchmarkScenarios.GetAdvancedScenarios())
        {
            var result = BenchmarkParityVerifier.VerifyAlderScenario(scenario, data);
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
    }

    [Test]
    public void TypedDelegateCompilation_ProducesCorrectResults()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var fn = engine.Compile<Func<int, int, int>>("a + b", "a", "b");

        Assert.That(fn(3, 7), Is.EqualTo(10));
        Assert.That(fn(100, -50), Is.EqualTo(50));
    }

    [Test]
    public void TypedDelegateCompilation_BusinessRule_MatchesNative()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var fn = engine.Compile<Func<Product, bool>>(
            "p.Price > 100m && p.IsActive", "p");

        var products = BenchmarkData.CreateProducts(100);
        var nativeCount = products.Count(p => p.Price > 100m && p.IsActive);
        var typedCount = products.Count(p => fn(p));

        Assert.That(typedCount, Is.EqualTo(nativeCount));
    }

    [Test]
    public void ExpressionTree_ParseAsExpression_ProducesValidTree()
    {
        using var engine = new AlderEngine(new AlderOptions().UseCompiler());
        var expr = engine.ParseAsExpression<Func<int, bool>>("x => x > 5");

        Assert.That(expr, Is.Not.Null);
        var compiled = expr.Compile();
        Assert.That(compiled(10), Is.True);
        Assert.That(compiled(3), Is.False);
    }

    [Test]
    public void PublicBenchmarkSuite_UsesApprovedCategoryTaxonomy()
    {
        var benchmarkAssembly = typeof(CompetitorBenchmarks).Assembly;
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
    public void PublicBenchmarkSuite_DoesNotIncludeRejectedDiagnosticBenchmarks()
    {
        var benchmarkAssembly = typeof(CompetitorBenchmarks).Assembly;
        var benchmarkTypeNames = benchmarkAssembly.GetTypes().Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var typeName in RejectedBenchmarkTypes)
            Assert.That(benchmarkTypeNames.Contains(typeName), Is.False,
                $"{typeName} is a diagnostic microbenchmark and should not be part of the public benchmark suite.");
    }

    [Test]
    public void ColdStartBenchmarks_DoNotUseGlobalSetup()
    {
        var setupMethods = typeof(ColdStartBenchmarks)
            .GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(GlobalSetupAttribute), inherit: false).Length > 0)
            .ToArray();

        Assert.That(setupMethods, Is.Empty,
            "Cold-start benchmarks must not use GlobalSetup because it pre-heats the benchmark process.");
    }
}
