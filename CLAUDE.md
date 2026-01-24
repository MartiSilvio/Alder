# CsEval - LLM Context File

This file provides context for AI assistants working with the CsEval codebase.

> **Documentation**: See the `/docs` folder for detailed documentation:
>
> - [features.md](docs/features.md) - Supported features and syntax
> - [syntax.md](docs/syntax.md) - Complete syntax reference
> - [api.md](docs/api.md) - Public API documentation
> - [architecture.md](docs/architecture.md) - Internal architecture and design decisions
> - [sandbox.md](docs/sandbox.md) - Sandbox modes and reflection blocking
> - [extensions.md](docs/extensions.md) - Language extensions (spread, object merging)
> - [benchmarks.md](docs/benchmarks.md) - Performance benchmarks and optimization
>
> **Feature Status**: See [ROADMAP.md](ROADMAP.md) for implemented features and future plans.

## Project Overview

CsEval is a C#-like scripting engine for .NET. It parses and evaluates expressions and statements at runtime, supporting control flow (`if`, `for`, `while`, `switch`), variable declarations, LINQ, and lambdas. Designed for rule engines, formula evaluation, and embedded scripting scenarios.

## Architecture

```
CsEval/
├── src/CsEval/
│   ├── Parsing/           # Lexer, Parser, AST definitions
│   │   ├── Lexer.cs       # Tokenizer
│   │   ├── Token.cs       # Token types enum
│   │   ├── Parser*.cs     # Recursive descent parser (partial classes)
│   │   └── Ast.cs         # Expression AST nodes
│   ├── Evaluation/        # Expression evaluation
│   │   ├── Evaluator*.cs  # Visitor-pattern evaluator (partial classes)
│   │   ├── Evaluator.Registry.cs  # Operator/LINQ method registries
│   │   ├── EvalContext.cs         # Variable scope management
│   │   ├── ExpressionCompiler.cs  # Optional compilation to delegates
│   │   ├── TypeCache.cs           # Reflection caching
│   │   └── StaticProxies.cs       # Built-in modules (Math, DateTime, etc.)
│   ├── CsEvalEngine.cs    # Main public API
│   ├── CsEvalExpression.cs # Pre-parsed expression wrapper
│   └── CsEvalOptions.cs   # Configuration options
└── tests/CsEval.Test/     # NUnit test suite
```

## Key Patterns

- **Visitor pattern**: Evaluator implements `IExprVisitor<object?>` to traverse AST
- **Registry-based dispatch**: Operators and LINQ methods use dictionary lookups (see `Evaluator.Registry.cs`)
- **Exception-based control flow**: `return`, `break`, `continue` use exceptions caught by parent blocks
- **Reflection blocking**: All reflection types blocked at evaluation boundary (see [sandbox.md](docs/sandbox.md))

## Extending CsEval

- **New operators**: Add to `BinaryOperators`/`UnaryOperators` in `Evaluator.Registry.cs`
- **New LINQ methods**: Add to `LinqMethodNames` + implement in `TryInvokeEnumerableMethod`
- **New statements**: Add AST node to `Ast.cs`, parser logic, and visitor method

## Tests

```bash
dotnet test
```

Test folders: `Core/`, `Parsing/`, `Evaluator/`, `Loops/`, `Compilation/`, `Integration/`, `Security/`, `Performance/`

## Implementation Guidelines

1. **Delegate to C# runtime whenever possible**: Prefer calling .NET runtime methods over custom implementations. For example, LINQ methods should delegate to `System.Linq.Enumerable` rather than implementing logic manually. This ensures correct behavior, better performance, and automatic support for edge cases.

2. **Update documentation when features are implemented**: When adding new features:
   - Update [ROADMAP.md](ROADMAP.md) to mark features as completed
   - Update [docs/features.md](docs/features.md) with the new functionality
   - Update [docs/syntax.md](docs/syntax.md) if new syntax is added

3. **Use typed collections in tests**: Test inputs should use real typed collections (e.g., `List<int>`, `List<string>`) instead of `List<object?>` to simulate realistic usage patterns.

## Common Gotchas

1. **Numeric literals**: `42` is `int`, `42L` is `long`, `3.14` is `double`, `3.14m` is `decimal`
2. **Integer division truncates**: `5/2` returns `2`, use `5.0/2.0` for `2.5` (matches C#)
3. **Decimal/float mixing throws**: `decimal + double` throws `RuntimeBinderException` (C# forbids this)
4. **LINQ returns `List<object?>`**: Not `IEnumerable<T>`
5. **Block scope**: `var` is scoped to block, no shadowing
6. **Case sensitivity**: Default is case-sensitive, use `CsEvalOptions { IgnoreCase = true }`
7. **Reserved keywords**: All C# keywords are reserved
