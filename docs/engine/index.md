---
title: "Engine API"
description: "AlderEngine public API — evaluate, parse, compile, configure, and extend"
sidebar:
  order: 1
---

Alder's public API is designed around a single class, `AlderEngine`, configured once at construction and thread-safe for concurrent evaluation. From that entry point, you evaluate expressions, parse and reuse them, validate syntax and semantics without execution, compile to native IL delegates, trace evaluation step-by-step, and inject variables from your application.

The configuration surface covers language mode, security policy, execution limits, type registration, custom functions, class-backed modules, and AOT metadata — all through `AlderOptions` and its sub-builders.

```csharp
var engine = new AlderEngine(o =>
{
    o.LanguageMode = LanguageMode.Extended;
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints { MaxStatements = 10_000 };
    o.UseCompiler();
});

engine.SetVariable<List<Order>>("orders", orderList);
double total = engine.Evaluate<double>("orders.Where(o => o.Status == \"Shipped\").Sum(o => o.Total)");
```

| Page | What it covers |
|------|---------------|
| [AlderEngine](alder-engine.md) | Full API — evaluate, parse, validate, compile, trace, dispose, thread safety, static API |
| [AlderOptions](alder-options.md) | Configuration — language mode, sandbox, constraints, compiler, sub-builders |
| [Variables](variables.md) | Injection patterns, typed vs untyped, anonymous objects, dictionaries, child engines, scoping, caching |
| [Compilation](compilation.md) | IL compilation, UseCompiler, CompileToFunc, ParseAsExpression, Compile\<T\> |
| [LINQ Dynamic](linq-dynamic.md) | String-based LINQ on any IEnumerable\<T\> or IQueryable\<T\> — filter, project, order, group, aggregate |
| [Functions and Modules](functions-and-modules.md) | Delegate functions, class-backed modules, attributes, assembly scanning, DI integration |
| [Type Registration](type-registration.md) | Assemblies, namespaces, extension methods, type resolution order |
| [Diagnostics](diagnostics.md) | AlderDiagnostic, AlderException, every CS and ALDR error code |
