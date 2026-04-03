---
title: "Async/Await"
description: "Full async/await support — await any .NET async API from dynamically evaluated C# code"
sidebar:
  order: 4
---

Alder is a fully async-capable runtime engine. `await` works everywhere — inside loops, try/catch, switch expressions, nested conditionals, pattern matching, goto labels. Every control flow construct that supports `await` in compiled C# supports it in Alder.

```csharp
var engine = new AlderEngine();
engine.SetVariable("http", new HttpClient());

string html = await engine.EvaluateAsync<string>("""
    await http.GetStringAsync("https://example.com")
    """);
```

This is a proper `await` — non-blocking, returning the unwrapped result. Not a `.Result` hack, not a `.GetAwaiter().GetResult()` deadlock trap. Native `Task<T>` and `ValueTask<T>` unwrapping, with correct continuation scheduling.

## API

Use `EvaluateAsync` instead of `Evaluate`:

```csharp
// Await Task<T> — returns T
int value = await engine.EvaluateAsync<int>("await Task.FromResult(42)");

// Await Task (void) — returns null
await engine.EvaluateAsync("await Task.Delay(100)");

// Multiple awaits in sequence
var result = await engine.EvaluateAsync("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
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
    return query;
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

`await` works inside every control flow construct — loops, conditionals, try/catch/finally, switch, pattern matching, goto:

```csharp
var result = await engine.EvaluateAsync("""
    var urls = new[] { "https://a.com", "https://b.com" };
    var results = new List<string>();
    foreach (var url in urls)
    {
        try
        {
            results.Add(await http.GetStringAsync(url));
        }
        catch (HttpRequestException ex)
        {
            results.Add($"error: {ex.Message}");
        }
    }
    return results;
    """);
```

```csharp
var value = await engine.EvaluateAsync("""
    var response = await http.GetAsync(url);
    if (response.IsSuccessStatusCode)
        return await response.Content.ReadAsStringAsync();
    return "error";
    """);
```

```csharp
var result = await engine.EvaluateAsync("""
    var sum = 0;
    for (var i = 0; i < 10; i++)
        sum += await Task.FromResult(i);
    return sum;
    """);
// 45
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

The interpreted evaluator is natively async — every evaluator node provides both synchronous and asynchronous dispatch. When a user expression contains `await`, Alder's evaluator walks the bound tree asynchronously, suspending at each `await` point and resuming when the awaited task completes. C#'s own async/await machinery handles suspension, resumption, thread pool interaction, and `SynchronizationContext` flow.

The source generator produces a dual dispatch system: synchronous `Dispatch` for `Evaluate`, asynchronous `DispatchAsync` for `EvaluateAsync`. Both paths share the same bound tree, binder, and pipeline — the only difference is how child expressions are invoked.

## Execution backend

Expressions containing `await` run on the interpreted backend. Non-async expressions called via `EvaluateAsync` use the compiled backend when configured.

This is by design: async code is I/O-bound. The CPU overhead difference between interpreted and compiled evaluation is nanoseconds per node — invisible against network or disk latency. The interpreted path also avoids any dependency on `System.Linq.Expressions`, which cannot represent `await` nodes (`CS1989`), keeping Alder fully AOT-compatible.

## Constraints

| Constraint | Details |
|-----------|---------|
| `await` requires `EvaluateAsync()` | Using `await` in a synchronous `Evaluate()` call produces CS4033 |
| `await` in lock body | Prohibited per ECMA-334 §12.9.8.1, produces CS1996 |
| LINQ `ParseAsExpression<T>` cannot contain `await` | Expression trees don't support async; use `EvaluateAsync` directly |
