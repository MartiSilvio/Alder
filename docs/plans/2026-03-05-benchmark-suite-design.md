# Benchmark Suite Reliability Overhaul Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Turn `benchmarks/CsEval.Benchmarks` into a reliable, repeatable benchmark suite with fair cross-library comparisons and realistic scenario coverage.

**Architecture:** Use scenario catalogs plus shared benchmark global data so every suite is declarative and maintainable. Add setup-time parity verification to enforce semantic alignment before timing. Split benchmarks into capability-overlap comparisons and advanced-feature comparisons to avoid misleading claims.

**Tech Stack:** .NET 8, BenchmarkDotNet, Roslyn scripting, NCalc, CsEval.

---

## Implemented Tasks Summary

1. Added benchmark scenario model and catalogs:
- `ComparableScenario` for overlap-based cross-library cases.
- `AdvancedScenario` for CsEval/Roslyn-only realistic language scenarios.

2. Added deterministic input model:
- `BenchmarkGlobalData` now centralizes all values and collections used across suites.

3. Added fairness guard:
- `BenchmarkParityVerifier` validates expected semantic parity in setup and fails fast on mismatch.

4. Replaced old repetitive benchmark classes with parameterized suites:
- `ComparableExecutionBenchmarks`
- `ColdStartComparableBenchmarks`
- `AdvancedLanguageBenchmarks`

5. Added suite-level benchmark configuration:
- `BenchmarkSuiteConfig` provides consistent runtime/job/memory/reporting defaults.

6. Added test coverage for benchmark design contract:
- `BenchmarkSuiteDesignTests` checks coverage thresholds, uniqueness, and parity.

7. Updated benchmark documentation:
- `benchmarks/README.md`
- `docs/benchmarks.md`
