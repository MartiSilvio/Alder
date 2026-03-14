using NUnit.Framework;

namespace CsEval.Benchmarks.Tests;

[TestFixture]
public class BenchmarkSuiteDesignTests
{
    [Test]
    public void ComparableExecutionScenarios_AreBroadEnough_ForMeaningfulResults()
    {
        var scenarios = BenchmarkScenarioCatalog.GetComparableExecutionScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(6),
            "Need at least 6 comparable scenarios to avoid overfitting to trivial expressions.");
    }

    [Test]
    public void AdvancedLanguageScenarios_AreBroadEnough_ForRepresentativeCoverage()
    {
        var scenarios = BenchmarkScenarioCatalog.GetAdvancedLanguageScenarios();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(4),
            "Need at least 4 advanced scenarios to represent realistic CsEval usage.");
    }

    [Test]
    public void ScenarioNames_AreUniqueWithinEachSuite()
    {
        var comparableNames = BenchmarkScenarioCatalog.GetComparableExecutionScenarios().Select(x => x.Name).ToList();
        var advancedNames = BenchmarkScenarioCatalog.GetAdvancedLanguageScenarios().Select(x => x.Name).ToList();
        var extendedNames = BenchmarkScenarioCatalog.GetExtendedParityScenarios().Select(x => x.Name).ToList();

        Assert.That(comparableNames, Is.Unique, "Comparable scenario names must be unique.");
        Assert.That(advancedNames, Is.Unique, "Advanced scenario names must be unique.");
        Assert.That(extendedNames, Is.Unique, "Extended parity scenario names must be unique.");
    }

    [Test]
    public void ExtendedParityScenarios_IncludeAliasOperators_ForHotPathCoverage()
    {
        var scenarios = BenchmarkScenarioCatalog.GetExtendedParityScenarios();
        var names = scenarios.Select(x => x.Name).ToList();

        Assert.That(scenarios, Has.Count.GreaterThanOrEqualTo(8),
            "Need at least 8 extended parity scenarios to cover aliases and symbols.");
        Assert.That(names, Does.Contain("ExtendedParity/NotInAlias"),
            "Extended parity must benchmark the 'not in' alias hot path.");
        Assert.That(names, Does.Contain("ExtendedParity/NotLikeAlias"),
            "Extended parity must benchmark the 'not like' alias hot path.");
    }

    [Test]
    public void ComparableScenarios_StaySemanticallyAlignedAcrossEngines()
    {
        var globals = BenchmarkGlobalData.CreateDefault();

        foreach (var scenario in BenchmarkScenarioCatalog.GetComparableExecutionScenarios())
        {
            var result = BenchmarkParityVerifier.VerifyComparableScenario(scenario, globals);
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
    }

    [Test]
    public void AdvancedScenarios_StaySemanticallyAlignedBetweenCsEvalAndRoslyn()
    {
        var globals = BenchmarkGlobalData.CreateDefault();

        foreach (var scenario in BenchmarkScenarioCatalog.GetAdvancedLanguageScenarios())
        {
            var result = BenchmarkParityVerifier.VerifyAdvancedScenario(scenario, globals);
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
    }
}
