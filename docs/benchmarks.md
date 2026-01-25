# CsEval Benchmarks & Performance

This document covers CsEval's performance characteristics, benchmark suite, and optimization techniques.

## Quick Start

```bash
cd benchmarks/CsEval.Benchmarks
dotnet run -c Release
```

**Important**: Always run in Release mode for accurate results.

## Benchmark Suites

### 1. StandardBenchmarks

Classic language benchmarks from the [Computer Language Benchmarks Game](https://benchmarksgame-team.pages.debian.net/benchmarksgame/):

| Benchmark               | Description                        |
| ----------------------- | ---------------------------------- |
| **Fibonacci_Iterative** | Iterative Fibonacci (n=10, 20, 30) |
| **SumLoop**             | Sum 1 to N (n=100, 1000, 10000)    |
| **PrimeCheck**          | Is N prime? (97, 7919, 104729)     |
| **Factorial**           | N! calculation (n=10, 15, 20)      |
| **CollatzSequence**     | Steps to reach 1 (27, 97, 871)     |

Each includes a `_NativeCSharp` comparison method.

### 2. CsEvalBenchmarks

CsEval-specific performance:

| Benchmark                   | Description                           |
| --------------------------- | ------------------------------------- |
| **Parse\_\***               | Time to tokenize and parse expression |
| **Evaluate\_\*\_PreParsed** | Evaluate pre-parsed expression        |
| **ParseAndEvaluate\_\***    | Full pipeline (parse + evaluate)      |

### 3. LinqBenchmarks

LINQ method performance at different collection sizes:

| Benchmark            | Description                                    |
| -------------------- | ---------------------------------------------- |
| **Where/Select/Sum** | Individual operations (10, 100, 1000 items)    |
| **Chained**          | `Where().Select().Sum()` pipeline              |
| **\_NativeCSharp**   | Native C# comparisons for overhead measurement |

### 4. PropertyAccessBenchmarks

Property access on typed objects (tests compiled getter performance):

| Benchmark                   | Description                                        |
| --------------------------- | -------------------------------------------------- |
| **PropertyAccess_Single**   | Single property access on typed object             |
| **PropertyAccess_Multiple** | Multiple properties on same object                 |
| **ObjectMerge_TypedObject** | Merge typed object with anonymous object           |
| **Spread_TypedObject**      | Spread operator on typed object                    |
| **Linq_OnTypedObjects**     | LINQ with property access in lambda                |
| **RepeatedPropertyAccess**  | Same property accessed 100/1000 times (warm cache) |
| **RepeatedObjectMerge**     | Same merge operation 100/1000 times (warm cache)   |

The `Repeated*` benchmarks demonstrate the benefit of compiled property getters with warm caches.

### 5. BlockExpressionBenchmarks

Control flow performance:

| Benchmark                       | Description                                 |
| ------------------------------- | ------------------------------------------- |
| **SimpleBlock**                 | Variable declaration and return             |
| **IfElseBlock**                 | Conditional branching                       |
| **ForLoopBlock/WhileLoopBlock** | Loop performance (10, 100, 1000 iterations) |
| **NestedLoopBlock**             | O(n²) nested loops (10, 20, 30)             |

### 6. CompilationBenchmarks

Expression compilation performance comparing compiled vs tree-walking evaluation:

| Benchmark                              | Description                                |
| -------------------------------------- | ------------------------------------------ |
| **Evaluate_Compiled_SimpleArithmetic** | Compiled `1 + 2 * 3` evaluation            |
| **Evaluate_TreeWalk_SimpleArithmetic** | Tree-walking `1 + 2 * 3` evaluation        |
| **Evaluate_Compiled_WithVariables**    | Compiled `x + y * z` evaluation            |
| **Evaluate_TreeWalk_WithVariables**    | Tree-walking `x + y * z` evaluation        |
| **Evaluate_Compiled_Ternary**          | Compiled ternary expression                |
| **Evaluate_Compiled_PropertyAccess**   | Compiled property access                   |
| **CompileTime\_\***                    | Time to compile different expression types |

The `_Compiled` vs `_TreeWalk` comparisons show the speedup from compilation (~5-20x for simple expressions).

## Running Specific Benchmarks

```bash
# All benchmarks
dotnet run -c Release -- --filter *

# Standard language benchmarks only
dotnet run -c Release -- --filter *StandardBenchmarks*

# Only Fibonacci
dotnet run -c Release -- --filter *Fibonacci*

# Property access benchmarks (tests compiled getters)
dotnet run -c Release -- --filter *PropertyAccess*

# Export to JSON
dotnet run -c Release -- --filter * --exporters json
```

## Understanding Results

BenchmarkDotNet outputs:

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Allocated**: Memory allocated per operation

Compare CsEval methods with `_NativeCSharp` counterparts to measure interpreter overhead.

## Performance Characteristics

### Tree-Walking Interpreter Overhead

CsEval is a tree-walking interpreter, which means:

- Each AST node requires virtual method dispatch
- Variable lookups traverse the scope chain
- Type checking and boxing occur at runtime

This architecture provides flexibility and extensibility but has inherent overhead compared to compiled code. Typical overhead is ~100-200x compared to native C#, which is normal for interpreters.

### Pre-Parsing Benefit

Pre-parsed expressions skip the lexer and parser phases:

```csharp
// Parse once, evaluate many times
var expr = engine.Parse("x + y * 2");
for (int i = 0; i < 1000; i++)
{
    engine.SetVariable("x", i);
    engine.Evaluate(expr);
}
```

This provides ~80% speedup for repeated evaluations.

### Compiled Property Getters

CsEval uses compiled property getters via Expression Trees for typed object property access. This eliminates `PropertyInfo.GetValue()` reflection overhead:

- First access: Compiles getter delegate (~microseconds)
- Subsequent accesses: Direct delegate invocation (~150ns)

The `TypeCache` maintains a `ConcurrentDictionary<PropertyInfo, Func<object, object?>>` for thread-safe caching.

### LINQ Overhead

CsEval's LINQ implementation has overhead from:

- Lambda invocation through the evaluator
- Immediate materialization to `List<object?>`
- Dynamic property access on items

For performance-critical scenarios with large datasets, consider filtering in the data source before passing to CsEval.

### Loop Scaling

Loops scale linearly with iteration count. The per-iteration cost includes:

- Condition evaluation
- Variable lookup/update
- Loop control flow handling

### Automatic IL Compilation

CsEval automatically compiles expressions to native IL via `System.Linq.Expressions` (Expression Trees) during `Parse()`. This provides near-native performance for most expressions, with automatic fallback to tree-walking for non-compilable expressions.

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("x", 10);
engine.SetVariable("y", 20);

var expr = engine.Parse("x + y * 2");  // Automatically IL-compiled
Console.WriteLine(expr.IsCompiled);    // true

engine.Evaluate(expr);  // Uses compiled delegate
```

**What compiles to native IL (~5-50x speedup):**

- Literals, identifiers, property access
- Arithmetic (`+`, `-`, `*`, `/`, `%`) on strings and numerics
- Comparisons (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical (`&&`, `||`, `!`) with short-circuit
- Ternary (`? :`), null coalesce (`??`)
- Control flow: `if`/`else`, `for`, `while`, `do-while`, `foreach`
- `break`, `continue`, `return` (uses IL branches, not exceptions)
- Variable declarations and assignments
- Compound assignments (`+=`, `-=`, etc.) and increment/decrement

**What falls back to tree-walking:**

- LINQ methods with lambda expressions
- Method calls on objects
- Switch statements
- Object spread/merge

## Optimization Summary

| Optimization           | Impact                                      | Location                        |
| ---------------------- | ------------------------------------------- | ------------------------------- |
| Pre-parsing            | ~80% faster for repeated eval               | `CsEvalEngine.Parse()`          |
| Expression compilation | ~5-20x faster for simple expressions        | `ExpressionCompiler.cs`         |
| TypeCache              | Eliminates repeated reflection              | `TypeCache.cs`                  |
| Compiled getters       | ~350x faster than `PropertyInfo.GetValue()` | `TypeCache.GetCompiledGetter()` |
| ConcurrentDictionary   | Thread-safe caching                         | All caches                      |
