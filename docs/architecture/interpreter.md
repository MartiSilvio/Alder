---
title: "Interpreter"
description: "Tree-walking evaluation, numeric dispatch, control flow signals, AOT integration"
sidebar:
  order: 7
---

The interpreter is Alder's default execution backend. It walks the bound tree node by node, evaluating each expression and propagating results through the tree.

## Dispatch

Each bound node kind has a dedicated evaluator class annotated with `[EvaluatesNode(BoundNodeKind.Xxx)]`. Dispatch is source-generated at compile time — the generated code is a flat switch expression mapping each node kind to its evaluator's static `Evaluate` method. No virtual method calls, no visitor pattern overhead.

## Numeric Operations

Binary and unary operators use a two-tier dispatch that avoids `dynamic` and reflection entirely:

### Fast path

When the binder has computed a `PromotedType` AND the runtime values match their static types, the engine routes directly to pre-built delegate tables keyed by type pairs:

```
Add:      { (int,int)→(l,r)=>l+r, (long,long)→(l,r)=>l+r, (double,double)→(l,r)=>l+r, ... }
Subtract: { (int,int)→(l,r)=>l-r, ... }
Compare:  { (int,int)→(l,r)=>l.CompareTo(r), ... }
```

This is how `1 + 2` evaluates without boxing. The binder pre-computes the promoted type (`int` for `int + int`), and the interpreter uses it as a direct table lookup key. The tables cover seven numeric types: `int`, `long`, `float`, `double`, `decimal`, `uint`, `ulong`.

Checked arithmetic has separate delegate tables that use `checked()` for integer types and standard operations for floating-point (per IEEE 754).

### Fallback path

When types don't match the fast path — mixed types, nullables, `string + object`, `DateTime ± TimeSpan`, enum arithmetic, user-defined operators — the engine falls through to general operator handling:

- String concatenation (any operand is `string`)
- `DateTime` arithmetic (`DateTime + TimeSpan`, `DateTime - DateTime`)
- Enum arithmetic (`Enum + int`, `Enum - Enum`, `~Enum`)
- Nullable three-valued logic (`bool? & bool?`, `bool? | bool?`)
- User-defined operators (`op_Addition`, `op_Subtraction`, etc.)
- Cross-type numeric promotion via the ECMA-334 §12.4.7.3 rules (see [Numeric Promotion](numeric-promotion.md))
- Extended mode: string repetition (`"ab" * 3`), object merge (`new { A = 1 } + new { B = 2 }`)

Before the fallback path, ECMA-334 §10.2.11 constant promotion is applied — if one operand is a literal constant, it may be implicitly promoted (e.g., `int` 0 to `uint`).

## Control Flow

### Signal Propagation

Control flow uses a signal propagation pattern. `break`, `continue`, `return`, `goto`, `goto case`, and `goto default` produce `ControlFlowSignal` values that propagate upward through the evaluation stack:

| Signal | Produced by | Consumed by |
|--------|------------|-------------|
| `Return(value)` | `return expr;` | Engine entry point, lambda invocation |
| `Break` | `break;` | Innermost loop or switch |
| `Continue` | `continue;` | Innermost loop |
| `GotoCase(value)` | `goto case value;` | Switch statement |
| `GotoDefault` | `goto default;` | Switch statement |
| `Goto(label)` | `goto label;` | Block containing the target label |

Signals are ordinary return values, not exceptions. Every intermediate construct (blocks, if-statements, try-catch) checks for signals and propagates them. Unwrapping happens at three boundaries:

1. **Engine entry point**: Extracts the return value
2. **Lambda invocation**: Extracts the return value for the caller
3. **Compiled root**: Emits IL to extract the return value

### Loops

Each loop creates a child context for the iteration scope. The child context is reused between iterations (cleared, not recreated) for performance. Execution constraints — statement count, timeout, per-loop iteration count — are checked at the top of each iteration.

### Goto and Labels

`goto label` in a block produces a `Goto` signal. The block evaluator scans the statement list for the matching label, sets the execution index to the label position, and re-enters the statement loop — providing efficient intra-block jumps without new stack frames.

### Switch Fall-Through

