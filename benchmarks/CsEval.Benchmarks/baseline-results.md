# Benchmark Results

**Date:** 2026-03-05
**Machine:** Apple M3 Max, 14 cores, macOS Sequoia 15.7.2
**Runtime:** .NET 8.0.4, Arm64 RyuJIT AdvSIMD
**Settings:** WarmupCount=1, IterationCount=3

## Micro-Benchmarks: Before/After Comparison

| Scenario | Baseline Compiled (ns) | After Compiled (ns) | DE (ns) | Speedup | After/DE | Baseline Alloc | After Alloc | DE Alloc |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ArithmeticOnly | 98 | 96 | 36 | 1.0x | 2.7x | 232 B | 232 B | 48 B |
| ChainedStaticCalls | 3,692 | 3,493 | 35 | 1.1x | 99.8x | 6,592 B | 6,592 B | 48 B |
| InstanceMethodCall | 606 | 230 | 41 | 2.6x | 5.6x | 1,448 B | 600 B | 48 B |
| PropertyAccess | 95 | 100 | 35 | 1.0x | 2.9x | 176 B | 176 B | 48 B |
| StaticMethodCall | 997 | 994 | 35 | 1.0x | 28.4x | 2,392 B | 2,392 B | 48 B |
| TernaryOnly | 56 | 54 | 37 | 1.0x | 1.5x | 96 B | 96 B | 48 B |

### Changes from Baseline

- **InstanceMethodCall improved 2.6x** (606ns -> 230ns) with allocation reduced from 1,448B to 600B
- **ChainedStaticCalls improved ~5%** (3,692ns -> 3,493ns) -- marginal improvement; static method resolution overhead dominates
- Other scenarios are within measurement noise of baseline

## Comparable Execution Benchmarks: Before/After Comparison

| Scenario | Baseline Compiled (ns) | After Compiled (ns) | Roslyn (ns) | DE (ns) | Flee (ns) | Speedup | After/DE |
|---|---:|---:|---:|---:|---:|---:|---:|
| Arithmetic/Precedence | 171 | 159 | 72 | 36 | 3.6 | 1.1x | 4.4x |
| Arithmetic/WithVariables | 198 | 179 | 74 | 37 | 39 | 1.1x | 4.8x |
| Arithmetic/ModuloEquality | 66 | 63 | 73 | 36 | 21 | 1.0x | 1.8x |
| Boolean/Composite | 108 | 108 | 76 | 36 | 40 | 1.0x | 3.0x |
| Conditional/Ternary | 61 | 60 | 72 | 36 | 28 | 1.0x | 1.7x |
| Functions/MathMix | 3,664 | 3,623 | 76 | 36 | 38 | 1.0x | 100.6x |
| Mix/NumericAndPredicate | 183 | 177 | 75 | 37 | 30 | 1.0x | 4.8x |

### Changes from Baseline

- **Arithmetic/Precedence improved** (171ns -> 159ns, ~7%)
- **Arithmetic/WithVariables improved** (198ns -> 179ns, ~10%)
- **Mix/NumericAndPredicate improved** (183ns -> 177ns, ~3%)
- **CsEval Compiled beats Roslyn** in ModuloEquality (63ns vs 73ns) and Conditional/Ternary (60ns vs 72ns)
- **CsEval Compiled beats NCalc** in every scenario (NCalc ranges 173-369ns)
- **Functions/MathMix remains the outlier** at 100x vs DE -- this is entirely dominated by static method resolution overhead

## Advanced Language Benchmarks (After)

| Scenario | CsEval Compiled (ns) | Roslyn (ns) | DE (ns) | Flee (ns) | Compiled/DE | Compiled/Roslyn | Compiled Alloc |
|---|---:|---:|---:|---:|---:|---:|---:|
| Advanced/NestedMath | 3,782 | 80 | 36 | 47 | 105.1x | 47.1x | 6,848 B |
| Advanced/NestedConditional | 123 | 74 | 37 | 48 | 3.3x | 1.7x | 144 B |
| Advanced/StringPredicate | 392 | 81 | 42 | 28 | 9.3x | 4.8x | 896 B |
| Advanced/CollectionProperties | 253 | 74 | 40 | 24 | 6.3x | 3.4x | 520 B |
| Advanced/ObjectGraphAccess | 472 | 76 | 37 | 31 | 12.8x | 6.2x | 800 B |

### Key Observations

- **Advanced/NestedConditional** is excellent at 1.7x vs Roslyn and only 3.3x vs DE
- **NestedMath** is the worst case (same Math.Abs/Max static method resolution bottleneck)
- **StringPredicate and CollectionProperties** are reasonable at 4-6x vs DE
- **ObjectGraphAccess** involves indexer + property access chains, 12.8x vs DE

## Performance Tier Summary

**Tier 1 -- Near parity (< 3x vs DynamicExpresso):**
- TernaryOnly (1.5x), ModuloEquality (1.8x), Conditional/Ternary (1.7x), ArithmeticOnly (2.7x), PropertyAccess (2.9x)

**Tier 2 -- Moderate overhead (3-10x vs DynamicExpresso):**
- NestedConditional (3.3x), Boolean/Composite (3.0x), Arithmetic/Precedence (4.4x), Arithmetic/WithVariables (4.8x), Mix/NumericAndPredicate (4.8x), InstanceMethodCall (5.6x), CollectionProperties (6.3x), StringPredicate (9.3x)

**Tier 3 -- Significant overhead (> 10x vs DynamicExpresso):**
- ObjectGraphAccess (12.8x), StaticMethodCall (28.4x), ChainedStaticCalls (99.8x), Functions/MathMix (100.6x), AdvancedNestedMath (105.1x)

## Root Cause Analysis

The tier 3 gap is caused by CsEval's context-based variable binding architecture:

1. **Static method resolution** -- CsEval resolves methods per-invocation through the runtime context, while DynamicExpresso/Flee compile method references directly into the delegate closure at parse time
2. **Variable boxing** -- CsEval stores variables as `object?` in a dictionary, requiring boxing/unboxing on each access; DynamicExpresso captures typed variables in a closure
3. **Per-invocation overhead** -- Each Evaluate() call traverses the compiled delegate through the context layer, adding fixed overhead that compounds with expression complexity

These are architectural trade-offs that enable CsEval's broader feature set (full C# expression support, runtime variable mutation, sandboxing, execution constraints) at the cost of raw throughput for simple evaluations.
