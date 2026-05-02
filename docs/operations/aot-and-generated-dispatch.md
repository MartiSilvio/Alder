---
title: AOT and generated dispatch
description: How Alder runs in NativeAOT and trimming-sensitive deployments by combining the interpreter, generated type contexts, and reflection-free dispatch metadata.
---

# AOT and generated dispatch

Alder's AOT model is interpreter-first. The language front end still parses, binds, validates, and interprets expressions at runtime; generated contexts supply the dispatch metadata needed for member access, invocation, construction, and selected rooted generic static calls. Closed delegate conversion is supported through factories supplied by an `AlderTypeContext`. In normal JIT deployments that metadata is a typed-first path with reflection fallback. In NativeAOT-style deployments it becomes the authoritative dispatch surface for operations that need runtime type metadata.

## What AOT means for Alder

Ahead-of-time compilation changes the host runtime more than it changes Alder's language model. NativeAOT and similar environments restrict dynamic code generation, trim unused metadata, and make open-ended reflection unreliable unless the host roots the required shapes. Alder accounts for that by keeping the core interpreter usable without `System.Linq.Expressions.Compile()` and by routing covered runtime operations through generated dispatch tables.

The practical consequences are:

- use the interpreter in NativeAOT and trimming-sensitive deployments
- register generated contexts for application-specific types that expressions need to read, write, construct, or call
- root closed generic and delegate shapes that expressions need at runtime
- keep `UseCompiler()` out of AOT deployments; it enables a compiled execution surface that requires dynamic code support
- avoid reflection-heavy discovery APIs when the deployment is trimmed

AOT support changes the mechanics of dispatch, construction, delegate conversion, and generic closure while preserving the bound language contract for covered shapes. Generated contexts cover registered CLR types, selected rooted generic static calls, and any delegate factories the host supplies. Missing coverage becomes a deployment diagnostic when the runtime cannot fall back to reflection or dynamic closure.

## Interpreter first, compiler optional

The interpreter is Alder's AOT-capable execution path. It walks the bound tree and delegates runtime operations to the same security, overload, member, and construction services used elsewhere. Those services try generated dispatch before they reach for reflection.

The compiled backend is a different runtime choice. `UseCompiler()` installs the `Alder.Compiled` expression-tree lowering path and throws when the runtime cannot generate code. In NativeAOT, IL2CPP-style, and other dynamic-code-restricted environments, the supported route is interpreted evaluation with generated metadata for the types and operation shapes expressions touch.

Generated dispatch serves the metadata and invocation layer. The compiled backend serves synchronous delegate generation in JIT-capable environments.

## Generated contexts

A generated context is a partial class derived from `AlderTypeContext`. The source generator looks for `[AlderRegistered(typeof(...))]` attributes on that context and emits a companion implementation containing `TypedDispatch` entries for registered CLR types. For rooted `Task<T>` registrations, it also emits a `GenericStaticDispatch` entry for `Task.FromResult<T>`.

Registration is an inventory of concrete runtime types and supported operation shapes, not a namespace-level allowance. If an expression reaches `order.Customer.Name`, the generated surface must cover the runtime customer type as well as the order type. If an expression calls `order.CalculateTotal(taxRate)`, the generated surface must include the method shape Alder will invoke.

```csharp
using Alder;
using Alder.Aot;

namespace Rules;

public sealed class OrderRow
{
    public decimal Total { get; set; }
    public string Status { get; set; } = "";

    public bool IsOpen() => Status == "Open";
}

[AlderRegistered(typeof(OrderRow))]
public partial class RulesAotContext : AlderTypeContext
{
}
```

The generated part supplies a `Default` instance and returns typed dispatch metadata from `GetTypeMetadata()`:

```csharp
var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});

engine.SetVariable("order", new OrderRow { Total = 125m, Status = "Open" });

var accepted = engine.Evaluate<bool>(
    """order.IsOpen() && order.Total >= 100m""");
```

In a JIT process, Alder can still fall back to reflection if a generated entry misses a shape. In a NativeAOT process, a missing generated path for the same operation produces a generated-mode diagnostic because the deployment cannot depend on a reflection route.

## Built-in and user contexts

Alder loads `AlderBuiltInContext.Default` automatically. The built-in context covers core primitives and common BCL shapes such as strings, numeric types, `Math`, `DateTime`, common generic collections, nullable forms, tuples, and selected async helper roots used by the runtime.

Application contexts stack on top:

