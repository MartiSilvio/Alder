# CsEval Benchmarks

Performance benchmarks for CsEval using [BenchmarkDotNet](https://benchmarkdotnet.org/).

For full documentation, see [docs/benchmarks.md](../docs/benchmarks.md).

## Quick Start

```bash
dotnet run -c Release
```

**Important**: Always run in Release mode for accurate results.

## Available Suites

- `StandardBenchmarks` - Classic language benchmarks (Fibonacci, Collatz, etc.)
- `CsEvalBenchmarks` - Parse/evaluate timing
- `LinqBenchmarks` - LINQ operations at different sizes
- `PropertyAccessBenchmarks` - Compiled getter performance
- `BlockExpressionBenchmarks` - Control flow (loops, conditionals)

## Examples

```bash
# All benchmarks
dotnet run -c Release -- --filter *

# Specific suite
dotnet run -c Release -- --filter *StandardBenchmarks*

# Specific benchmark
dotnet run -c Release -- --filter *Fibonacci*

# Export results
dotnet run -c Release -- --filter * --exporters json
```
