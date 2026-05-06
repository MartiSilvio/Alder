using Alder.Compiled;

namespace Alder.Benchmarks;

public static class BenchmarkSmokeValidator
{
    public static IReadOnlyList<BenchmarkMatrixRow> BuildValidationRows(BenchmarkLane lane) =>
        MatrixCatalogBuilder.BuildSupportedRows(lane);

    public static int Run()
    {
        var data = BenchmarkData.CreateStandard();
        var failures = new List<string>();

        Console.WriteLine("=== Cross-engine scenarios ===");
        foreach (var scenario in BenchmarkScenarios.GetCrossEngineScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyCrossEngineScenario(scenario, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {scenario.Name}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Cross-engine matrix parity (pre-parsed) ===");
        var preParsedRows = BuildValidationRows(BenchmarkLane.PreParsed);
        var preParsedUnsupported = MatrixCatalogBuilder.BuildUnsupportedRows(BenchmarkLane.PreParsed);
        foreach (var row in preParsedUnsupported)
            Console.WriteLine($"  N/A: {row.CaseId}/{row.EvaluatorId} ({row.Capability.ReasonCode})");
        foreach (var row in preParsedRows)
        {
            var parity = ParityRunner.VerifyRow(row, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {row}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Cross-engine matrix parity (warm) ===");
        var warmRows = BuildValidationRows(BenchmarkLane.Warm);
        var warmUnsupported = MatrixCatalogBuilder.BuildUnsupportedRows(BenchmarkLane.Warm);
        foreach (var row in warmUnsupported)
            Console.WriteLine($"  N/A: {row.CaseId}/{row.EvaluatorId} ({row.Capability.ReasonCode})");
        foreach (var row in warmRows)
        {
            var parity = ParityRunner.VerifyRow(row, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {row}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Cross-engine matrix parity (cold) ===");
        var coldRows = BuildValidationRows(BenchmarkLane.Cold);
        var coldUnsupported = MatrixCatalogBuilder.BuildUnsupportedRows(BenchmarkLane.Cold);
        foreach (var row in coldUnsupported)
            Console.WriteLine($"  N/A: {row.CaseId}/{row.EvaluatorId} ({row.Capability.ReasonCode})");
        foreach (var row in coldRows)
        {
            var parity = ParityRunner.VerifyRow(row, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {row}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Advanced language scenarios ===");
        foreach (var scenario in BenchmarkScenarios.GetAdvancedScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyAlderScenario(scenario, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {scenario.Name}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== LINQ scenarios ===");
        foreach (var scenario in BenchmarkScenarios.GetLinqScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyAlderScenario(scenario, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {scenario.Name}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Extended syntax scenarios ===");
        foreach (var scenario in BenchmarkScenarios.GetExtendedScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyExtendedScenario(scenario, data, includeFec: false);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {scenario.Name}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Dynamic LINQ scenarios ===");
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());
        foreach (var scenario in BenchmarkScenarios.GetDynamicLinqScenarios())
        {
            var parity = BenchmarkParityVerifier.VerifyDynamicLinqScenario(scenario, data);
            Console.WriteLine(parity.IsSuccess ? $"  PASS: {scenario.Name}" : $"  FAIL: {parity.Message}");
            if (!parity.IsSuccess) failures.Add(parity.Message);
        }

        Console.WriteLine("\n=== Typed delegate compilation ===");
        var typedDelegateChecks = new (string Name, Func<ParityResult> Check)[]
        {
            ("TypedDelegate/Func2Params", () =>
            {
                using var engine = new AlderEngine(new AlderOptions().UseCompiler());
                var fn = engine.Compile<Func<int, int, int>>("a + b", "a", "b");
                var result = fn(data.X, data.Y);
                var expected = data.X + data.Y;
                return result == expected
                    ? new ParityResult(true, "TypedDelegate/Func2Params: parity OK")
                    : new ParityResult(false, $"TypedDelegate/Func2Params: expected {expected}, got {result}");
            }),
            ("TypedDelegate/BusinessRule", () =>
            {
                using var engine = new AlderEngine(new AlderOptions().UseCompiler());
                var fn = engine.Compile<Func<Product, bool>>(
                    "p.Price > 100m && p.IsActive", "p");
                var product = data.Products[0];
                var result = fn(product);
                var expected = product.Price > 100m && product.IsActive;
                return result == expected
                    ? new ParityResult(true, "TypedDelegate/BusinessRule: parity OK")
                    : new ParityResult(false, $"TypedDelegate/BusinessRule: expected {expected}, got {result}");
            }),
            ("TypedDelegate/MultiStatement", () =>
            {
                using var engine = new AlderEngine(new AlderOptions().UseCompiler());
                var fn = engine.Compile<Func<int, string>>("""
                    if (n > 0) return "positive";
                    else if (n < 0) return "negative";
                    else return "zero";
                    """, "n");
                if (fn(5) != "positive") return new ParityResult(false, "TypedDelegate/MultiStatement: 5 != positive");
                if (fn(-1) != "negative") return new ParityResult(false, "TypedDelegate/MultiStatement: -1 != negative");
                if (fn(0) != "zero") return new ParityResult(false, "TypedDelegate/MultiStatement: 0 != zero");
                return new ParityResult(true, "TypedDelegate/MultiStatement: parity OK");
            }),
        };
        foreach (var (name, check) in typedDelegateChecks)
        {
            try
            {
                var parity = check();
                Console.WriteLine(parity.IsSuccess ? $"  PASS: {name}" : $"  FAIL: {parity.Message}");
                if (!parity.IsSuccess) failures.Add(parity.Message);
            }
            catch (Exception ex)
            {
                var msg = $"{name}: {ex.GetType().Name} - {ex.Message}";
                Console.WriteLine($"  FAIL: {msg}");
                failures.Add(msg);
            }
        }

        Console.WriteLine();
        if (failures.Count == 0)
        {
            Console.WriteLine("All benchmark parity checks passed.");
            return 0;
        }

        Console.WriteLine($"{failures.Count} parity failure(s):");
        foreach (var failure in failures)
            Console.WriteLine($"  - {failure}");
        return 1;
    }

}
