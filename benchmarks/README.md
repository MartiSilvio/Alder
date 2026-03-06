# CsEval Benchmarks

Performance benchmarks for CsEval using [BenchmarkDotNet](https://benchmarkdotnet.org/).

For full documentation, see [docs/benchmarks.md](../docs/benchmarks.md).

## Quick Start

```bash
dotnet run -c Release
```

Always run in `Release` mode for meaningful measurements.

## Available Suites

- `ComparableExecutionBenchmarks`
  - Warm execution of pre-prepared expressions.
  - Compared engines: CsEval interpreted, CsEval compiled, Roslyn script compiled runner, NCalc, and native delegate baseline.
- `ColdStartComparableBenchmarks`
  - End-to-end cold path (engine/script/expression creation + execute each invocation).
  - Compared engines: CsEval interpreted, CsEval compiled, Roslyn scripting `EvaluateAsync`, NCalc.
- `AdvancedLanguageBenchmarks`
  - Realistic control flow and LINQ-heavy expressions where NCalc is not feature-compatible.
  - Compared engines: CsEval interpreted, CsEval compiled, Roslyn script compiled runner.

## Examples

```bash
# All benchmarks
dotnet run -c Release -- --filter *

# Warm comparable suite
dotnet run -c Release -- --filter *ComparableExecutionBenchmarks*

# Cold-start only
dotnet run -c Release -- --filter *ColdStartComparableBenchmarks*

# Export markdown + json
dotnet run -c Release -- --filter * --exporters markdown,json
```

## Guardrails

- Scenario parity is validated during benchmark setup.
- Benchmarks use fixed deterministic input data (`BenchmarkGlobalData`).
- Cross-engine comparisons are limited to capability-overlap scenarios.
- Advanced suites are explicitly CsEval-vs-Roslyn only to avoid misleading comparisons.
