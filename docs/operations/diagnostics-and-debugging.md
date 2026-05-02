---
title: Diagnostics and debugging
description: How Alder reports parse, bind, validation, compilation, export, and runtime failures, and how hosts should debug and operate expressions in production.
---

# Diagnostics and debugging

Alder reports problems through structured diagnostics. The same diagnostic model covers parsing, binding, validation, sandbox checks, execution limits, runtime dispatch, compiled execution, expression-tree export, and AOT generated-dispatch failures. Hosts can treat expression failures as data: code, message, source span, line, column, severity, and phase context.

Parse and validate before activation. Execute only expressions that were accepted under the same engine policy they will use in production. When execution fails, log the diagnostic code and source location before the raw exception text. That gives stored expressions, user-authored rules, and provider-facing query fragments a stable operational contract.

## Diagnostic model

`AlderDiagnostic` is the structured diagnostic record:

```csharp
public sealed record AlderDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    DiagnosticCode? Code = null,
    TextSpan Span = default,
    int? Line = null,
    int? Column = null);
```

`Code` is the machine-readable identifier. `FormattedCode` renders it as `CS0103`, `ALDR0003`, or another public diagnostic ID. `Span` is the zero-based source range. `Line` and `Column` are one-based when Alder can resolve the position from source text.

The exception path uses `AlderException`. It carries the same diagnostics in `Diagnostics` and exposes first-diagnostic convenience properties:

```csharp
catch (AlderException ex)
{
    var code = ex.FormattedCode;
    var line = ex.Line;
    var column = ex.Column;
    var span = ex.Span;
    var diagnostics = ex.Diagnostics;
}
```

`AlderException.Message` includes the formatted code and message. Hosts should prefer `Diagnostics` for logs, APIs, and UI rendering because it preserves multiple validation errors and exact source ranges.

## Codes

Alder uses Roslyn-compatible `CS` codes when the failure corresponds to C# behavior. Undefined names report `CS0103`, invalid binary operators report `CS0019`, missing members report `CS1061`, unsupported expression-tree features report `CS7053`, and ordinary syntax errors use the relevant `CS10xx` or parser code.

`ALDRxxxx` codes identify Alder-specific runtime, integration, security, and deployment failures. The important families are:

- `ALDR0001`: strict compiled execution could not produce or invoke a compiled delegate.
- `ALDR0002`: binding failed through an internal fallback boundary. The exception may still carry more specific diagnostics in `Diagnostics`.
- `ALDR0003`: an explicit compiled wrapper is stale because the visible variable type surface changed after compilation.
- `ALDR0010`-`ALDR0012`: compiled/export API shape errors, such as invalid `ParseAsExpression` delegate type or parameter-count mismatch.
- `ALDR0020`: an Extended-mode feature was used under Standard mode.
- `ALDR0100`-`ALDR0108`: sandbox policy blocked a method call, assignment, property access, construction, type access, or reflection type.
- `ALDR0200`-`ALDR0203`: execution constraints were exceeded.
- `ALDR0300`-`ALDR0318`: runtime semantic and dispatch failures, including null member access, failed invocation, unsupported runtime shapes, module instance resolution, and authoritative generated-mode misses.
- `ALDR0400`-`ALDR0406`: Extended-language runtime failures such as invalid slicing, spread placement, chained comparison support, and projection materialization.

Code-first handling should group by family only when that is operationally useful. User-facing messages should still show the exact code and source location.

## Parse, bind, and runtime failures

Parsing turns source text into an `AlderExpression`. Parse failures are syntax and lexical failures: incomplete expressions, invalid literals, unterminated strings, unexpected tokens, and excessive nesting. `Parse(...)` throws `AlderException`; `TryParse(...)` returns `false` with parse diagnostics.

```csharp
if (!engine.TryParse(source, out var parsed, out var parseDiagnostics))
{
    return Reject(source, parseDiagnostics);
}
```

