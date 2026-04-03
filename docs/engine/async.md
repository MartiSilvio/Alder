---
title: "Async/Await"
description: "Evaluate expressions containing await — call async .NET APIs from dynamically evaluated code"
sidebar:
  order: 4
---

Alder supports `await` in dynamically evaluated expressions. Call any async .NET API — HTTP clients, database queries, cloud services, message queues — directly from runtime-evaluated C# code.

```csharp
var engine = new AlderEngine();
engine.SetVariable("http", new HttpClient());

string html = await engine.EvaluateAsync<string>("""
    await http.GetStringAsync("https://example.com")
    """);
```

This is a full `await` — non-blocking, returning the unwrapped result. Not a `.Result` hack, not a `.GetAwaiter().GetResult()` deadlock trap. Real async, real `Task<T>` unwrapping, real `ValueTask<T>` support.

## API

Use `EvaluateAsync` instead of `Evaluate`:

```csharp
// Await Task<T> — returns T
int value = await engine.EvaluateAsync<int>("await Task.FromResult(42)");

// Await Task (void) — returns null
await engine.EvaluateAsync("await Task.Delay(100)");

// Multiple awaits in a block
var result = await engine.EvaluateAsync("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    a + b
    """);
// 30
```

`EvaluateAsync` also works for non-async expressions — making it a safe default for any evaluation path.

## Inject async services

Pass async-capable objects as variables and call their methods:

```csharp
engine.SetVariable("db", myDbContext);

var users = await engine.EvaluateAsync<List<User>>("""
    var query = await db.Users.Where(u => u.IsActive).ToListAsync();
    query
    """);
```

Or pass a pre-built `Task` directly:

```csharp
var pending = SomeService.FetchDataAsync();
var result = await engine.EvaluateAsync(
    "await data",
    new Dictionary<string, object?> { ["data"] = pending });
```

## Await in control flow

`await` works inside loops, conditionals, try/catch, and every other control flow construct:

```csharp
var result = await engine.EvaluateAsync("""
    var urls = new[] { "https://a.com", "https://b.com" };
    var results = new List<string>();
    foreach (var url in urls)
    {
        results.Add(await http.GetStringAsync(url));
    }
    results
    """);
```

```csharp
var value = await engine.EvaluateAsync("""
    var response = await http.GetAsync(url);
    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadAsStringAsync();
    }
    return "error";
    """);
```

## Supported awaitable types

| Type | Result |
|------|--------|
| `Task<T>` | Awaits, returns `T` |
| `Task` | Awaits, returns `null` |
| `ValueTask<T>` | Awaits, returns `T` |
| `ValueTask` | Awaits, returns `null` |

## Static API

```csharp
var result = await AlderEval.EvaluateAsync<int>("await Task.FromResult(99)");
```

## Use cases

- **Rule engines** that call async APIs — validate against external services, fetch reference data
- **Dynamic workflows** with HTTP calls — orchestrate microservices from user-defined expressions
- **Low-code platforms** where user expressions need I/O — query databases, call APIs, read files
- **Plugin systems** where user code awaits host-provided async services
- **Report builders** that pull data from multiple async sources in a single expression

## How it works

The interpreted evaluator's methods are `async` — each `await` in the expression maps to a real .NET `await` in the evaluator. C#'s own async/await machinery handles suspension, resumption, and continuation scheduling. No custom state machine, no code generation.

The source generator produces a dual dispatch system: synchronous `Dispatch` for `Evaluate`, asynchronous `DispatchAsync` for `EvaluateAsync`. Every evaluator that can contain child expressions provides both sync and async variants. Leaf evaluators (literals, identifiers) are automatically wrapped.

## Execution backend

Expressions containing `await` run on the interpreted backend. Non-async expressions called via `EvaluateAsync` use the compiled backend when configured.

This is by design: async code is I/O-bound. The CPU overhead difference between interpreted and compiled evaluation is nanoseconds per node — invisible against network or disk latency. The interpreted path also means `await` works without any dependency on `System.Linq.Expressions`, which has no support for `await` nodes (`CS1989`).

## Known limitations

| Limitation | Workaround |
|-----------|------------|
| `await` requires `EvaluateAsync()` | Use `EvaluateAsync` for any expression that may contain `await` |
| No `async` lambda declarations yet | Pass pre-built async delegates as variables |
| LINQ `ParseAsExpression<T>` cannot contain `await` | Use `EvaluateAsync` directly |
