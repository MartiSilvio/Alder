# CsEval Architecture

This document explains the internal architecture of CsEval and key design decisions.

## Tree-Walking Interpreter

CsEval uses a **tree-walking interpreter** architecture with three phases:

```
Source Code → Lexer → Tokens → Parser → AST → Evaluator → Result
```

### How It Works

1. **Lexer** ([Lexer.cs](../src/CsEval/Parsing/Lexer.cs)): Tokenizes source text into tokens (numbers, operators, identifiers, keywords)

2. **Parser** ([Parser.cs](../src/CsEval/Parsing/Parser.cs)): Uses recursive descent parsing to build an Abstract Syntax Tree (AST). Each grammar rule maps to a parsing method with proper operator precedence.

3. **Evaluator** ([Evaluator.cs](../src/CsEval/Evaluation/Evaluator.cs)): Walks the AST using the visitor pattern (`IExprVisitor<T>`), evaluating each node recursively.

### Why Tree-Walking?

- **Extensibility**: Easy to add custom operators and semantics (like object merging with `+`, spread operators)
- **Transparency**: Clear separation between parsing and evaluation phases
- **Flexibility**: The AST can be inspected, cached (pre-parsing), or evaluated with different contexts
- **Control**: Full control over evaluation semantics without relying on external compilation

### AST Node Types

The AST consists of expression nodes defined in [Ast.cs](../src/CsEval/Parsing/Ast.cs):

```csharp
// Examples
BinaryExpr(Left, Op, Right)     // x + y
CallExpr(Callee, Arguments)     // func(a, b)
LambdaExpr(Parameters, Body)    // x => x * 2
BlockExpr(Statements, Return)   // { var x = 1; return x; }
WhileStatementExpr(Condition, Body)  // while (x > 0) { ... }
```

Each node implements `Accept<T>(IExprVisitor<T>)` for visitor pattern traversal.

### Evaluation Example

When evaluating `x + y * 2`:

1. Parser builds: `BinaryExpr(IdentifierExpr("x"), +, BinaryExpr(IdentifierExpr("y"), *, LiteralExpr(2)))`
2. Evaluator visits the outer `BinaryExpr`
3. Recursively evaluates left (`x` → lookup value) and right (`y * 2` → recursive eval)
4. Applies the `+` operator to the results

## Hybrid Compilation

CsEval supports optional expression compilation using `System.Linq.Expressions`. This provides a hybrid approach: simple expressions can be compiled to delegates for maximum performance, while complex expressions fall back to tree-walking.

### How It Works

```
                    ┌─── compilable ───> ExpressionCompiler ───> Delegate
AST ── CanCompile? ─┤
                    └─── not compilable ───> Evaluator (tree-walk)
```

1. **ExpressionCompiler** ([ExpressionCompiler.cs](../src/CsEval/Evaluation/ExpressionCompiler.cs)): Converts AST nodes to `System.Linq.Expressions.Expression` trees, then compiles to delegates.

2. **CompilerHelpers** ([CompilerHelpers.cs](../src/CsEval/Evaluation/CompilerHelpers.cs)): Static helper methods called by compiled expressions for operations like arithmetic, comparisons, and property access.

### CompilationMode

Three modes control compilation behavior:

| Mode | Behavior |
|------|----------|
| `Disabled` | Always tree-walk. No compilation overhead. |
| `OnDemand` | Tree-walk by default. Compile only when `Compile()` is called explicitly. (Default) |
| `Eager` | Compile during `Parse()` automatically. Non-compilable expressions fall back silently. |

### What Compiles

The `CanCompile()` method checks if an expression can be compiled:

**Compilable (~5-20x speedup):**
- `LiteralExpr` - Constants
- `IdentifierExpr` - Variable lookup via `context.Get()`
- `UnaryExpr` - Negation (`-`), Not (`!`)
- `BinaryExpr` - Arithmetic, comparisons (but not object merging with `+`)
- `LogicalExpr` - `&&`, `||` with short-circuit
- `ConditionalExpr` - Ternary `? :`
- `NullCoalesceExpr` - `??`
- `MemberAccessExpr` - Property access
- `GroupingExpr` - Parentheses

