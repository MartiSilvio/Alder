---
title: Extended language mode
description: How LanguageMode.Extended expands Alder's C# syntax with scripting forms, expression ergonomics, and host-controlled compatibility boundaries.
---

# Extended language mode

`LanguageMode.Extended` accepts Standard mode plus Alder-specific syntax for rules, filters, scripts, and query fragments: pipelines, inclusive and exclusive integer ranges, collection literals, regex predicates, SQL-style comparisons, date arithmetic sugar, and concise aggregate helpers. These forms use Alder's C# binding model, CLR type system, sandbox enforcement, and execution backends.

Extended mode is a host policy choice. It fits expressions authored by application users, administrators, analysts, rule authors, and configuration systems that benefit from compact syntax. Standard mode remains the default when the accepted syntax should stay within Alder's C# subset.

## Standard and Extended

`LanguageMode.Standard` accepts C# expressions and statement blocks. It is the default on `AlderOptions` and the baseline for compatibility-sensitive integrations.

`LanguageMode.Extended` accepts Standard mode syntax plus Alder-specific forms:

```csharp
var engine = new AlderEngine(options =>
{
    options.LanguageMode = LanguageMode.Extended;
});

var value = engine.Evaluate<double>("2 ** 10");
```

The mode is captured when the engine is created. It affects parsing, binding, and runtime helper resolution for that engine and its child contexts. It leaves sandbox policy, execution limits, and C# semantics intact. Valid C# expressions keep their normal meaning; Extended mode adds extra forms around that contract.

When a Standard engine receives an Extended-only feature, Alder reports `ALDR0020` for language-mode-gated forms: "Use LanguageMode.Extended to enable non-standard syntax extensions." Some syntax is rejected earlier as ordinary parse failure when Standard mode has no matching grammar path.

## Extended surface at a glance

| Area | Extended forms | Boundary |
| --- | --- | --- |
| Operators | `**`, `**=`, `===`, `!==`, `<=>`, chained comparisons, `in`, `not in`, `like`, `not like`, `=~`, `!~`, `between`, `and`, `or`, `not` | Runtime evaluation only for Extended-only syntax; expression-tree export stays Standard. |
| Data shaping | Untargeted collection literals, collection spread, comprehensions, slices, inclusive and exclusive integer ranges | Collection literals materialize arrays; object spread is rejected. Standard C# `..` range expressions remain available in Standard mode. |
| Local expression syntax | `let`, `let ... in ...`, simple member destructuring, `if` expressions, `unless`, `until` | These forms lower to Alder's normal binding and control-flow model. |
| Built-ins | Bare math names, aggregate helpers, date/time unit members, `today()`, `now()` | Host variables shadow bare constants. Registered functions shadow call-form built-ins. Module names do not shadow call-form math helpers such as `sin(...)`. |

Every Extended form runs through Alder's parser, binder, sandbox, execution constraints, and backend-specific support rules. Runtime evaluation, compiled synchronous evaluation, expression-tree export, and Dynamic LINQ have distinct surfaces. Provider and export paths stay on Standard syntax unless an integration explicitly documents a translation.

## Why Extended mode exists

Runtime expressions often live closer to configuration than to source files. They appear in rules, dashboards, alerts, data filters, workflow conditions, and administrative tooling. In those settings, C# is a strong semantic foundation, but some C# forms are heavier than the host needs.

Extended mode addresses that gap with syntax that keeps common rule and query fragments compact:

```csharp
var engine = new AlderEngine(options => options.LanguageMode = LanguageMode.Extended);
engine.SetVariable("orders", orders);

var report = engine.Evaluate("""
    let open = orders.Where(o => o.Status == "Open") in
    new
    {
        Count = count(open),
        Revenue = sum(open.Select(o => o.Total)),
        Large = count(open.Where(o => o.Total between 500m and 5000m))
    }
    """);
```

The expression still uses CLR members, LINQ calls, lambdas, and decimal arithmetic. Extended syntax removes ceremony around the parts that rule authors tend to write repeatedly.

## Operator families

Extended mode adds operators for expression shapes that are common in rules and filters.

### Numeric and comparison operators

The power operator and compound power assignment call Alder's numeric runtime:

```csharp
engine.Evaluate("2 ** 8");
engine.Evaluate("""
    var scale = 3.0;
    scale **= 2;
    return scale;
    """);
```

Strict equality compares both type and value:

```csharp
engine.Evaluate("1 === 1");   // true
engine.Evaluate("1 === 1L");  // false
engine.Evaluate("1 !== 1L");  // true
```

The three-way comparison operator returns `-1`, `0`, or `1` and orders `null` before non-null values:

```csharp
engine.Evaluate(""" "alpha" <=> "beta" """);
engine.Evaluate("null <=> 5");
```

Chained comparisons evaluate the middle operands once and short-circuit:

```csharp
engine.Evaluate("0 <= score <= 100");
engine.Evaluate("""status == "Open" == isActive""");
```

### Membership and pattern predicates

`in` checks membership against an enumerable value. `not in` is parsed as the negated form:

