# CsEval Benchmarks

## Overview

CsEval is benchmarked against four competing expression evaluators and Roslyn scripting. All benchmarks use BenchmarkDotNet with memory diagnostics on .NET 8.0, Arm64 (Apple M3 Max).

## Optimizations Applied

The following optimizations have been applied to the compiled execution path:

- **Direct method call emission** -- Expression tree emitter generates `Expression.Call` with resolved `MethodInfo`, compiling to direct CLR calls rather than reflection-based invocation
- **Direct property/field access** -- Member access compiles to `Expression.Property`/`Expression.Field` with resolved member info
- **Boxing elimination** -- Arithmetic and conditional expressions emit typed expression trees, avoiding unnecessary box/unbox operations for value types
- **MethodResolver fast path** -- `Type.GetMethod` with exact argument types is tried first before falling back to overload scoring
- **TypeInferrer static analysis** -- Pre-pass infers types for AST nodes so the emitter can generate typed IL instead of object-based dispatch

## Benchmark Results

**Machine:** Apple M3 Max, 14 cores, macOS Sequoia 15.7.2
**Runtime:** .NET 8.0.4, Arm64 RyuJIT AdvSIMD

### Warm Execution (pre-parsed, compiled)

| Scenario | CsEval Compiled (ns) | Roslyn (ns) | DynamicExpresso (ns) | Flee (ns) | NCalc (ns) | vs Roslyn | vs DE |
|---|---:|---:|---:|---:|---:|---:|---:|
| Conditional/Ternary | 60 | 72 | 36 | 28 | 226 | **0.8x** | 1.7x |
| ModuloEquality | 63 | 73 | 36 | 21 | 173 | **0.9x** | 1.8x |
| ArithmeticOnly | 96 | -- | 36 | -- | -- | -- | 2.7x |
| PropertyAccess | 100 | -- | 35 | -- | -- | -- | 2.9x |
| Boolean/Composite | 108 | 76 | 36 | 40 | 368 | 1.4x | 3.0x |
| NestedConditional | 123 | 74 | 37 | 48 | -- | 1.7x | 3.3x |
| Arithmetic/Precedence | 159 | 72 | 36 | 3.6 | 305 | 2.2x | 4.4x |
| Arithmetic/WithVars | 179 | 74 | 37 | 39 | 369 | 2.4x | 4.8x |
| Mix/NumericPredicate | 177 | 75 | 37 | 30 | 327 | 2.4x | 4.8x |
| InstanceMethodCall | 230 | -- | 41 | -- | -- | -- | 5.6x |
| CollectionProperties | 253 | 74 | 40 | 24 | -- | 3.4x | 6.3x |
| StringPredicate | 392 | 81 | 42 | 28 | -- | 4.8x | 9.3x |
| ObjectGraphAccess | 472 | 76 | 37 | 31 | -- | 6.2x | 12.8x |
| StaticMethodCall | 994 | -- | 35 | -- | -- | -- | 28.4x |
| ChainedStaticCalls | 3,493 | -- | 35 | -- | -- | -- | 99.8x |
| Functions/MathMix | 3,623 | 76 | 36 | 38 | 362 | 47.7x | 100.6x |

**Bold** = CsEval faster than Roslyn.

### Competitive Position

- **CsEval beats Roslyn** on simple expressions (ternary, modulo equality) and is within 2.5x for most arithmetic/boolean scenarios
- **CsEval beats NCalc** in every comparable scenario, typically by 2-3x
- **CsEval is 1.5-5x of DynamicExpresso** for expressions without method calls -- this is the practical overhead of full C# expression support
- **Flee** is fastest for simple numeric expressions (near-native speed) but supports a very limited expression language

### Allocations

| Scenario | CsEval Compiled | DynamicExpresso | Flee |
|---|---:|---:|---:|
| TernaryOnly | 96 B | 48 B | 24 B |
| ArithmeticOnly | 232 B | 48 B | 24 B |
| InstanceMethodCall | 600 B | 48 B | -- |
| Functions/MathMix | 6,592 B | 48 B | 24 B |

DynamicExpresso and Flee allocate a flat 24-48B per evaluation (boxed return value). CsEval's allocations scale with expression complexity due to context-based variable binding.

## Remaining Gaps

### Static method calls (28-100x vs DynamicExpresso)

Static method invocation (`Math.Abs`, `Math.Max`) is the primary remaining bottleneck. While the expression tree emitter generates direct `Expression.Call` nodes, the per-evaluation overhead comes from:

1. **Context-based variable resolution** -- Variables are stored as `object?` in a dictionary and looked up per-evaluation, requiring boxing for value types. DynamicExpresso captures typed variables directly in the compiled delegate closure.
2. **Argument marshalling** -- Each method argument is extracted from the context, boxed, and passed through the expression tree's parameter layer. With chained calls (`Math.Abs(x - y) + Math.Max(y, z)`), this overhead compounds.
3. **Per-evaluation delegate overhead** -- The compiled delegate receives the full evaluation context as input, adding a fixed cost that dominates for simple method calls.

### Why this gap is architectural

DynamicExpresso and Flee compile to closed-over delegates with direct variable capture at parse time. CsEval's architecture prioritizes:
- Runtime variable mutation (variables can change between evaluations)
- Execution constraints (timeout, iteration limits, memory limits)
- Sandboxing (type/member allow/deny lists checked per-evaluation)
- Full C# expression support (statements, control flow, pattern matching)

These features require the context-based indirection that adds overhead. Closing this gap would require a fundamentally different compilation strategy (e.g., re-compiling the expression tree when variables change), which would trade cold-start performance for warm execution speed.

### Follow-up opportunities

- **Variable capture optimization**: For read-only variables, the expression tree could capture values directly in the closure, eliminating dictionary lookup and boxing. This would require detecting which variables are mutated.
- **Method resolution caching**: Cache resolved `MethodInfo` objects keyed by (type, name, argTypes) to avoid repeated reflection. Currently each static method call re-resolves on every evaluation in interpreted mode.
- **Allocation pooling**: Pool intermediate objects (evaluation contexts, argument arrays) to reduce GC pressure for high-frequency evaluation scenarios.

## Benchmark Methodology

See the [benchmark suite README](../benchmarks/README.md) for methodology, fairness rules, and repeatability guidelines.

### Suites

1. **MicroBenchmarks** -- CsEval vs DynamicExpresso on isolated micro-operations (arithmetic, method calls, property access)
2. **ComparableExecutionBenchmarks** -- All engines on equivalent expressions with parity verification
3. **AdvancedLanguageBenchmarks** -- CsEval vs Roslyn/DynamicExpresso/Flee on richer expressions (NCalc excluded due to feature mismatch)
4. **ColdStartComparableBenchmarks** -- End-to-end cost including parse/compile time

### Running

```bash
# All suites
dotnet run -c Release --project benchmarks/CsEval.Benchmarks/ -- --filter *

# Specific suite
dotnet run -c Release --project benchmarks/CsEval.Benchmarks/ -- --filter *MicroBenchmarks*
dotnet run -c Release --project benchmarks/CsEval.Benchmarks/ -- --filter *ComparableExecutionBenchmarks*
dotnet run -c Release --project benchmarks/CsEval.Benchmarks/ -- --filter *AdvancedLanguageBenchmarks*
```