```csharp
var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

Context merging is deterministic. Alder starts with the built-in context, then applies additional contexts in registration order. Later typed-dispatch entries replace earlier entries for the same CLR type. Generic static dispatchers accumulate by declaring type. Delegate factories use last-registration-wins by delegate type, and closed delegate roots are merged as a set.

`ClearBuiltInContext()` removes the built-in context and clears queued additional contexts:

```csharp
var engine = new AlderEngine(options =>
{
    options.Aot.ClearBuiltInContext();
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

That is a narrow configuration choice for hosts that want a fully curated dispatch surface. It does not simulate NativeAOT by itself; authoritative generated mode follows the runtime's dynamic-code support. Most applications keep the built-in context and add their own.

## What typed dispatch covers

`TypedDispatch` is Alder's reflection-free operation contract for a single CLR type. Generated implementations can handle:

- instance property and field reads
- writable instance property and field assignment
- static property and field reads
- single-argument indexer reads and writes
- public constructor calls
- instance method invocation
- static method invocation

Generated method dispatch is keyed first by member name, then by argument shape. Same-arity overloads use `is` checks before casting. `out` parameters are supported and copied back into the argument array. `params` methods get both the direct-array form and the expanded-argument form.

The generated surface is intentionally bounded. Generic methods are not expanded into open-ended dispatch. `ref` and `in` parameters, delegate parameters, unsafe parameters, function pointers, ref-like types, by-ref returns, and some value-type mutation shapes stay outside generated dispatch. Init-only and read-only members are omitted from write dispatch. For value types, generated writes are conservative because unboxing would mutate a copy.

Those omissions are part of the AOT contract. Under JIT they can continue through reflection when reflection is available. Under NativeAOT they define the edge of the generated surface a host can rely on.

## Runtime and generated metadata paths

For supported operations, Alder follows a typed-first policy:

1. Try the registered `TypedDispatch` entry for the runtime type.
2. Walk base-type dispatch entries where that operation allows a base-chain lookup.
3. Fall back to the normal reflection path when dynamic code and metadata are available.
4. In authoritative generated mode, fail with a generated-mode diagnostic when no generated path can perform the operation.

The same pattern appears across member access, construction, and method invocation. Object construction first calls generated `TryCreate`. Method invocation first calls `TryInvoke` or `TryInvokeStatic`. Member access first calls `TryGet`, `TrySet`, `TryGetIndex`, or `TrySetIndex`.

Reflection fallback remains valuable in development and normal server deployments. It lets generated contexts be adopted incrementally. A trimmed NativeAOT binary has a stricter contract: expressions should reach only the types and operation shapes that Alder can satisfy through built-in metadata, user-generated metadata, rooted generic closures, or host-provided delegate factories.

Typed dispatch changes the mechanics of runtime operations after semantic binding has already selected or deferred an operation. It does not change parsing, binding rules, overload resolution, sandbox policy, execution limits, or the meaning of an Alder expression. The contract is behavioral equivalence between generated and reflective paths wherever both are available.

Typed dispatch entries are exact. In case-insensitive mode, Alder can preserve the engine's external name-matching contract by resolving the canonical member name and retrying before leaving the typed path. That canonical-name retry depends on runtime metadata. A miss on the typed path is not an error in JIT deployments; it means execution continues through the general runtime path. In generated-only deployments, prefer exact member casing and do not rely on reflection-assisted canonicalization for case mismatches.

## Authoritative generated mode

Alder detects whether runtime dynamic code is supported. When dynamic code support is unavailable, the runtime behaves as an authoritative generated dispatcher for operations that would otherwise need reflection or runtime generic closure.

The failure mode is explicit:

- `ALDR0316`: a member is unavailable in authoritative generated mode
- `ALDR0317`: a method is unavailable in authoritative generated mode
- `ALDR0318`: a constructor is unavailable in authoritative generated mode

That turns deployment mistakes into clear integration errors. If an expression reads `order.Customer.Name`, the registered context must cover the runtime type of `order.Customer` as well as `OrderRow` when that customer object is reached dynamically. If an expression constructs `new Money(...)`, the generated context must include `Money` and the generated constructor shape must match the arguments Alder will pass.

## Generic static dispatch and delegate factories

Some AOT-sensitive operations are not ordinary instance member calls. Alder models those separately.

`GenericStaticDispatch` covers explicit, rooted generic static method shapes. The source generator emits this for `Task.FromResult<T>` when the context registers closed `Task<T>` roots:

```csharp
using Alder.Aot;

[AlderRegistered(typeof(Task<int>))]
[AlderRegistered(typeof(Task<string>))]
public partial class AsyncRulesAotContext : AlderTypeContext
{
}
```

That gives `await Task.FromResult(42)` and `await Task.FromResult("done")` a generated route for those exact result types. Other result types need their own roots.

Delegate factories cover closed delegate types that must be constructible without runtime generic closure. The source generator does not synthesize factories from delegate-typed parameters; factories come from `AlderTypeContext.GetDelegateFactories()`. Each entry maps a closed delegate type to a factory that wraps an Alder lambda in that delegate type. Built-in contexts can root common delegate shapes, and custom contexts can provide application-specific ones:

```csharp
public sealed class RulesDelegateContext(
    Func<object, Func<int, bool>> createPredicate) : AlderTypeContext
{
    public override IReadOnlyList<TypedDispatch> GetTypeMetadata() => [];

    public override IReadOnlyDictionary<Type, Func<object, Delegate>> GetDelegateFactories() =>
        new Dictionary<Type, Func<object, Delegate>>
        {
            [typeof(Func<int, bool>)] = lambda => createPredicate(lambda)
        };
}
```

A closed delegate type is rooted explicitly, and the factory returns that exact delegate shape. Context instances are part of engine configuration, so factory behavior is engine-scoped.

## Reflection boundaries

Alder still uses reflection where the runtime allows it and where the API contract is explicitly reflection-based. That boundary matters in trimmed deployments.

Assembly scanning is the broadest reflection surface. `Modules.RegisterFromAssembly(...)` walks all types and members in an assembly and is marked as trim-sensitive. Prefer explicit registration in AOT-oriented applications:

```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.RegisterFromType<PricingFunctions>();
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

Type resolution and module registration are separate concerns from generated dispatch. Adding an assembly or namespace makes type names resolvable. Reflection-free member access still comes from generated contexts after a type has been resolved.

Security remains independent. Generated dispatch runs behind sandbox policy, and Alder validates whether a type or operation is allowed before evaluation reaches the invocation layer.

## Trimming considerations

Generated contexts root the types and members they reference in generated code. A property read such as `typed.Total`, a constructor call such as `new OrderRow(...)`, or a static call such as `Math.Max(...)` is visible to the C# compiler and to the AOT toolchain. That is the central reason generated dispatch exists.

Trimming-sensitive applications should keep their reachable expression surface explicit:

- register generated contexts for model, DTO, module, and helper types used by expressions
- prefer exact type and module registration over assembly-wide scanning
- root closed generic and delegate shapes that expressions need
- keep expression-facing APIs public and stable
- test the published NativeAOT binary as well as the JIT test host

The built-in context covers common BCL cases, but it cannot know an application's domain model. If an expression can reach a user type in production, that type belongs in a generated context or in a host-curated replacement path. Registering only the root variable type is enough only when expressions never navigate into additional user-defined runtime types.

## When users need this

Generated dispatch matters when the deployment constrains reflection, metadata retention, or runtime code generation:

- NativeAOT applications
- IL2CPP-style deployments
- mobile, game, plugin, and embedded hosts with AOT restrictions
- trimmed services that want predictable metadata retention
- security-sensitive hosts that prefer explicit expression-facing type surfaces

It is also useful for teams that want the expression boundary to be visible in code. A generated context is an auditable list of CLR types Alder is expected to reach.

## JIT deployments with reflection fallback

Most JIT-based server and desktop applications can use Alder without custom generated contexts. The built-in context is loaded automatically, and reflection fallback handles application types when the runtime permits it.

You can still add generated contexts in those environments. They make the eventual AOT boundary visible early and exercise the same dispatch code that a NativeAOT build will depend on. The tradeoff is maintenance: every expression-facing type and supported shape must be kept in the generated surface.

## Deployment tradeoffs

AOT deployment trades runtime breadth for explicit reachability. The interpreter remains flexible, but reflection-heavy runtime discovery is replaced by generated metadata for the parts of the type system that matter to the application.

Startup configuration becomes part of the deployment contract. The engine must be built with the same generated contexts that the published binary expects to use.

Expression design should avoid unbounded runtime shapes. Open-ended extension-method chains, arbitrary generic method closure, and delegate shapes discovered only from expression text are poor fits unless the host explicitly roots the relevant forms.

Parity testing should include both the ordinary JIT path and the published AOT artifact. Alder's tests simulate missing dynamic-code support for many paths, and the repository also contains an AOT matrix harness for real NativeAOT execution. Application test suites should follow the same principle: validate the expression set under the runtime that will ship.

## Practical model

The runtime behavior follows the metadata path available in the current deployment.

The runtime metadata path is broad. It uses reflection, overload resolution, runtime generic closure, and cached invokers when the host runtime supports them.

The generated metadata path is explicit. It uses `AlderTypeContext`, generated `TypedDispatch` entries, generated `GenericStaticDispatch` entries, and any host-supplied delegate factories to make selected operations visible after trimming. It is the path used by AOT deployments; the compiled backend remains a JIT-dependent option for synchronous delegate generation.

JIT deployments can use both paths. NativeAOT deployments depend on the generated path for operations that reflection can no longer guarantee. Good AOT integration is therefore an inventory exercise: identify the types and shapes expressions must reach, register generated contexts for them, avoid reflection-heavy registration, and verify the published artifact.

## Related pages

- [Deploy with NativeAOT](/guides/nativeaot-deployment/)
- [Compiled backend](/concepts/compiled-backend/)
- [Execution and reuse](/operations/execution-and-reuse/)
- [Configuration](/reference/configuration/)