```csharp
engine.SetVariable("allowedStatuses", new[] { "Open", "Pending" });

engine.Evaluate("""status in allowedStatuses""");
engine.Evaluate("""region not in new[] { "Blocked", "Retired" }""");
```

`like` uses SQL-style wildcard matching, where `%` matches any sequence and `_` matches one character. Regex metacharacters remain literal inside `like` patterns:

```csharp
engine.Evaluate("""CustomerName like "Acme%" """);
engine.Evaluate("""Code not like "TEMP_%" """);
```

`=~` and `!~` evaluate .NET regular expressions through Alder's built-in regex helper. The helper applies a one-second regex timeout in the current implementation:

```csharp
engine.Evaluate("""Email =~ "^[^@]+@example\\.com$" """);
engine.Evaluate("""Sku !~ "^TEST-" """);
```

`between ... and ...` lowers to an inclusive pair of comparisons:

```csharp
engine.Evaluate("Total between 100m and 500m");
```

### Boolean words

Extended mode accepts `and`, `or`, and `not` as expression-level boolean operators:

```csharp
engine.Evaluate("""IsActive and Total >= 100m""");
engine.Evaluate("""not IsDeleted""");
```

C# pattern combinators keep their Standard-mode behavior. The Extended word operators are an expression-authoring convenience for boolean expressions.

## Pipelines

The pipeline operator invokes the right-hand callable with the left-hand value as its single argument:

```csharp
engine.Evaluate("5 |> (x => x * 2)");
```

The right-hand side can be any Alder callable form: a lambda, a delegate value, a registered function, or another callable resolved through the runtime. Small transformations then read left to right:

```csharp
var engine = new AlderEngine(options =>
{
    options.LanguageMode = LanguageMode.Extended;
    options.Functions.Register("normalize", args =>
        args[0]?.ToString()?.Trim().ToUpperInvariant());
});

engine.Evaluate(""" "  open " |> normalize """);
```

Pipelines use the normal invocation path for the target callable. Method calls, registered functions, delegates, and modules keep their existing trust rules; cancellation and runtime diagnostics still apply.

## Ranges, slices, and comprehensions

Standard mode supports C# `..` range expressions and CLR indexing paths that consume `System.Range`. Extended mode adds explicit integer iteration forms for rule and scripting expressions:

```csharp
engine.Evaluate("(1..=5).Count()");  // inclusive
engine.Evaluate("(1..<5).Count()");  // exclusive end
```

When an Extended range is used as an enumerable, Alder generates an integer sequence. Reversed ranges produce an empty sequence. From-end ranges such as `^1..` are range values for indexing, not enumerable integer ranges.

Slice syntax works over supported indexed values:

```csharp
engine.SetVariable("values", new[] { 10, 20, 30, 40, 50 });

engine.Evaluate("values[1:4]");
engine.Evaluate("values[::2]");
engine.Evaluate(""" "alphabet"[2:6] """);
```

Comprehensions provide a compact projection/filter form over enumerable values:

```csharp
engine.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");
```

Comprehensions lower to LINQ-shaped calls and materialize arrays. They depend on the normal runtime type surface for `Where`, `Select`, and `ToArray`.

## Collection literals and spread

Extended mode accepts collection literals without requiring a target type:

```csharp
engine.Evaluate("[1, 2, 3]");
```

The binder infers an element type and the runtime materializes an array. Spread inserts the contents of an enumerable into a collection literal:

```csharp
engine.SetVariable("first", new[] { 1, 2 });
engine.SetVariable("second", new[] { 3, 4 });

engine.Evaluate("[..first, ..second, 5]");
```

Spread is supported in collection expressions. Object spread syntax such as `new { ..obj }` is rejected; structural projections use explicit members:

```csharp
engine.Evaluate("""new { Name = customer.Name, Total = order.Total }""");
```

This keeps projection shape visible at the expression boundary and avoids ambiguous object flattening rules.

## Local expression syntax

Extended mode adds `let` as an implicitly typed local declaration and as a `let ... in ...` expression form:

```csharp
engine.Evaluate("""
    let discounted = price * 0.9m in
    discounted >= minimum
    """);
```

`let-in` also supports simple member destructuring:

```csharp
engine.Evaluate("""
    let { Name, Total } = order in
    Name + ": " + Total.ToString()
    """);
```

`if` expressions give rule authors a compact branch form:

```csharp
engine.Evaluate("""if (score >= 90) "pass" else "review" """);
```

For statement-oriented scripts, Extended mode accepts `unless` and `until`, which lower to inverted `if` and `while` shapes:

```csharp
engine.Evaluate("""
    var attempts = 0;
    until (attempts == 3)
        attempts++;
    return attempts;
    """);
```

These forms use Alder's normal control-flow and statement-limit machinery.

## Built-ins for expression authors

Extended mode resolves selected bare math constants and functions through its normal identifier and call binding rules:

```csharp
engine.Evaluate("sin(pi / 2)");
engine.Evaluate("clamp(score, 0, 100)");
engine.Evaluate("round(amount, 2)");
```