Binding is the semantic boundary. The binder resolves names, types, members, overloads, conversions, assignment targets, control-flow legality, pattern compatibility, and language-mode restrictions. Binding failures include `CS0103`, `CS1061`, `CS0029`, `CS0121`, `CS0246`, and `ALDR0020`.

`TryValidate(...)` is the host-facing validation probe for syntax and binding. It parses, binds, collects semantic diagnostics, reports multiple unbound identifiers when possible, and does not execute user code:

```csharp
if (!engine.TryValidate(source, out var diagnostics))
{
    return Reject(source, diagnostics);
}
```

Runtime failures occur after the expression is bound and evaluation has started. They include null member access, method invocation failures, sandbox rejections, execution limits, failed casts that depend on runtime values, AOT generated-dispatch misses, and exceptions thrown by host methods. These failures normally surface through `Evaluate(...)`, `EvaluateAsync(...)`, compiled wrappers, or expression-tree delegates.

## Exception types

Most Alder-controlled failures use `AlderException`. It is the common exception type for parse, bind, validation, security, runtime semantic, compilation, export, and generated-dispatch diagnostics.

Execution limits use `AlderExecutionLimitException`, a subclass of `AlderException`. It adds operational fields:

```csharp
catch (AlderExecutionLimitException ex)
{
    LogLimit(
        ex.LimitType,
        ex.LimitValue,
        ex.ActualValue,
        ex.StatementsExecuted,
        ex.ElapsedTime);
}
```

Cancellation and disposal remain ordinary .NET lifecycle failures. `TryEvaluate(...)`, `TryValidate(...)`, and other `Try` APIs rethrow `OperationCanceledException` and `ObjectDisposedException`. They return `false` for expression failures, not for canceled work or use-after-dispose.

Host code invoked by an expression can throw its own exceptions. When Alder wraps a runtime operation with an Alder diagnostic, the original exception may be available as `InnerException`. Log both the Alder diagnostic and the underlying exception type when host code is part of the failure.

## Source locations

`TextSpan` stores offsets as `[Start..End)`. The span is zero-based and end-exclusive. `Line` and `Column` are one-based, matching editor and log conventions.

Interpreted evaluation enriches runtime `AlderException` instances from the active bound expression frame. Async interpreted evaluation uses the same source-enrichment path. Binding errors in compiled evaluation are also enriched. Runtime errors thrown inside compiled delegates have a narrower location model because compiled IL does not preserve the same evaluator frame boundary; Alder enriches some compiled exceptions from the root expression when no span exists.

For diagnostic displays, use span information as the canonical range and line/column as the human-facing entry point:

```csharp
foreach (var diagnostic in diagnostics)
{
    Console.WriteLine(
        $"{diagnostic.FormattedCode} at {diagnostic.Line}:{diagnostic.Column}: {diagnostic.Message}");
}
```

When a diagnostic has no populated line or column, hosts can resolve `diagnostic.Span.Start` against the stored source text with `SourceText`.

## Validation before activation

Stored expressions should move through an activation pipeline:

1. Parse or validate under the production engine configuration.
2. Reject expressions with diagnostics before they become active.
3. Store the original source text, expression identifier, engine policy version, and validation diagnostics.
4. Activate only after the host has accepted the expression under the same language mode, sandbox, type registrations, functions, modules, AOT contexts, and compiler setting used for execution.
5. Revalidate when the host changes the expression-facing type surface or policy.

Validation is not execution. It catches syntax and semantic failures without invoking host methods, enumerating data, mutating state, or hitting execution limits. Runtime failures still need production handling because they depend on values, nulls, provider behavior, host method exceptions, timeouts, cancellation, and deployment metadata.

Use `TryValidate(...)` for activation. Use `TryEvaluate(...)` only when a host wants a boolean success result from actual execution and does not need structured diagnostics. `TryEvaluate(...)` intentionally returns only `false` and a default result on ordinary failures.

## Tracing

