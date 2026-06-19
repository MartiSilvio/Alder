# Alder: C# Expression Engine for .NET

<p align="center">
  <a href="https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml"><img src="https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml/badge.svg?branch=master" alt=".NET CI"></a>
  <a href="https://www.nuget.org/packages/Alder"><img src="https://img.shields.io/nuget/v/Alder?logo=nuget&logoColor=white" alt="NuGet"></a>
  <img src="https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet&logoColor=white" alt=".NET 8+">
  <img src="https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white" alt=".NET Standard 2.0">
  <img src="https://img.shields.io/badge/NativeAOT-generated%20dispatch-brightgreen" alt="NativeAOT generated dispatch">
  <img src="https://img.shields.io/badge/dependencies-none-brightgreen" alt="No third-party runtime dependencies">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>

<p align="center">
  <b>An embeddable C# expression evaluator with compiler-style binding for your .NET types.</b><br>
  <sub>Interpreter-first execution with optional compiled delegates, Dynamic LINQ, expression-tree export, host-controlled security, and NativeAOT generated dispatch.</sub>
</p>

<p align="center">
  C# semantics&nbsp; · &nbsp;AOT-aware&nbsp; · &nbsp;Async&nbsp; · &nbsp;Dynamic LINQ&nbsp; · &nbsp;Zero dependencies
</p>

Alder evaluates C# expressions and statement blocks at runtime against the objects your code supplies. Before execution, the parser and binder resolve types, overloads, conversions, and control flow. The same pipeline applies your security policy and execution limits.

The interpreter is the baseline execution path. JIT-capable hosts can opt into compiled delegates. Query providers can receive `Expression<Func<...>>` trees for generic `Func` delegate shapes. NativeAOT hosts can route registered member access through generated dispatch metadata.

Standard mode follows ECMA-334 7th edition semantics. It covers lambdas and query syntax, pattern matching, async code, iterators, and user-defined conversions and operators. The interpreter and compiled backend share parser and binder. They also share validation, security, and limits. Backend behavior is held to parity; unintended divergence is treated as a defect.

## At a glance

- **C# expressions and statements at runtime.** Standard mode follows ECMA-334 7th edition for runtime expressions and statement blocks. It includes lambdas and queries, pattern matching, async code, iterators, and user-defined conversions and operators. [Support matrix](docs/reference/language/standard-mode-language-support.md).
- **Native AOT through generated dispatch.** A source generator emits reflection-free dispatch from `[AlderRegistered]` declarations. The interpreter runs under AOT without actionable trim/AOT warning sites. Register the types and methods your expressions reach; what isn't registered falls back to reflection on JIT and surfaces an explicit `ALDR0316/0317/0318/0319` diagnostic under AOT that propagates to the host rather than being caught by the script.
- **Async inside expressions.** `EvaluateAsync` awaits inside the expression itself. `await Task<T>`, iterators (`yield return`/`yield break`), cancellation, and execution limits all flow through the interpreter.
- **Shared semantics across surfaces.** Expression evaluation, Dynamic LINQ (`WhereDynamic`, `OrderByDynamic`), and `Expression<TDelegate>` export go through one parser and binder. They use the same security policy and execution limits.

Targets `net8.0` and `netstandard2.0`. Zero third-party runtime dependencies.

## Install

```bash
dotnet add package Alder
```

The `Alder` package is the single public package. It includes the runtime, optional `Alder.Compiled` APIs for JIT-capable consumers, and the source generator for AOT generated dispatch metadata.

## A first look

Alder is meant for host-owned rules: your application supplies the data and policy, while the rule author supplies the C# fragment. This example calculates a cart total from everyday money fields.

<!-- test: RootReadme_CartFirstLook_EvaluatesTotalAndShippingRule -->
```csharp
using Alder;

var cart = new
{
    Subtotal = 120m,
    Discount = 20m,
    Tax = 8m,
    ItemCount = 3,
    PostalCode = "94107"
};

var total =
    "cart.Subtotal - cart.Discount + cart.Tax"
    .Evaluate<decimal>(new { cart });                     // 108m
```

When a rule needs local variables or branching, use a statement block. This one turns the same cart into a shipping label:

