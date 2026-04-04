`await` works inside loops, try/catch, switch expressions, conditionals, pattern matching, goto labels. Every control flow construct that supports `await` in compiled C# supports it in Alder.

```csharp
var engine = new AlderEngine();
engine.SetVariable("http", new HttpClient());

string html = await engine.EvaluateAsync<string>("""
    await http.GetStringAsync("https://example.com")
    """);
```

## API

Use `EvaluateAsync` instead of `Evaluate`:

```csharp
// Task<T>: returns T
int value = await engine.EvaluateAsync<int>("await Task.FromResult(42)");

// Task (void): returns null
await engine.EvaluateAsync("await Task.Delay(100)");

// Multiple awaits
var result = await engine.EvaluateAsync("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
    """);
// 30
```

`EvaluateAsync` also works for non-async expressions, making it a safe default.

### Variable overloads

All `EvaluateAsync` overloads match `Evaluate`:

| Overload | Variables |
|----------|-----------|
| `EvaluateAsync(string)` | Engine's persistent variables |
| `EvaluateAsync(string, IDictionary<string, object?>)` | Persistent + dictionary (scoped) |
| `EvaluateAsync(string, object)` | Persistent + anonymous object (scoped) |
| `EvaluateAsync(AlderExpression, ...)` | Pre-parsed expression |
| `EvaluateAsync<T>(...)` | Generic typed return |

All accept `CancellationToken` as the last parameter.

## Async services

Pass async-capable objects as variables:

```csharp
engine.SetVariable("db", myDbContext);

var users = await engine.EvaluateAsync<List<User>>("""
    var query = await db.Users.Where(u => u.IsActive).ToListAsync();
    return query;
    """);
```

## Async lambdas

```csharp
var result = await engine.EvaluateAsync("""
    Func<int, Task<int>> doubler = async x => await Task.FromResult(x * 2);
    return await doubler(21);
    """);
// 42
```

Async lambdas return `Task<object?>` and evaluate via the async path.

## CancellationToken injection

If a method's last parameter is `CancellationToken` and the caller provides one fewer argument, the engine automatically appends the current cancellation token:

```csharp
// db.Users.ToListAsync() has a CancellationToken parameter
// Alder injects the token from EvaluateAsync's CancellationToken
await engine.EvaluateAsync("""
    return await db.Users.ToListAsync();
    """, cancellationToken: cts.Token);
```

## Await in control flow

```csharp
// In foreach + try/catch
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
// In conditional
var value = await engine.EvaluateAsync("""
    var response = await http.GetAsync(url);
    if (response.IsSuccessStatusCode)
        return await response.Content.ReadAsStringAsync();
    return "error";
    """);
```

```csharp
// In for loop
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
| `Task<T>` | Returns `T` |
| `Task` | Returns `null` |
| `ValueTask<T>` | Returns `T` |
| `ValueTask` | Returns `null` |

Other types produce `CS4001`.

## Static API

```csharp
var result = await AlderEval.EvaluateAsync<int>("await Task.FromResult(99)");
```

## Execution backend

Expressions containing `await` run on the interpreted backend. Async code is I/O-bound; the per-node overhead is negligible against network or disk latency. The interpreted path also avoids `System.Linq.Expressions` (which cannot represent `await`), keeping full AOT compatibility.

Non-async expressions called via `EvaluateAsync` use the compiled backend when configured.

## Constraints

| Constraint | Diagnostic |
|-----------|------------|
| `await` requires `EvaluateAsync()` | CS4033 |
| `await` in `lock` body | CS1996 (§12.9.8.1) |
| `ParseAsExpression<T>` cannot contain `await` | Expression trees don't support async |