`EvaluateWithTrace(...)` executes through the interpreter with tracing enabled, even when the engine has a compiler configured. It returns `EvaluationTraceResult`:

```csharp
var trace = engine.EvaluateWithTrace(
    "price * (1 - discount) + tax",
    new Dictionary<string, object?>
    {
        ["price"] = 100m,
        ["discount"] = 0.15m,
        ["tax"] = 8m
    });

if (trace.Error != null)
{
    LogTrace(trace.Tree, trace.Error);
}
```

Each `TraceNode` records the bound node kind, source substring, span, value, value type, optional description, child nodes, and error details. Binary and unary nodes expose operator descriptions. Identifier nodes expose variable names. Member access nodes expose member descriptions. When evaluation fails, the trace contains the partial tree and the failed node records `ErrorCode` and `ErrorMessage`.

Trace data is structured enough for rule editors and support tooling:

```csharp
var root = trace.Tree;
var discounted = root.Children[0];
var discount = discounted.Children[1];

Console.WriteLine(root.NodeKind);       // BinaryOperator
Console.WriteLine(root.Source);         // price * (1 - discount) + tax
Console.WriteLine(root.Span.Start);     // source offset
Console.WriteLine(root.ValueType);      // System.Decimal, System.Double, ...
Console.WriteLine(root.Description);    // operator or member description

Console.WriteLine(discount.Source);     // 1 - discount
Console.WriteLine(discount.Value);      // 0.85
Console.WriteLine(discount.ValueType);  // System.Decimal
Console.WriteLine(discount.Span.End);   // end-exclusive source offset
```

Failed evaluations keep the partial tree:

```csharp
engine.SetVariable("x", 0);

var failing = engine.EvaluateWithTrace("10 / x");
var failedRoot = failing.Tree;

Console.WriteLine(failing.Error?.GetType().Name); // DivideByZeroException
Console.WriteLine(failedRoot.Source);             // 10 / x
Console.WriteLine(failedRoot.ErrorCode);          // DivideByZeroException or Alder code
Console.WriteLine(failedRoot.ErrorMessage);       // underlying error message
Console.WriteLine(failedRoot.Children[1].Source); // x
Console.WriteLine(failedRoot.Children[1].Value);  // 0
```

Tracing is a debugging tool, not the hot-path execution model. Use it to diagnose a stored expression, reproduce a support case, or build an internal expression inspector. Keep routine production evaluation on `Evaluate(...)`, `EvaluateAsync(...)`, compiled wrappers, or Dynamic LINQ plans.

## Compiled diagnostics

When `UseCompiler()` is configured, synchronous `Evaluate(...)` uses the compiled backend. Parse and bind diagnostics still follow the normal model. Compilation adds two operational cases.

`TryCompile(...)` is a probe. It returns `false` when no compiler is configured or when compilation cannot produce an invocable delegate. `Compile(AlderExpression)` and compiled extension APIs throw `ALDR0001` when strict compilation fails:

```csharp
var expression = engine.Parse("x + 1");

try
{
    engine.Compile(expression);
}
catch (AlderException ex) when (ex.ErrorCode == DiagnosticCode.ALDR0001)
{
    LogCompilationFailure(expression.Source, ex.Diagnostics);
}
```

`AlderCompiledExpression<T>` captures the parent context type version at compile time. Value changes remain visible. Type-surface changes invalidate the wrapper. Invoking a stale wrapper throws `ALDR0003`:

```csharp
var compiled = engine.Compile<int>("x + 1");
engine.SetVariable<int>("x", 1);
_ = compiled.Invoke();

engine.SetVariable<string>("x", "one");
// compiled.Invoke() throws ALDR0003
```

Normal `Evaluate(AlderExpression)` is more adaptive: it recompiles when the cached compiled artifact is stale. Explicit wrappers are stricter because their delegate shape is part of the host integration contract.

## Export and provider diagnostics