<!-- test: RootReadme_CartFirstLook_EvaluatesTotalAndShippingRule -->
```csharp
var shipping = """
    var total = cart.Subtotal - cart.Discount + cart.Tax;

    if (total >= freeShippingMinimum)
        return "free-shipping";

    if (cart.ItemCount > 20)
        return "bulk-review";

    return "standard";
    """.Evaluate<string>(new { cart, freeShippingMinimum = 100m }); // free-shipping
```

## End-to-end integration

A configured `AlderEngine` lets a host validate, execute, trace, compile, and export rules through one semantic pipeline.

<!-- test: EngineConfiguration_CanCombinePolicyAndCompilerSettings -->
```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options =>
{
    options.UseCompiler();
    options.Security = SecurityOptions.Trusted();
    options.Modules.Register<TaxModule>("tax");
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

Validate a stored rule before activating it. Validation uses the identifiers already registered on the engine, so register representative variable shapes first:

<!-- test: RootReadme_CartRuleValidationEvaluationAndTrace_UseTypedCartShape -->
```csharp
var totalRule =
    "cart.Subtotal - cart.Discount + cart.Tax";
var sampleCart = new Cart
{
    Subtotal = 120m,
    Discount = 20m,
    Tax = 8m,
    ItemCount = 3,
    PostalCode = "94107"
};

engine.SetVariable<Cart>("cart", sampleCart);

if (!engine.TryValidate(totalRule, out var diagnostics))
    return diagnostics;
```

Run the validated rule against each cart:

<!-- test: RootReadme_CartRuleValidationEvaluationAndTrace_UseTypedCartShape -->
```csharp
var total = engine.Evaluate<decimal>(
    totalRule,
    new { cart = sampleCart });
```

Trace a cart calculation without changing the rule:

<!-- test: RootReadme_CartRuleValidationEvaluationAndTrace_UseTypedCartShape -->
```csharp
engine.SetVariable<Cart>("cart", sampleCart);
var trace = engine.EvaluateWithTrace(totalRule);

Console.WriteLine(trace.Result);       // 108m
Console.WriteLine(trace.Tree.Source);
// cart.Subtotal - cart.Discount + cart.Tax
```

Call host services from the same rule environment when the host exposes a module:

<!-- test: RootReadme_AsyncTaxModule_ComputesCartTotal -->
```csharp
var totalWithLiveTax = await engine.EvaluateAsync<decimal>(
    """
    var discounted = cart.Subtotal - cart.Discount;
    var tax = await tax.CalculateAsync(cart.PostalCode, discounted);
    return discounted + tax;
    """,
    new { cart = sampleCart });
```

Use the same rule vocabulary to filter carts in a query provider:

<!-- test: RootReadme_DynamicLinq_CartReviewPipeline_FiltersAndProjectsRows -->
```csharp
var cartsForReview = await db.Carts
    .WhereDynamic(
        engine,
        "Subtotal - Discount >= @0",
        100m)
    .OrderByDynamic<Cart, int>(engine, "ItemCount")
    .SelectDynamic<Cart, CartReviewRow>(
        engine,
        "new { Id, Subtotal, Discount, ItemCount }")
    .ToListAsync();
```

## Language surface

Standard mode evaluates C# at the expression and statement-block level under ECMA-334 7th edition semantics. Type and member declarations, namespaces, attributes, preprocessor directives, and unsafe code are out of scope. The full support matrix lives in [Standard mode language support](docs/reference/language/standard-mode-language-support.md).

[Extended mode](docs/concepts/extended-language-mode.md) adds scripting syntax on the same parser. This campaign rule focuses on the predicate-heavy side of that surface: regex predicates, SQL-style comparisons, membership checks, and word-form boolean operators. The reference page covers the rest of Extended mode, including pipelines, ranges, date arithmetic, and aggregate helpers.

<!-- test: RootReadme_ExtendedCampaignDecision_UsesPredicateOperators -->
```csharp
using Alder;

using var rules = new AlderEngine(options =>
    options.LanguageMode = LanguageMode.Extended);

