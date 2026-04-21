# Alder Benchmark Suite

This suite uses a unified **Case × Evaluator × Lane** architecture for cross-engine claims.
`DynamicLinqBenchmarks` remains specialized but aligns to the same taxonomy and manifest contract.

Every benchmark still belongs to one of three categories:

- `HeadToHead/*`: apples-to-apples comparisons on workloads that all compared engines can express and evaluate with semantic parity.
- `Capability/*`: Alder-only or Alder-vs-Roslyn scenarios that demonstrate product value outside the common denominator.
- `Operational/*`: production-shaped costs such as cold start, compilation amortization, reusable business-rule execution, dynamic query pipelines, and concurrent throughput.

## Core Architecture

- `BenchmarkCase`: semantic workload with expected native truth and per-engine expression payloads.
- `IBenchmarkEvaluator`: engine adapter with capability declaration and lane-specific execution semantics.
- `BenchmarkLane`: `PreParsed`, `Warm`, `Cold`.
- `BenchmarkMatrixRow`: normalized row (`CaseId`, `EvaluatorId`, `Lane`, `Scale`, capability).
- `MatrixCatalogBuilder`: deterministic row generation.
- Capability resolution is explicit for every case/evaluator pair. Unsupported syntax is emitted as `N/A` with reason code and mapped to native-equivalent baseline context.
- `PreParsedRunner` / `WarmRunner` / `ColdRunner`: enforce lane semantics.
- `ParityRunner`: validates semantic equivalence before claims.
- `BenchmarkManifestWriter`: emits JSON manifest with required tags (`Suite`, `Category`, `Lane`, `CaseId`, `EvaluatorId`, `Scale`).

## What This Suite Does Not Claim

- A `Capability/*` benchmark is not evidence that Alder is faster than simpler libraries outside that capability.
- An `Operational/*` benchmark is not a parser microbenchmark. It measures a production unit of work.
- A `HeadToHead/*` benchmark is only valid when parity checks pass for every compared engine.

## Reproducibility Rules

- Run on a quiet machine with release builds only.
- Record CPU model, core count, OS version, .NET SDK/runtime version, and power profile with published results.
- Treat `BDN_QUICK=1` as local developer smoke mode only. Do not use quick-mode results in published claims.
- Run `--smoke-validate` before performance runs and publish any exclusions.

## Minimum Validation Before Citing Results

1. `dotnet test benchmarks/Alder.Benchmarks.Tests/Alder.Benchmarks.Tests.csproj`
2. `dotnet run --project benchmarks/Alder.Benchmarks/Alder.Benchmarks.csproj -- --smoke-validate`
3. `dotnet run --project benchmarks/Alder.Benchmarks/Alder.Benchmarks.csproj -- --smoke-fec`
4. Run the target BenchmarkDotNet command on a clean machine and archive the Markdown, HTML, and CSV artifacts.

## Why Some Benchmarks Were Removed

The suite intentionally excludes diagnostic microbenchmarks that are easy to attack as synthetic or not representative of product value. If a benchmark does not support a public claim under external scrutiny, it does not belong in this suite.
