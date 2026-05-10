---
title: Alder FAQ
description: Direct answers to common evaluation, integration, security, Dynamic LINQ, and NativeAOT questions.
---

# Alder FAQ

Alder is an embeddable C# expression engine for .NET applications that need runtime evaluation with compiler-style binding, host-controlled security policy, Dynamic LINQ, expression-tree export, and NativeAOT generated dispatch.

## Is Alder a C# expression evaluator?

Yes. Alder evaluates C# expressions and statement blocks against CLR objects supplied by the host. The evaluation path parses source text, binds it against the configured type surface, validates it under the active security policy, and executes the bound tree through the interpreter or the compiled backend.

## How is Alder different from Dynamic LINQ?

Dynamic LINQ is one integration surface over Alder's core pipeline. Alder also evaluates standalone expressions and statement blocks, supports async execution through the interpreter, exports expression trees for provider-facing workflows, and runs under NativeAOT through generated dispatch metadata.

## Does Alder support statement blocks?

Yes. Standard mode supports expression input and statement-block input, including local variables, assignment, `if`, `switch`, loops, `return`, `throw`, `try/catch/finally`, lambdas, query expressions, pattern matching, async, and iterators within Alder's documented language surface.

## Does Alder support async expressions?

Yes. `EvaluateAsync(...)` runs through the interpreter and awaits expression-level asynchronous work inside the bound tree. Alder supports `await`, async calls, `IAsyncEnumerable<T>`, `await foreach`, iterator forms, cancellation, and execution constraints on the async path.

## Does Alder support NativeAOT?

Yes. The interpreter is Alder's NativeAOT-compatible execution path. Generated type contexts provide dispatch metadata for member access, invocation, construction, and selected rooted generic shapes. JIT deployments can use reflection fallback; AOT-sensitive deployments should root the runtime shapes expressions can reach.

## Can Alder export LINQ expression trees?

Yes. The compiled integration surface can export supported lambda shapes as `Expression<TDelegate>` and powers `IQueryable<T>` Dynamic LINQ operators. Provider export has a narrower node surface than in-process evaluation because it must produce ordinary LINQ expression trees.

## Is Alder a rules engine?

Alder can power rules, policy checks, formulas, and configurable calculations, but it does not impose a rules storage model, agenda, conflict resolution system, or business-rule lifecycle. The host owns those product decisions and uses Alder as the expression engine.

## Is Alder a scripting engine?

Alder supports expression and statement-block evaluation over host-provided CLR types. It does not implement C# compilation units, type declarations, namespaces, attributes, preprocessor directives, or unsafe code.

## Is Alder a sandbox?

No. Alder executes inside the host process. `SecurityOptions`, type and namespace rules, reflection metadata guards, and execution constraints limit Alder-observed operations, but registered functions, modules, delegates, extension methods, and host objects execute with host authority.

## What does Alder use by default?

`AlderOptions` defaults to `LanguageMode.Standard`, case-sensitive name matching, trusted security policy, the interpreter for synchronous evaluation, the interpreter for async evaluation, and the built-in generated dispatch context. Hosts can configure security, execution limits, type resolution, modules, functions, AOT contexts, and the optional compiled backend.

## When should a host configure SecurityOptions explicitly?

Hosts should configure `SecurityOptions` explicitly when expressions are user-authored, tenant-authored, stored externally, or evaluated across trust boundaries. `SecurityOptions.Trusted()` is the adoption default for trusted application code. Custom policies use `new SecurityOptions { ... }` with each allowed operation named directly.
