# CsEval Architecture

This document explains the internal architecture of CsEval and key design decisions.

## Overview

CsEval processes expressions in three phases:

```
Source Code → Lexer → Tokens → Parser → AST → IL Compilation → Result
```

Expressions are compiled to native IL via `System.Reflection.Emit.DynamicMethod` for maximum performance. Non-compilable expressions (LINQ with lambdas, method calls) fall back to tree-walking.

## Phase 1: Lexing

The [Lexer](../src/CsEval/Parsing/Lexer.cs) tokenizes source text into tokens: numbers, operators, identifiers, keywords, and punctuation.

## Phase 2: Parsing

The [Parser](../src/CsEval/Parsing/Parser.cs) uses recursive descent to build an Abstract Syntax Tree (AST). Each grammar rule maps to a parsing method with proper operator precedence.

The AST consists of expression nodes defined in [Ast.cs](../src/CsEval/Parsing/Ast.cs):

```csharp
BinaryExpr(Left, Op, Right)          // x + y
CallExpr(Callee, Arguments)          // func(a, b)
LambdaExpr(Parameters, Body)         // x => x * 2
BlockExpr(Statements, Return)        // { var x = 1; return x; }
WhileStatementExpr(Condition, Body)  // while (x > 0) { ... }
```

## Phase 3: Evaluation

### IL Compilation (Primary)

CsEval compiles expressions to native IL using `System.Reflection.Emit.DynamicMethod`. This happens automatically during `Parse()`.

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("x + y * 2");  // Automatically compiled
Console.WriteLine(expr.IsCompiled);    // true
```

The [ILCompiler](../src/CsEval/Evaluation/ILCompiler.cs) emits IL instructions for:

- Literals, identifiers, property access
- Arithmetic, comparisons, logical operators (with short-circuit)
- Ternary (`? :`), null coalesce (`??`)
- Control flow: `if`/`else`, `for`, `while`, `do-while`, `foreach`
- `break`, `continue`, `return` (native IL branches)
- Variable declarations and assignments

The compiled delegate signature:

```csharp
delegate object? ILCompiledDelegate(
    EvalContext context,
    CsEvalOptions options,
    CancellationToken cancellationToken);
```

**Key implementation details:**

1. **Context-Based Scoping**: Control flow blocks create child contexts via `EvalContext.CreateChild()` for proper variable isolation.

2. **Loop Control**: `break` and `continue` use IL branch instructions (`br`) to labeled targets, not exceptions.

3. **Safety Checks**: Loops emit iteration limit checks and cancellation token checks.

4. **Resource Cleanup**: `foreach` uses try/finally to ensure `IEnumerator.Dispose()` is called.

### Tree-Walking (Fallback)

Expressions that cannot be IL-compiled fall back to the [Evaluator](../src/CsEval/Evaluation/Evaluator.cs), which walks the AST using the visitor pattern. This handles:

- Lambda expressions (closure capture)
- LINQ method chains
- Runtime method calls
- Object merging operations

The fallback is automatic and silent - callers don't need to handle it.

## Design Decisions

### LINQ Returns `List<object?>`

CsEval returns `List<object?>` from LINQ methods rather than `IEnumerable<T>` (immediate evaluation).

**Why:**
- Context may change before deferred enumeration
- Lists support multiple enumeration and index access
- Deterministic evaluation without timing dependencies

**Trade-off:** Large collections are fully evaluated. Filter in the data source for performance-critical scenarios.

### Numeric Type Handling

CsEval matches C# numeric literal and arithmetic behavior exactly:

**Literals:**
- `42` → `int`, `42L` → `long`, `42U` → `uint`, `42UL` → `ulong`
- `3.14` → `double`, `3.14f` → `float`, `3.14m` → `decimal`

**Arithmetic (via `dynamic`):**
- `int / int` → `int` (truncates!)
- Mixed types promote: `int + long` → `long`, `int + float` → `float`
- `decimal + float` or `decimal + double` → **Throws** (C# forbids this)

### GroupBy and Zip Return Dictionaries

- `GroupBy` returns `[{ Key: ..., Items: [...] }, ...]` instead of `IGrouping<TKey, TElement>`
- `Zip` (no selector) returns `[{ First: ..., Second: ... }, ...]` instead of tuples

This simplifies access in expressions without requiring generic interface or tuple syntax support.

## Thread Safety

- Compiled delegates are thread-safe after creation
- `CsEvalExpression` stores compilation state in a volatile field
- Use `CreateChild()` for concurrent evaluation with isolated contexts

## Performance

See [benchmarks.md](benchmarks.md) for detailed performance information.
