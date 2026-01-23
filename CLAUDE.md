# CsEval - LLM Context File

This file provides context for AI assistants working with the CsEval codebase.

> **Documentation Reference**: For detailed documentation, see the `/docs` folder:
> - [features.md](docs/features.md) - Supported features and syntax
> - [syntax.md](docs/syntax.md) - Complete syntax reference
> - [api.md](docs/api.md) - Public API documentation
> - [architecture.md](docs/architecture.md) - Internal architecture and design decisions
> - [extensions.md](docs/extensions.md) - How to extend CsEval
> - [benchmarks.md](docs/benchmarks.md) - Performance benchmarks and optimization
>
> **Feature Status**: See [ROADMAP.md](ROADMAP.md) for implemented features and future plans.

## Project Overview

CsEval is a C#-like expression evaluator library for .NET 8. It parses and evaluates expressions at runtime, designed for scenarios where dynamic expression evaluation is needed (e.g., query languages, formula evaluation, rule engines).

## Architecture

```
CsEval/
├── src/CsEval/
│   ├── Parsing/           # Lexer, Parser, AST definitions
│   │   ├── Lexer.cs       # Tokenizer - converts source to tokens
│   │   ├── Token.cs       # Token types enum and Token record
│   │   ├── Parser.cs              # Core parser utilities and entry point
│   │   ├── Parser.Expressions.cs  # Expression precedence hierarchy
│   │   ├── Parser.Primary.cs      # Primary expressions and literals
│   │   ├── Parser.Statements.cs   # Statement and control flow parsing
│   │   └── Ast.cs         # Expression AST node definitions
│   ├── Evaluation/        # Expression evaluation
│   │   ├── Evaluator.cs   # Main visitor-pattern evaluator
│   │   ├── Evaluator.Operators.cs  # +, -, *, /, comparisons
│   │   ├── Evaluator.Linq.cs       # LINQ method implementations
│   │   ├── Evaluator.Helpers.cs    # Method invocation, type coercion
│   │   ├── EvalContext.cs          # Variable scope management
│   │   ├── EvalException.cs        # Runtime exceptions
│   │   ├── ExpressionCompiler.cs   # AST to System.Linq.Expressions compilation
│   │   ├── CompilerHelpers.cs      # Static helpers for compiled expressions
│   │   ├── TypeCache.cs            # Reflection caching, compiled property getters
│   │   └── StaticProxies.cs        # Built-in modules (Math, DateTime, etc.)
│   ├── Attributes/        # Registration attributes
│   │   ├── CsEvalModuleAttribute.cs
│   │   └── CsEvalFunctionAttribute.cs
│   ├── CsEvalEngine.cs    # Main public API
│   ├── CsEvalExpression.cs # Pre-parsed expression wrapper
│   └── CsEvalOptions.cs   # Configuration options
└── tests/CsEval.Test/     # NUnit test suite
```

## Key Design Decisions

### 1. Visitor Pattern for Evaluation
The evaluator implements `IExprVisitor<object?>` to traverse the AST. Each expression type has a corresponding `Visit*` method.

### 2. Object Merging with `+` Operator
The `+` operator is overloaded to merge objects/dictionaries when operands aren't numeric. Properties from the right side override the left side.

### 3. Block Expressions with Control Flow
Blocks support `var` declarations, control flow statements (`if`, `while`, `for`, `foreach`, `do-while`, `switch`), and `return`. Early returns are implemented using a `ReturnValue` exception that's caught by `VisitBlock`. Loop control (`break`, `continue`) uses `BreakException` and `ContinueException`. Switch statements also use `BreakException` to exit cases.

### 4. LINQ as First-Class Feature
LINQ methods are implemented directly in `Evaluator.Linq.cs` rather than delegating to .NET LINQ. This allows lambda expressions to be evaluated in the CsEval context.

### 5. Module System
Types can be registered as "modules" which appear as objects in expressions:
- `Math.Abs(-5)` - Built-in MathProxy
- `Custom.MyMethod()` - User-registered module

### 6. Lazy Instance Resolution
Module instances can be resolved from `IServiceProvider` at evaluation time, enabling DI integration.

