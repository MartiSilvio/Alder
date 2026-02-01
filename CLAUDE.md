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
│   │   ├── RuntimeHelpers.cs  # Shared runtime logic (single source of truth)
│   │   ├── Compiler/      # IL compilation via Expression Trees
│   │   ├── CsEvalContext.cs   # Variable scope management
│   │   ├── TypeCache.cs       # Reflection caching
│   │   └── StaticProxies.cs   # Built-in modules (Math, DateTime, etc.)
│   ├── CsEvalEngine.cs    # Main public API
│   ├── CsEvalExpression.cs # Pre-parsed expression wrapper
│   └── CsEvalOptions.cs   # Configuration options
└── tests/CsEval.Test/     # NUnit test suite
```

## Key Patterns

- **Visitor pattern**: Evaluator implements `IExprVisitor<object?>` to traverse AST
- **RuntimeHelpers as single source of truth**: All shared logic (operators, LINQ, method invocation) lives in `RuntimeHelpers.cs`. Both the tree-walking evaluator and IL compiler delegate to these methods.
- **Exception-based control flow**: `return`, `break`, `continue` use exceptions caught by parent blocks
- **Reflection blocking**: All reflection types blocked at evaluation boundary (see [sandbox.md](docs/sandbox.md))

## Tests

```bash
dotnet test
```

Tests run in 3 compilation modes via `[TestFixture]` attributes:
- `CompilationMode.Interpreted` - Tree-walking evaluation
- `CompilationMode.Compiled` - IL compilation with fallback
- `CompilationMode.StrictCompiled` - IL compilation only (throws if not compilable)

Test folders: `Core/`, `Parsing/`, `Evaluator/`, `Loops/`, `Compilation/`, `Integration/`, `Security/`, `Performance/`

## Code Style Guidelines

1. **Delegate to .NET runtime - don't reinvent the wheel**: This is the most important rule. Always prefer calling .NET runtime methods over custom implementations:
   - LINQ methods delegate to `System.Linq.Enumerable`
   - IL compilation uses `System.Linq.Expressions` (Expression Trees) - never emit raw IL
   - Operators delegate to `dynamic` dispatch
   - Type conversions use `Convert.ChangeType`
   - If .NET has a method for it, use it

2. **No code duplication**: If logic is needed by both the IL compiler and tree-walking evaluator, put it in `RuntimeHelpers.cs` and have both call it.

3. **Delegate to helpers**: Functions that just call another function should not exist - the caller should use the target function directly.

4. **No refactoring comments**: Don't add comments like "single source of truth", "now delegates to X", "consolidated from Y".

5. **Minimal comments**: Only add comments when logic isn't self-evident.

6. **Clean up dead code**: When refactoring, delete unused methods entirely.

## Extending CsEval

- **New operators**: Add to `RuntimeHelpers.cs` and reference from both evaluator and IL compiler
- **New LINQ methods**: Add handler to `LinqHandlers` dictionary in `RuntimeHelpers.cs`
- **New statements**: Add AST node to `Ast.cs`, parser logic, visitor method in Evaluator, and IL compilation support

## Common Gotchas

1. **Numeric literals**: `42` is `int`, `42L` is `long`, `3.14` is `double`, `3.14m` is `decimal`
2. **Integer division truncates**: `5/2` returns `2`, use `5.0/2.0` for `2.5` (matches C#)
3. **Decimal/float mixing throws**: `decimal + double` throws `RuntimeBinderException` (C# forbids this)
4. **LINQ returns `List<object?>`**: Not `IEnumerable<T>`
5. **Block scope**: `var` is scoped to block, no shadowing
6. **Case sensitivity**: Default is case-sensitive, use `CsEvalOptions { IgnoreCase = true }`
7. **Reserved keywords**: All C# keywords are reserved
