# Alder Benchmarks

Performance benchmarks for Alder using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Quick Start

```bash
dotnet run -c Release
```

Always run in `Release` mode for meaningful measurements.

## Available Suites

- **ComparableExecutionBenchmarks** — Warm execution of pre-prepared expressions across Alder interpreted, Alder compiled, Roslyn script compiled runner, NCalc, and native delegate baseline.
- **ColdStartComparableBenchmarks** — End-to-end cold path (engine creation + evaluation per invocation) across Alder interpreted, Alder compiled, Roslyn scripting, and NCalc.
- **AdvancedLanguageBenchmarks** — Realistic control flow and LINQ-heavy expressions. Alder interpreted vs Alder compiled vs Roslyn script compiled runner.

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
- Benchmarks use fixed deterministic input data.
- Cross-engine comparisons are limited to capability-overlap scenarios.
- Advanced suites are Alder-vs-Roslyn only where NCalc lacks feature parity.
