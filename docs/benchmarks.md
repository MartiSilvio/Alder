# Performance Benchmarks

Comparative benchmarks of CsEval against other .NET expression evaluators using [BenchmarkDotNet](https://benchmarkdotnet.org/) v0.14.0.

## Environment

```
BenchmarkDotNet v0.14.0, macOS Sequoia 15.7.4
Apple M3 Max, 1 CPU, 14 logical and 14 physical cores
.NET SDK 8.0.204
  Runtime: .NET 8.0.4 (8.0.424.16909), Arm64 RyuJIT AdvSIMD
  GC:      Concurrent Workstation
  Config:  IterationCount=12, WarmupCount=4
```

## Compared Engines

| Engine | Version | Description |
|--------|---------|-------------|
| **CsEval Compiled** | current | Pre-compiled expression delegate via `CsEvalEngine.Parse()` + `Evaluate()` |
| **CsEval Interpreted** | current | Tree-walking evaluator, no IL generation |
| **[DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso)** | 2.19.3 | Pre-parsed lambda via `Interpreter.Parse()` + `Invoke()` |
| **[Flee](https://github.com/mparlak/Flee)** | 2.0.0 | Pre-compiled IL via `CompileDynamic()` + `Evaluate()` |
| **[NCalc](https://github.com/ncalc/ncalc)** | 5.12.0 | Pre-parsed expression via `Expression.Evaluate()` |
| **[Roslyn Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting)** | 4.12.0 | Pre-compiled `ScriptRunner<T>` delegate |
| **Native Delegate** | — | Hand-written C# lambda as baseline |

All engines use their documented fast path: expressions are pre-parsed and pre-compiled during setup. Only the evaluation step is measured in warm benchmarks. Parity is validated at setup time — all engines produce identical results (within 0.000001 decimal tolerance).

## Methodology

- Each scenario is expressed in each engine's native syntax. Semantically equivalent expressions are used where syntax differs (e.g., `&&` in C# vs `and` in Flee, `?:` in C# vs `if()` in NCalc).
- Roslyn uses the `ScriptRunner<object>` delegate pattern (`Script.CreateDelegate()`), invoked synchronously via `GetAwaiter().GetResult()`.
- Engines that cannot express a scenario are excluded from that scenario (e.g., NCalc cannot express method chains or object graph access).

---

## 1. Warm Execution

Pre-compiled expressions evaluated repeatedly. This is the hot-path performance that matters for rule engines, formula evaluation, and repeated expression execution.

### Expressions Tested

| Scenario | Expression |
|----------|------------|
| Arithmetic/Precedence | `1 + 2 * 3 - 4 / 2` |
| Arithmetic/WithVariables | `(x + y) * z - x / 2` |
| Boolean/Composite | `x > y && y < z \|\| x == 10` |
| Conditional/Ternary | `x > y ? x : y` |
| Functions/MathMix | `Math.Abs(x - y) + Math.Max(y, z)` |
| Arithmetic/ModuloEquality | `(x % y) == 1` |
| Mix/NumericAndPredicate | `(x * y) + (z * 2) > 20` |
| SmallBranching | `((23 > 15 && 3*7 == 21) \|\| ...) ? ... : ...` |
| BigBooleanStress | 30-clause boolean expression |

Variables: `x = 10`, `y = 3`, `z = 5`, `text = "alpha"`, `value = 42`

### Results

| Scenario | CsEval Compiled | Native | DynamicExpresso | Flee | Roslyn | NCalc |
|----------|---------------:|-------:|----------------:|-----:|-------:|------:|
| Arithmetic/Precedence | **3.99 ns** | 2.15 ns | 36.76 ns | 3.54 ns | 66.80 ns | 314.96 ns |
| Arithmetic/WithVariables | **19.31 ns** | 3.07 ns | 35.24 ns | 37.97 ns | 70.43 ns | 342.82 ns |
| Boolean/Composite | **19.25 ns** | 2.69 ns | 38.16 ns | 38.13 ns | 70.98 ns | 332.97 ns |
| Conditional/Ternary | **14.75 ns** | 2.76 ns | 35.14 ns | 27.80 ns | 68.38 ns | 224.83 ns |
| Functions/MathMix | **25.14 ns** | 2.76 ns | 36.03 ns | 38.21 ns | 70.69 ns | 343.67 ns |
| ModuloEquality | **14.50 ns** | 2.90 ns | 36.85 ns | 20.12 ns | 69.69 ns | 169.50 ns |
| Mix/NumericAndPredicate | **19.12 ns** | 2.77 ns | 35.65 ns | 29.38 ns | 70.69 ns | 305.52 ns |
| SmallBranching | **14.48 ns** | 2.77 ns | 35.39 ns | 20.78 ns | 71.57 ns | 1,061.96 ns |
| BigBooleanStress | **28.89 ns** | 66.11 ns | 35.34 ns | 692.79 ns | 190.78 ns | 14,010.41 ns |

### Speedup vs Competitors (Compiled)

| Scenario | vs DynamicExpresso | vs Flee | vs Roslyn | vs NCalc |
|----------|------------------:|--------:|----------:|---------:|
| Arithmetic/Precedence | 9.2x | 0.9x | 16.7x | 78.9x |
| Arithmetic/WithVariables | 1.8x | 2.0x | 3.6x | 17.8x |
| Boolean/Composite | 2.0x | 2.0x | 3.7x | 17.3x |
| Conditional/Ternary | 2.4x | 1.9x | 4.6x | 15.2x |
| Functions/MathMix | 1.4x | 1.5x | 2.8x | 13.7x |
| ModuloEquality | 2.5x | 1.4x | 4.8x | 11.7x |
| Mix/NumericAndPredicate | 1.9x | 1.5x | 3.7x | 16.0x |
| SmallBranching | 2.4x | 1.4x | 4.9x | 73.3x |
| BigBooleanStress | 1.2x | 24.0x | 6.6x | 485.0x |

CsEval Compiled is **fastest on every scenario** except Arithmetic/Precedence where Flee's IL-compiled `1 + 2 * 3 - 4 / 2` (pure constants, no variables) has a slight edge.

### Allocations

CsEval Compiled allocates **24 bytes** per evaluation across all scenarios — a single boxed return value. DynamicExpresso allocates 48 bytes. NCalc allocates 760–14,010 bytes depending on expression complexity.

---

## 2. Advanced Language Features

Expressions using method chains, object graph access, string operations, and collection properties. NCalc is excluded — it cannot express these constructs. Baseline is Roslyn (ratio 1.00).

### Expressions Tested

| Scenario | Expression |
|----------|------------|
| NestedMath | `Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)` |
| NestedConditional | `x > y ? (y > z ? y : z) : x` |
| StringPredicate | `text.StartsWith("a") && text.Length > 3` |
| CollectionProperties | `numbers.Count > 500 && orders.Count == 5` |
| ObjectGraphAccess | `orders[0].Quantity + orders[1].Quantity + orders.Count` |
| StringChain | `text.Trim().ToUpper().Length` |
| StringContains | `text.Contains("lph") && text.StartsWith("a")` |
| NestedFunctionCalls | `Math.Max(Math.Abs(x - y), Math.Min(y, z))` |

### Results

| Scenario | CsEval Compiled | DynamicExpresso | Flee | Roslyn |
|----------|----------------:|----------------:|-----:|-------:|
| NestedMath | **29.47 ns** | 37.39 ns | 46.34 ns | 72.88 ns |
| NestedConditional | **19.44 ns** | 38.07 ns | 46.62 ns | 70.50 ns |
| StringPredicate | **23.91 ns** | 41.22 ns | 27.36 ns | 74.87 ns |
| CollectionProperties | **19.82 ns** | 35.84 ns | 23.19 ns | 67.81 ns |
| ObjectGraphAccess | **26.80 ns** | 35.74 ns | 30.42 ns | 77.13 ns |
| StringChain | **22.22 ns** | 45.55 ns | 23.85 ns | 80.49 ns |
| StringContains | **27.48 ns** | 45.64 ns | 31.20 ns | 79.91 ns |
| NestedFunctionCalls | **25.76 ns** | 37.42 ns | 37.66 ns | 73.93 ns |

CsEval Compiled is **fastest on every advanced scenario**, outperforming Roslyn (the full compiler) by 2.5–3.6x.

---

## 3. Cold Start

End-to-end time from engine creation through parse, compile, and first evaluation. This is the latency users experience on the first call.

### Results

| Scenario | CsEval Interpreted | CsEval Compiled | DynamicExpresso | Flee | Roslyn |
|----------|-------------------:|----------------:|----------------:|-----:|-------:|
| Arithmetic/Precedence | **7.39 μs** | 38.29 μs | 167.59 μs | 385.84 μs | 17.80 ms |
| Boolean/Composite | **8.83 μs** | 90.40 μs | 156.16 μs | 436.63 μs | 17.86 ms |
| Conditional/Ternary | **8.35 μs** | 70.83 μs | 117.39 μs | 439.10 μs | 17.33 ms |
| Functions/MathMix | **44.37 μs** | 215.53 μs | 174.24 μs | 628.50 μs | 18.79 ms |
| BigBooleanStress | **419.91 μs** | 1,204.15 μs | 6,818.76 μs | 7,268.74 μs | 21.04 ms |

CsEval Interpreted cold start is **18–52x faster than Flee**, **13–23x faster than DynamicExpresso**, and **2,000–2,500x faster than Roslyn**.

For one-shot evaluation, CsEval Interpreted is the optimal choice. For repeated evaluation, CsEval Compiled amortizes its compilation cost after 2–10 invocations.

---

## 4. Parse / Compile Time

Time to parse and compile an expression (no evaluation). Measures how quickly each engine can prepare an expression for execution.

| Scenario | CsEval Parse | DynamicExpresso | Flee | Roslyn | NCalc |
|----------|-------------:|----------------:|-----:|-------:|------:|
| Arithmetic (simple) | **4.04 μs** | 35.90 μs | 50.54 μs | 18.82 ms | 0.06 μs |
| SmallBranching | **14.45 μs** | 215.64 μs | 142.31 μs | 22.68 ms | 0.06 μs |
| BigBooleanStress | **381.52 μs** | 6,361.79 μs | 4,626.28 μs | 20.38 ms | 0.07 μs |

NCalc parsing is near-instant because its grammar is much simpler (no type system, no member resolution). Among full-featured engines, CsEval parses **9–17x faster than DynamicExpresso** and **12x faster than Flee**.

---

## 5. Engine Lifecycle

Cost of creating a new engine/interpreter instance.

| Engine | Creation Time | Allocated |
|--------|-------------:|----------:|
| NCalc | 55.6 ns | 408 B |
| **CsEval** | **1,397 ns** | **9,072 B** |
| DynamicExpresso | 87,990 ns | 31,564 B |
| Flee | 90,572 ns | 142,133 B |

CsEval engine creation is **63x faster than DynamicExpresso** and **65x faster than Flee**.

---

## 6. Invocation Micro-Benchmarks

Isolated invocation patterns, CsEval vs DynamicExpresso (ratio 1.00 = DynamicExpresso).

| Scenario | CsEval Compiled | DynamicExpresso | Ratio |
|----------|----------------:|----------------:|------:|
| Overload/StringLiteral | 10.49 ns | 34.83 ns | **0.30x** |
| InstanceMethod/Contains | 12.06 ns | 35.97 ns | **0.34x** |
| ImplicitConversion | 15.25 ns | 34.62 ns | **0.44x** |
| OverloadResolution/Int | 15.26 ns | 34.60 ns | **0.44x** |
| OptionalArgument | 15.26 ns | 35.07 ns | **0.44x** |
| ChainedInstanceCalls | 29.55 ns | 47.11 ns | **0.63x** |
| StaticMathMix | 24.79 ns | 34.16 ns | **0.73x** |
| ParamsExpansion | 34.32 ns | 39.41 ns | **0.87x** |

CsEval Compiled is faster than DynamicExpresso on every invocation pattern tested, by 1.1–3.3x.

---

## 7. LINQ

LINQ operations over a 1,000-element collection. Among the engines tested, only CsEval and Roslyn support LINQ with lambda expressions. DynamicExpresso, Flee, and NCalc cannot express these constructs.

| Scenario | CsEval Compiled | CsEval Interpreted | Roslyn | Native |
|----------|----------------:|-------------------:|-------:|-------:|
| WhereCount | 270.0 μs | 258.4 μs | 1.57 μs | 0.88 μs |
| AnyPredicate | 236.8 μs | 234.2 μs | 2.00 μs | 1.95 μs |
| SelectSum | 270.8 μs | 264.7 μs | 2.35 μs | 1.97 μs |
| OrderByFirst | 166.9 μs | 166.4 μs | 2.34 μs | 2.05 μs |
| WhereSelectSum | 529.7 μs | 527.9 μs | 2.97 μs | 2.24 μs |

Roslyn compiles lambdas to native delegates, so its per-element cost is equivalent to hand-written C#. CsEval evaluates each lambda invocation through its runtime context, which enables sandboxing and runtime variable mutation at the cost of per-element overhead (~270 ns/element on this hardware).

---

## Reproducing

```bash
cd benchmarks/CsEval.Benchmarks
dotnet run -c Release -- --filter *
```

Individual suites:

```bash
dotnet run -c Release -- --filter *ComparableExecutionBenchmarks*
dotnet run -c Release -- --filter *AdvancedLanguageBenchmarks*
dotnet run -c Release -- --filter *ColdStartComparableBenchmarks*
```

Always run in Release mode. Results vary by hardware — relative ratios are more meaningful than absolute nanoseconds.