User variables can shadow bare constants, and registered functions can shadow bare functions. Module names do not shadow call-form math helpers such as `sin(...)`; use explicit module access when a module and an Extended helper share a name.

Aggregate helpers operate over enumerable values and delegate to .NET semantics for supported numeric and comparable collections:

```csharp
engine.SetVariable("values", new[] { 10, 20, 30 });

engine.Evaluate("sum(values)");
engine.Evaluate("avg(values)");
engine.Evaluate("count(values)");
engine.Evaluate("min(values)");
engine.Evaluate("max(values)");
```

Date/time sugar maps numeric unit members to `TimeSpan` values and exposes clock helpers:

```csharp
engine.Evaluate("30.days");
engine.Evaluate("2.hours + 30.minutes");
engine.Evaluate("today()");
engine.Evaluate("now()");
```

Date arithmetic stays within .NET's type model. `DateTime + TimeSpan` works; `DateTime + DateTime` remains invalid.

## Compatibility boundaries

Extended mode is additive. It preserves the meaning of valid C# expressions and rejects operations that would invent semantics outside Alder's contract.

Several boundaries are intentional:

- negative indexing keeps normal CLR indexing behavior and throws
- `string * int` remains invalid
- bare `it` and `_` are ordinary identifiers; lambdas must still be explicit
- object spread is rejected
- Extended built-ins do not override user-defined variables or registered functions
- Standard mode remains the default engine policy

Those boundaries are part of the compatibility model. Extended mode improves expression ergonomics while staying inside Alder's C# and CLR runtime model.

## Execution and export

Interpreted evaluation supports Extended mode. The compiled backend also evaluates supported Extended syntax through the bound-tree compilation path when `UseCompiler()` is configured on a JIT-capable runtime. `EvaluateAsync(...)` remains interpreted, including for Extended expressions.

| Surface | Extended syntax support |
| --- | --- |
| Interpreted `Evaluate(...)` | Yes. |
| Compiled `Evaluate(...)` | Yes, for shapes the compiled backend can lower. |
| `EvaluateAsync(...)` | Yes. It runs through the interpreter. |
| `EvaluateWithTrace(...)` | Yes. It runs through the interpreter. |
| `ParseAsExpression<TDelegate>(...)` | No. Export parses with Standard syntax. |
| `IQueryable` Dynamic LINQ export | No. Provider-facing export uses Standard syntax. |

Expression-tree export has a narrower contract. `ParseAsExpression<TDelegate>(...)` parses in Standard mode regardless of the engine's `LanguageMode`, and it rejects Extended-only syntax such as `**`. Dynamic LINQ prepares provider-facing expression fragments through the same Standard-mode parser before exporting expression trees.

Runtime evaluation can execute Alder-specific helpers in process. Provider export must produce ordinary LINQ expression trees that external query providers can understand. Use Standard syntax for `IQueryable` and expression-tree interop, and keep Extended syntax for in-process evaluation unless the host explicitly translates the resulting behavior itself.

## Safety and governance

Extended mode expands the accepted language surface. Treat that choice as part of the host governance model.

Security policy remains the primary enforcement mechanism. `SandboxOptions` still controls method calls, construction, assignment, property reads, writes, static access, trusted types, denied types, and collection-size limits. Execution constraints still bound statement counts, loop iterations, timeouts, and cancellation.

Pipelines, comprehensions, ranges, aggregate helpers, regex predicates, and collection literals can increase how much work an expression asks the runtime to do. Hosts that accept user-authored expressions should pair Extended mode with a sandbox, execution limits, validation, and review tooling appropriate to the trust level of those users.

For public or multi-tenant expression authoring, a common policy is:

- use `SandboxOptions.Safe()` or a stricter custom policy
- set statement, loop, timeout, and collection-size limits
- validate stored expressions before activation
- version the host's allowed expression surface
- document which Extended forms are allowed in that product context

## Migration guidance

Move from Standard to Extended when expression authors repeatedly reach for concise rule syntax, date arithmetic, collection shaping, or readable predicate forms. The migration is usually configuration-first:

```csharp
var engine = new AlderEngine(options =>
{
    options.LanguageMode = LanguageMode.Extended;
    options.Sandbox = SandboxOptions.Safe();
    options.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(2)
    };
});
```

Evaluate the stored expression corpus under the new mode and look for name collisions. Variables can shadow bare constants, and registered functions can shadow call-form built-ins. Module names with the same spelling as call-form math helpers require explicit module access. The main migration risk is syntactic: words such as `like`, `between`, `unless`, and `until` gain feature meaning in positions where Extended mode recognizes them.

Keep Standard mode for provider-exported expressions, C# compatibility test suites, and integrations where the accepted syntax must remain conservative. Use Extended mode for in-process rule evaluation, scripting-style configuration, internal tooling, and expression surfaces where readability for non-compiler specialists is a product requirement.

## Related pages

- [Execution and reuse](../operations/execution-and-reuse.md)
- [Compiled backend](./compiled-backend.md)
- [Dynamic LINQ](./dynamic-linq.md)
- [Security model](../operations/security-model.md)
- [Configuration](../reference/configuration.md)