### 7. Hybrid Compilation
Simple expressions can be optionally compiled to `System.Linq.Expressions` delegates for ~5-20x speedup. Complex expressions (blocks, loops, LINQ) fall back to tree-walking. Controlled by `CompilationMode` enum:
- `Disabled`: Always tree-walk
- `OnDemand`: Compile only when `Compile()` is called explicitly (default)
- `Eager`: Compile automatically during `Parse()`

## Common Modification Patterns

### Adding a New Operator

1. Add token type to `Token.cs`
2. Update `Lexer.cs` to recognize the token
3. Add AST node to `Ast.cs` with visitor method
4. Update `Parser.cs` to parse the operator
5. Add `Visit*` method to `Evaluator.cs`
6. Add tests

### Adding a New LINQ Method

1. Add method name to `IsEnumerableMethod()` in `Evaluator.Linq.cs`
2. Add case in `TryInvokeEnumerableMethod()`
3. Add tests

### Adding a New Built-in Module

1. Create proxy class in `StaticProxies.cs`
2. Register in `RegisterBuiltInModules()` in `CsEvalEngine.cs`

### Adding a Statement Type

1. Add AST node to `Ast.cs`
2. Add visitor method to `IExprVisitor`
3. Update `ParseStatement()` or `ParseStatementList()` in `Parser.cs`
4. Add `Visit*` method to `Evaluator.cs`
5. Handle interaction with `ReturnValue` exception if needed

## Important Implementation Details

### EvalContext Scoping
- `CreateChild()` creates a child scope that inherits parent variables
- `Define()` sets a variable in the current scope
- `Set()` updates an existing variable in the scope chain
- `Get()` searches up the scope chain

### Lambda Closure Capture
Lambdas capture the `EvalContext` at definition time (`LambdaValue.Closure`). When invoked, a child context is created from the closure.

### Method Invocation
- Module methods go through `InvokeModuleMethod()`
- Instance methods go through `TryInvokeMethod()`
- LINQ methods are handled specially in `TryInvokeEnumerableMethod()`
- `CancellationToken` is auto-appended if method expects it
- `ArgumentTransformer` hook allows preprocessing arguments

### Task Unwrapping
Methods returning `Task<T>` are automatically awaited via `UnwrapTask()`.

### Loop Safety
All loops (`while`, `for`, `foreach`, `do-while`) have a configurable iteration limit via `CsEvalOptions.MaxIterations` (default: 100,000) to prevent infinite loops. `break` exits the innermost loop, `continue` skips to the next iteration.

## Test Structure

Tests are organized in `tests/CsEval.Test/`:

```
CsEval.Test/
├── Core/           # Engine, validation, thread safety
├── Parsing/        # Lexer and parser tests
├── Evaluator/      # Expression evaluation (arithmetic, LINQ, collections, etc.)
├── Loops/          # While, for, foreach, do-while tests
├── Compilation/    # Expression compilation tests
├── Integration/    # Async, DI, caching, attribute registration
└── Performance/    # Benchmarks
```

## Running Tests

```bash
dotnet test
```

## Common Gotchas

1. **Numeric literals match C# spec**: `42` is `int`, `42L` is `long`, `3.14` is `double`, `3.14m` is `decimal`. Integer literals use automatic type promotion (int if fits, else long).

2. **LINQ returns `List<object?>`**: Not `IEnumerable<T>`. Methods like `ToArray()` return `object?[]`.

3. **`+` with null objects**: Adding `null + dict` or `typed + null` will throw. Use null checks or `??`.

4. **Block scope**: Variables declared with `var` are scoped to the block. No variable shadowing.

5. **Return from if**: `if (cond) return x;` works but the `return` must include value or semicolon.

6. **Case sensitivity default**: Default is case-sensitive. Use `CsEvalOptions { IgnoreCase = true }` for case-insensitive.

7. **Service resolution timing**: `IServiceProvider` is used at evaluation time, not registration time.

8. **Reserved keywords**: All C# keywords (including contextual keywords like `value`, `base`, etc.) are reserved and cannot be used as variable names.