Switch cases enforce C#'s no-fall-through rule: if a case body executes without producing a control flow signal (break, return, throw, or goto), `CS0163` is thrown. `goto case` and `goto default` produce signals that the switch evaluator resolves by scanning for the matching case, then re-executing from that case in a loop — enabling multi-hop chains without recursion.

### Try/Catch Signal Preservation

Control flow signals inside a `try` block are captured and held while the `finally` block executes. After `finally` completes, the held signal is returned. This ensures that `return` inside `try` still executes `finally` — matching C# semantics exactly.

## Pattern Matching

The pattern matching runtime handles every ECMA-334 §11.2 pattern kind:

| Pattern | Behavior |
|---------|----------|
| Constant | Evaluates constant, compares with equality |
| Type | Runtime type check, optional variable binding |
| Var | Always matches, binds variable |
| Discard (`_`) | Always matches, no binding |
| Relational (`<`, `<=`, `>`, `>=`) | Comparison against operand |
| And / Or / Not | Short-circuit logical combinators |
| Property (`{ Prop: pattern }`) | Optional type check, then reads each property and recursively matches subpatterns. Short-circuits on first mismatch. |
| Positional (`(p1, p2)`) | Checks `ITuple`, validates length, recursively matches elements |
| List (`[p1, p2, ..]`) | Length check, prefix/suffix matching, optional slice capture |
| Slice (`..`) | Captures remaining elements between prefix and suffix in list patterns |

Pattern variables are defined in the evaluation context at match time and visible to `when` guards and subsequent code.

## Member Access

### Resolved

When the binder produced a resolved node (`BoundPropertyAccessExpr`, `BoundFieldAccessExpr`), the interpreter has the `PropertyInfo`/`FieldInfo` and calls `GetValue` directly. AOT typed dispatch is checked first — if available, no reflection is needed.

Null-conditional (`?.`): if the target is null, returns null without accessing the member.

### Dynamic

When the binder produced `BoundDynamicMemberAccessExpr`, the interpreter resolves the member at runtime:
1. Check typed dispatch (AOT path)
2. Check cached type metadata for property/field
3. For `ExpandoObject`, read from the `IDictionary<string, object?>` interface
4. Extended mode: check date/time sugar for numeric units (`30.days`, `2.hours`)

## Method Invocation

Method calls use a three-tier dispatch:

**Tier 1 — Typed dispatch (AOT, O(1))**: Check for source-generator-produced dispatch. No reflection.

**Tier 2 — Reflection with caching**: Overload resolution selects the best method. The resolved method is cached keyed by type, method name, and argument shape. Methods with 0–4 parameters that aren't on value types get a compiled fast-invoke delegate that bypasses `MethodInfo.Invoke`.

**Tier 3 — Extension methods**: Searched per-type in registration order. Each extension type is checked with interleaved typed dispatch (Tier 1) + reflection (Tier 2). Lambda arguments undergo return type inference — the engine evaluates the lambda body with the collection's element type to infer the return type and convert the lambda to a concrete delegate for overload selection.

### Callable Types

The unified call entry point handles all callable types: module methods (dependency-injected), registered functions, lambdas (with closure capture), compiled lambdas (with arity-specialized paths), standard .NET delegates, and static/instance method references.

### CancellationToken Injection

If a method's last parameter is `CancellationToken` and the caller provided one fewer argument, the engine automatically appends the current cancellation token. This is transparent to expression authors.

## AOT Integration

Before any reflection-based member access or method invocation, the engine checks for typed dispatch via the AOT path. The check walks the type hierarchy — registering dispatch for `List<int>` covers access to members inherited from `IList<int>`, `ICollection<int>`, etc.

## Tracing

When `EvaluateWithTrace` is called, the interpreter records each node before evaluation and each result after. The tracer builds a tree that mirrors the evaluation order. Tracing uses the security-only pipeline (no constant folding or dead branch elimination) so every node is visible.

## Context Management

The interpreter maintains a current scope that changes during evaluation:
- Block entry → create child context
- Loop iteration → reuse child context, clear between iterations
- `for` loop → two child contexts (initializer scope + body scope)

The engine saves and restores context around scope changes using local variables, ensuring that scope exits always restore the previous context even on exceptions.

## Performance Details

Frequently-returned values (`true`, `false`, `0`, `1`, `null`) use pre-allocated boxed instances to avoid repeated boxing allocations on the hot path.