var cart = new Cart
{
    Subtotal = 160m,
    Discount = 20m,
    ItemCount = 3,
    PostalCode = "94107",
    CouponCode = "SHIP-2026",
    Channel = "mobile",
    Region = "bay-area"
};

var campaignDecision = rules.Evaluate<string>(
    """
    let net = cart.Subtotal - cart.Discount in
    if (cart.CouponCode =~ "^SHIP-[0-9]{4}$"
        and net between 100m and 500m
        and cart.ItemCount between 1 and 20
        and cart.PostalCode like "94%"
        and cart.Channel in new[] { "web", "mobile" }
        and cart.Region not in new[] { "blocked", "manual-review" })
        "auto-approve"
    else if (net >= 500m or cart.ItemCount > 20)
        "manual-review"
    else
        "standard"
    """,
    new { cart });
```

## The expression engine

Alder's binder is the semantic boundary between syntax and execution. It resolves type relationships, overloads, member targets, assignment legality, and control-flow shape. It also records where runtime dispatch is still required.

Execution paths consume that bound model while preserving the same security policy and execution limits.

The **interpreter** evaluates the bound tree directly. It is the default synchronous path, the engine for `EvaluateAsync(...)`, and the path used under NativeAOT and trimming-sensitive deployments.

The **compiled backend** lowers the same bound tree to a reusable delegate through `System.Linq.Expressions`. With `UseCompiler()` configured, synchronous `Evaluate(...)` uses that delegate path and recompiles when the relevant type surface changes.

Both backends share parser and binder. They also share validation rules, security policy, execution limits, and language semantics. Backend behavior is held to parity; unintended divergence is treated as a defect.

Architecture: [Architecture](docs/concepts/architecture.md), [Binding system](docs/concepts/binding-system.md), [Execution model](docs/reference/execution-model.md).

## Async expressions

`EvaluateAsync(...)` runs through the interpreter and awaits expression-level asynchronous work directly inside the expression itself.

<!-- test: RootReadme_AsyncTaxModule_ComputesCartTotal -->
```csharp
using var engine = new AlderEngine(options =>
{
    options.Modules.Register<TaxModule>("tax");
});

var cart = new Cart
{
    Subtotal = 120m,
    Discount = 20m,
    PostalCode = "94107"
};

var totalWithLiveTax = await engine.EvaluateAsync<decimal>(
    """
    var discounted = cart.Subtotal - cart.Discount;
    var tax = await tax.CalculateAsync(cart.PostalCode, discounted);
    return discounted + tax;
    """,
    new { cart });
```

`await` cooperates with `CancellationToken` and execution constraints. Long-running expressions surface `OperationCanceledException` or `AlderExecutionLimitException` at expression-level checkpoints.

Iterators (`yield return`/`yield break`) are first-class inside the same evaluation tree.

See [Async execution](docs/concepts/async-execution.md).

## Dynamic LINQ

Dynamic LINQ adapts runtime fragments into LINQ pipelines for in-memory collections, query providers, and async streams.

<!-- test: RootReadme_DynamicLinq_CartReviewPipeline_FiltersAndProjectsRows -->
```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options => options.UseCompiler());

var page = carts
    .WhereDynamic(engine, "Subtotal - Discount >= @0", 100m)
    .OrderByDynamic<Cart, int>(engine, "ItemCount")
    .SelectDynamic<Cart, CartReviewRow>(
        engine,
        "new { Id, Subtotal, Discount, ItemCount }")
    .TakeDynamic(25)
    .ToList();
```

`IEnumerable<T>` executes in process through compiled delegates. `IQueryable<T>` exports expression trees and calls the matching `Queryable` operators. Provider translation belongs to the provider. `IAsyncEnumerable<T>` streams in process through compiled delegates during asynchronous enumeration.

Operator coverage spans filters and ordering; projection and flattening; grouping, joins, and group joins; paging and set operations; element operators, quantifiers, and aggregates.

`DynamicQueryPlan` captures a parsed fragment for reuse across operators, provider-backed query assembly, validation, delegate execution, and expression-tree export.

The full operator matrix is in [Use Dynamic LINQ](docs/guides/use-dynamic-linq.md).

## LINQ expression-tree export

Alder produces `Expression<TDelegate>` trees that LINQ providers translate.

<!-- test: RootReadme_ParseAsExpression_CartPredicate_ExportsProviderShape -->
```csharp
using System.Linq.Expressions;
using Alder.Compiled;