Expression-tree export has its own boundary. `ParseAsExpression<TDelegate>(...)` requires a generic delegate shape. Parameterized exports require lambda input; zero-parameter delegate exports can use body-only input. Shape errors report `ALDR0010` or `ALDR0011`. Parameter-count and delegate conversion failures use Roslyn-style delegate diagnostics where appropriate.

The exported tree supports a narrower set of expression-shaped constructs than Alder runtime evaluation. Unsupported export nodes report `CS7053`, for example block-bodied lambdas, assignments, nested lambdas, interpolated strings, named arguments, `out` arguments, dynamic member access, and other runtime-only shapes.

`TryParseAsExpression<TDelegate>(...)` returns `false` with diagnostics for parsing, binding, and export failures:

```csharp
if (!engine.TryParseAsExpression<Func<Order, bool>>(
        "o => { return o.Total > 100m; }",
        out var tree,
        out var diagnostics))
{
    return Reject(source, diagnostics);
}
```

Provider translation is a downstream boundary. Alder can produce a valid `Expression<TDelegate>` and an `IQueryable` provider can still reject it because the provider cannot translate the tree shape. Treat those as provider diagnostics or provider exceptions. Log the Alder expression source and exported-tree context, then report the provider's error separately.

## AOT generated-dispatch diagnostics

In JIT deployments, Alder tries generated dispatch first and then uses reflection fallback when metadata and dynamic code are available. In authoritative generated mode, missing generated dispatch is an Alder diagnostic:

- `ALDR0316`: a member is unavailable in authoritative generated mode.
- `ALDR0317`: a method is unavailable in authoritative generated mode.
- `ALDR0318`: a constructor is unavailable in authoritative generated mode.

These errors mean the expression reached a CLR operation that must be represented by generated metadata. Register the relevant type in an `AlderTypeContext`, make the expression-facing member public and supported by generated dispatch, or change the expression surface. For stored expressions, include the generated-context version in activation records so a production failure can be tied to the metadata set that was deployed.

## Host logging

Production logs should preserve the expression identity and diagnostic structure without treating user-authored source as the only key. A useful record includes:

- expression ID, rule ID, tenant ID, or configuration record key
- engine policy version or deployment version
- language mode and whether compiled execution was configured
- whether the failure happened during parse, validation, export, compilation, trace, or runtime execution
- `FormattedCode`, severity, message, span, line, and column for each diagnostic
- execution-limit fields for `AlderExecutionLimitException`
- provider name and provider exception for `IQueryable` translation failures
- AOT generated-context version when running in generated-authoritative deployments

Keep raw source logging under the host application's data policy. Expression text can contain business rules, tenant identifiers, field names, constants, or user-provided literals. When source text is sensitive, log a stable expression ID and a small source excerpt around the diagnostic span.

## Debugging stored expressions

Stored expressions should be debugged against the same engine configuration that executes them. Differences in language mode, sandbox policy, registered types, functions, modules, variable types, generated contexts, or compiler configuration change the diagnostic surface.

A practical debugging loop is:

1. Load the stored source and its activation metadata.
2. Recreate the production engine policy.
3. Run `TryValidate(...)` and inspect every diagnostic.
4. If validation succeeds, run `EvaluateWithTrace(...)` with a representative variable set.
5. If compiled execution fails, reproduce with interpreted `EvaluateWithTrace(...)` to isolate expression semantics from compiled backend behavior.
6. If provider execution fails, run `TryParseAsExpression(...)`, inspect the exported expression, then reproduce against the provider.
7. If AOT execution fails with `ALDR0316`, `ALDR0317`, or `ALDR0318`, inspect the generated context for the runtime type and operation shape.

The distinction between validation and representative execution matters. Validation confirms the expression is syntactically and semantically acceptable under a policy. Trace execution explains what happened for one value set. Provider execution shows whether a specific downstream translator accepts the exported tree. AOT execution checks whether the deployed metadata surface contains the runtime shapes the expression reaches.