**Not Compilable (tree-walk required):**
- `BlockExpr`, loops, `switch` - Exception-based control flow
- `LambdaExpr`, LINQ methods - Closure capture complexity
- `AssignmentExpr` - Context mutations
- Object merging with `+` - Polymorphic behavior at runtime

### Compiled Delegate Signature

```csharp
delegate object? CompiledExpression(
    EvalContext context,
    CsEvalOptions options,
    CancellationToken cancellationToken);
```

The compiled delegate receives the same parameters as tree-walking, allowing variable access and cancellation support.

### Thread Safety

- `ExpressionCompiler` uses a global `ConcurrentDictionary<string, CompiledExpressionInfo>` for caching
- `CsEvalExpression` stores compilation state in a volatile field
- Multiple threads can safely call `TryCompile()` on the same expression

## LINQ Returns `List<object?>` (Immediate Evaluation)

CsEval intentionally returns `List<object?>` from LINQ methods rather than `IEnumerable<T>`. This is a deliberate design choice, not a limitation.

**Why immediate evaluation?**

1. **Context Safety**: With deferred execution, the evaluation context may change or be disposed by the time the sequence is enumerated. Immediate evaluation ensures results are captured at evaluation time.

2. **Closure Capture**: Lambda expressions in deferred LINQ chains capture the evaluator's context. If the context changes between definition and enumeration, results become unpredictable.

3. **Multiple Enumeration**: `List<object?>` can be enumerated multiple times safely. Deferred sequences may have side effects on re-enumeration or may not support it at all.

4. **Index Access**: Lists support direct index access (`result[0]`), which is commonly needed in expressions. Deferred sequences require `.ElementAt()` or `.ToList()` first.

5. **Predictability**: Expression evaluation should be deterministic. Deferred execution introduces timing dependencies that make debugging difficult.

**Trade-off**: This means LINQ chains are always fully evaluated, even for large collections. For performance-critical scenarios with large datasets, consider filtering in the data source before passing to CsEval.

## Numeric Type Handling

CsEval matches C# numeric literal behavior:

**Literal parsing:**
- `42` → `int` (default, auto-promotes to `long` if too large for int)
- `42L` → `long` (explicit suffix)
- `42U` → `uint`, `42UL` → `ulong`
- `3.14` → `double` (default for floating-point)
- `3.14f` → `float`, `3.14m` → `decimal` (explicit suffixes)

**Arithmetic result types (matches C#):**

| Operation | Result Type |
|-----------|-------------|
| `decimal` op anything | `decimal` |
| `double`/`float` op non-decimal | `double` |
| `int` op `int` | `int` |
| `int` op `long` | `long` |
| `long` op `long` | `long` |
| small types (`byte`, `short`) | promote to `int` |
| Division (non-decimal) | `double` |

**Precision:**
- `decimal`: 28-29 significant digits
- `double`/`float`: 15-17 significant digits
- When mixing types, the higher-precision type wins

**Type coercion:**
- When comparing values (e.g., `list.Contains(2)`), CsEval automatically handles type mismatches between `int`, `long`, `double`, `float`, `decimal` by converting both values to `double` for comparison.
- External types (`float`, `decimal`, `short`, `byte`) work seamlessly in expressions.

## GroupBy Returns Dictionaries

Unlike C#'s `IGrouping<TKey, TElement>`, CsEval's `GroupBy` returns dictionaries with `Key` and `Items` properties:

```csharp
items.GroupBy(x => x.Category)
// Returns: [{ Key: "A", Items: [...] }, { Key: "B", Items: [...] }]
```

This simplifies access in expressions and avoids the complexity of generic interface handling.

## Zip Without Selector Returns Dictionaries

C# 10+ returns `ValueTuple<T1, T2>` for `Zip` without a result selector. CsEval returns dictionaries with `First` and `Second` properties:

```csharp
names.Zip(ages)
// Returns: [{ First: "Alice", Second: 30 }, { First: "Bob", Second: 25 }]
```

This provides named access without requiring tuple syntax support in the parser.

## Performance

For detailed benchmarking information, see [benchmarks.md](benchmarks.md).