Expression<Func<Cart, bool>> predicate =
    engine.ParseAsExpression<Func<Cart, bool>>(
        """
        cart => cart.Subtotal - cart.Discount >= 100m &&
            cart.ItemCount > 0
        """);
```

EF Core can translate the verified shapes Alder emits for filters and ordering; projections and grouping; flattening, joins, and group joins; paging; null-coalescing predicates; string methods; and `EF.Property<T>(...)`.

The export surface is narrower than runtime evaluation. Alder rejects statement-bodied lambdas, assignments, dynamic call shapes, collection expressions, and reflection-leaking members before provider translation begins.

Details in [Compiled backend](docs/concepts/compiled-backend.md).

## Security policy

`SecurityOptions` controls expression authority. Alder defaults to trusted execution for ease of adoption. `Trusted()` enables every gated operation for trusted expressions.

Hosts that evaluate user-authored or tenant-authored expressions should choose an explicit `new SecurityOptions { ... }` policy and name each allowed operation directly.

Allow and deny lists cover concrete types and namespaces. Reflection metadata is blocked at evaluation boundaries so expressions can compare types and read names without escaping into reflective discovery or invocation.

<!-- test: SecurityPolicyExplicitOptions_ConfigureOperationPolicy -->
```csharp
options.Security = new SecurityOptions
{
    AllowPropertyRead = true,
    AllowStaticPropertyRead = true,
    AllowStaticFieldRead = true,
    AllowConstruction = true,
    TrustedTypes = [typeof(StringBuilder)],
};
```

The default deny surface covers reflection and dynamic code generation; file and process access; networking and interop; security-sensitive runtime services; and data access. The boundary is in-process. Alder constrains expression behavior inside the host runtime; it does not provide process or operating-system isolation.

See [Security model](docs/operations/security-model.md).

## Execution limits

`ExecutionConstraints` bounds work. Limits apply across the interpreter, the compiled backend, and generated dispatch.

<!-- test: RootReadme_ExecutionConstraints_ConfiguresWorkLimits -->
```csharp
options.Constraints = new ExecutionConstraints
{
    MaxStatements     = 10_000,
    MaxLoopIterations = 1_000,
    MaxTimeout        = TimeSpan.FromSeconds(2),
};
```

When a limit is exceeded, Alder throws `AlderExecutionLimitException`. The exception reports the limit type and configured value, the observed value, executed statement count, and elapsed time. `SecurityOptions.MaxCollectionSize` bounds collection-producing results separately.

## NativeAOT

Alder supports NativeAOT through interpreted evaluation backed by generated dispatch metadata. The source generator reads `[AlderRegistered]` declarations on a partial `AlderTypeContext` and emits reflection-free dispatch code.

<!-- test: GeneratedContext_ProvidesReflectionFreeMemberAndMethodDispatch -->
```csharp
using Alder.Aot;

[AlderRegistered(typeof(Cart))]
[AlderRegistered(typeof(TaxModule))]
public partial class RulesAotContext : AlderTypeContext;
```

<!-- test: GeneratedContext_ProvidesReflectionFreeMemberAndMethodDispatch -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

JIT deployments adopt generated coverage incrementally because reflection fallback remains available. NativeAOT deployments use generated dispatch as the authoritative route for reflection-sensitive operations.

See [Deploy with NativeAOT](docs/guides/nativeaot-deployment.md) and [AOT and generated dispatch](docs/operations/aot-and-generated-dispatch.md).

## Reuse and performance

When the same rule runs over many carts, parse it once and evaluate the parsed expression for each cart.

`AlderExpression` preserves parsed syntax across evaluations and engines. The engine caches bound work for calls against the same context type surface.

JIT-capable hosts can move deeper later with `Compile<TDelegate>(...)` for typed synchronous delegates. `DynamicQueryPlan` does the same kind of reuse for Dynamic LINQ fragments across operators, expression-tree export, and delegate execution.

<!-- test: RootReadme_ParseCartTotalOnce_ReusesRuleForManyCarts -->
```csharp
var totalRule = engine.Parse(
    "cart.Subtotal - cart.Discount + cart.Tax");

