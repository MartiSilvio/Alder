using Alder.Compiled;

namespace Alder.Benchmarks;

public static class BenchmarkSmokeValidator
{
    public static int Run()
    {
        var globals = BenchmarkGlobalData.CreateDefault();
        var failures = new List<string>();

        foreach (var scenario in BenchmarkScenarioCatalog.GetComparableExecutionScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyComparableScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        foreach (var scenario in BenchmarkScenarioCatalog.GetAdvancedLanguageScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyAdvancedScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        foreach (var scenario in BenchmarkScenarioCatalog.GetExtendedParityScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyExtendedParityScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        foreach (var scenario in BenchmarkScenarioCatalog.GetLinqScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyLinqScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        foreach (var scenario in BenchmarkScenarioCatalog.GetDynamicLinqScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyDynamicLinqScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        foreach (var scenario in BenchmarkScenarioCatalog.GetCompilationScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyCompilationScenario(scenario, globals);
            if (!parity.IsSuccess)
                failures.Add(parity.Message);
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("Benchmark smoke validation passed for comparable, advanced, extended parity, LINQ, and compilation scenarios.");
            return 0;
        }

        Console.WriteLine("Benchmark smoke validation failed:");
        foreach (var failure in failures)
            Console.WriteLine($" - {failure}");
        return 1;
    }
}