var totals = new List<decimal>();

foreach (var cart in carts)
{
    var total = engine.Evaluate<decimal>(
        totalRule,
        new { cart });

    totals.Add(total);
}
```

Cache invalidation is conservative. Value-only changes keep prior work. Declared-type changes rebind because overload resolution, conversion legality, and the resolved-versus-dynamic boundary can shift.

Benchmarks for the parser, binder, interpreter, compiled backend, and Dynamic LINQ live under [`benchmarks/Alder.Benchmarks`](benchmarks/Alder.Benchmarks). Run them locally to compare against your workload:

```bash
dotnet run -c Release --project benchmarks/Alder.Benchmarks
```

See [Execution and reuse](docs/operations/execution-and-reuse.md).

## Host integration

Hosts shape Alder's expression-facing world through `AlderOptions`. Variables can come from typed values, anonymous objects, dictionaries, positional `@0` placeholders, or per-call input objects.

Host APIs reach expressions through global functions and named modules, attributed registration such as `[AlderModule]` and `[AlderFunction]`, registered assemblies or namespaces, and extension-method containers.

Modules resolve through `IServiceProvider`, so module-backed expressions obtain instance targets from the host container. Child engines inherit configuration with isolated local variable state.

<!-- test: RootReadme_HostIntegration_ConfiguresModulesFunctionsTypesAndExtensions -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.Register<TaxModule>("tax");
    options.Functions.Register("hash", args => Sha256((string)args[0]!));
    options.Types.AddAssembly(typeof(Money).Assembly);
    options.Types.AddNamespace("Billing");
    options.Types.AddExtensionMethods(typeof(MoneyExtensions));
});
```

See [Configuration](docs/reference/configuration.md), [Register types and extension methods](docs/guides/type-registration.md), [Expose functions and modules](docs/guides/functions-and-modules.md), and [Choose variables and child engines](docs/guides/variables-context-and-child-engines.md).

## Diagnostics and tracing

Parsing and binding failures report as `AlderException`. Validation, compilation, export, and runtime failures use the same exception type. Diagnostics carry codes (Roslyn `CS####` where applicable, `ALDR####` otherwise), human-readable messages, and source spans.

<!-- test: TryValidate_ReturnsStructuredDiagnosticsWithoutExecuting -->
```csharp
if (!engine.TryValidate(source, out var diagnostics))
{
    foreach (var d in diagnostics)
        log.Warn("{Code} at {Span}: {Message}", d.Code, d.Span, d.Message);
}
```

`EvaluateWithTrace(...)` returns a tree showing each evaluated node, its inputs, its output, and the execution path it took.

<!-- test: RootReadme_CartRuleValidationEvaluationAndTrace_UseTypedCartShape -->
```csharp
engine.SetVariable<Cart>("cart", new Cart
{
    Subtotal = 120m,
    Discount = 20m,
    Tax = 8m,
    ItemCount = 3,
    PostalCode = "94107"
});

var trace = engine.EvaluateWithTrace(
    "cart.Subtotal - cart.Discount + cart.Tax");

Console.WriteLine(trace.Result);       // 108m
Console.WriteLine(trace.Tree.Source);
// cart.Subtotal - cart.Discount + cart.Tax
```

A rule editor or support tool can render that tree like this:

```text
+  cart.Subtotal - cart.Discount + cart.Tax  = 108
  -  cart.Subtotal - cart.Discount           = 100
    cart.Subtotal                            = 120
    cart.Discount                            = 20
  cart.Tax                                   = 8
```

See [Diagnostics and debugging](docs/operations/diagnostics-and-debugging.md).

## Documentation

Full documentation lives in [`docs/`](docs/README.md), organized as concepts, guides, reference, and operations.

## Build from source

```bash
dotnet restore
dotnet build
dotnet test
```

Repository layout, test-suite organization, and the AOT-matrix harness are in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
