# CsEval - LLM Context File

This file provides context for AI assistants working with the CsEval codebase.

## Project Overview

CsEval is a C#-like expression evaluator library for .NET 8. It parses and evaluates expressions at runtime, designed for scenarios where dynamic expression evaluation is needed (e.g., query languages, formula evaluation, rule engines).

## Architecture

```
CsEval/
├── src/CsEval/
│   ├── Parsing/           # Lexer, Parser, AST definitions
│   │   ├── Lexer.cs       # Tokenizer - converts source to tokens
│   │   ├── Token.cs       # Token types enum and Token record
│   │   ├── Parser.cs      # Recursive descent parser - builds AST
│   │   └── Ast.cs         # Expression AST node definitions
│   ├── Evaluation/        # Expression evaluation
│   │   ├── Evaluator.cs   # Main visitor-pattern evaluator
│   │   ├── Evaluator.Operators.cs  # +, -, *, /, comparisons
│   │   ├── Evaluator.Linq.cs       # LINQ method implementations
│   │   ├── Evaluator.Helpers.cs    # Method invocation, type coercion
│   │   ├── EvalContext.cs          # Variable scope management
│   │   ├── EvalException.cs        # Runtime exceptions
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
The `+` operator is overloaded to merge objects/dictionaries when operands aren't numeric. This enables patterns like:
```csharp
entity + new { ComputedField = value }
```
Properties from the right side override the left side.

### 3. Block Expressions with Early Returns
Blocks support `var` declarations, `if` statements, and `return`. Early returns are implemented using a `ReturnValue` exception that's caught by `VisitBlock`.

### 4. LINQ as First-Class Feature
LINQ methods are implemented directly in `Evaluator.Linq.cs` rather than delegating to .NET LINQ. This allows lambda expressions to be evaluated in the CsEval context.

### 5. Module System
Types can be registered as "modules" which appear as objects in expressions:
- `Math.Abs(-5)` - Built-in MathProxy
- `Custom.MyMethod()` - User-registered module

### 6. Lazy Instance Resolution
Module instances can be resolved from `IServiceProvider` at evaluation time, enabling DI integration.

## AST Node Types

```csharp
// Literals
LiteralExpr(object? Value)

// Operators
UnaryExpr(Token Op, Expr Right)           // -x, !x
BinaryExpr(Expr Left, Token Op, Expr Right) // x + y, x == y
LogicalExpr(Expr Left, Token Op, Expr Right) // x && y, x || y

// Access
IdentifierExpr(Token Name)                // foo
MemberAccessExpr(Expr Object, Token Name, bool NullSafe) // obj.prop, obj?.prop
IndexAccessExpr(Expr Object, Expr Index)  // arr[0]
CallExpr(Expr Callee, List<Expr> Arguments) // func(args)

// Conditionals
ConditionalExpr(Expr Condition, Expr ThenBranch, Expr ElseBranch) // a ? b : c
NullCoalesceExpr(Expr Left, Expr Right)   // a ?? b
NullCoalesceAssignExpr(Token Name, Expr Value) // a ??= b

// Literals/Collections
ArrayLiteralExpr(List<Expr> Elements)     // [1, 2, 3]
ObjectLiteralExpr(List<(Token Key, Expr Value)> Properties) // { A = 1 }
InterpolatedStringExpr(List<InterpolatedPart> Parts) // $"..."

// Lambdas
LambdaExpr(List<Token> Parameters, Expr Body) // (x) => x * 2

// Blocks/Control Flow
BlockExpr(List<Expr> Statements, Expr? ReturnExpr)
VariableDeclExpr(Token Name, Expr Initializer) // var x = 5
IfStatementExpr(Expr Condition, List<Expr> ThenStatements, List<Expr>? ElseStatements)
ReturnExpr(Expr? Value)                   // return x

// New/Special
NewExpr(Expr Initializer)                 // new { ... }
GroupingExpr(Expr Expression)             // (expr)
```

## Token Types

Key tokens to understand:
- `QuestionQuestion` = `??`
- `QuestionQuestionEqual` = `??=`
- `QuestionDot` = `?.`
- `Arrow` = `=>`
- `New`, `Var`, `Return`, `If`, `Else` = keywords

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
- `Set()` updates an existing variable in the scope chain (added for `??=`)
- `Get()` searches up the scope chain

### Lambda Closure Capture
Lambdas capture the `EvalContext` at definition time (`LambdaValue.Closure`). When invoked, a child context is created from the closure.

### Object Merging Rules (in `Add()`)
1. String + anything = string concatenation
2. Numeric + numeric = arithmetic
3. Dict + Dict = merge (right overrides)
4. TypedObject + Dict = reflect left properties, merge right
5. Dict + TypedObject = copy dict, reflect right properties
6. TypedObject + TypedObject = reflect both

### Method Invocation
- Module methods go through `InvokeModuleMethod()`
- Instance methods go through `TryInvokeMethod()`
- LINQ methods are handled specially in `TryInvokeEnumerableMethod()`
- `CancellationToken` is auto-appended if method expects it
- `ArgumentTransformer` hook allows preprocessing arguments

### Task Unwrapping
Methods returning `Task<T>` are automatically awaited via `UnwrapTask()`.

## Test Structure

- `EvaluatorTests.cs` - Core expression evaluation
- `ParserTests.cs` - Parser behavior
- `LexerTests.cs` - Tokenization
- `EngineTests.cs` - High-level API
- `AttributeRegistrationTests.cs` - Module/function registration
- `AsyncTests.cs` - Async evaluation
- `BenchmarkTests.cs` - Performance tests
- `ValidationTests.cs` - Error handling
- `LazyResolutionTests.cs` - DI integration
- `ExpressionCachingTests.cs` - Pre-parsing

## Running Tests

```bash
cd api/CsEval
dotnet test
```

## Performance Considerations

- Pre-parsing (`engine.Parse()`) is ~80% faster for repeated evaluation
- LINQ operations materialize to `List<object?>` internally
- Reflection is used for property access and method invocation
- Consider using `ArgumentTransformer` for batch type coercion

## Common Gotchas

1. **Numbers are `long` by default**: `42` is `long`, not `int`. Use `42.0` for double.

2. **LINQ returns `List<object?>`**: Not `IEnumerable<T>`. Methods like `ToArray()` return `object?[]`.

3. **`+` with null objects**: Adding `null + dict` or `typed + null` will throw. Use null checks or `??`.

4. **Block scope**: Variables declared with `var` are scoped to the block. No variable shadowing.

5. **Return from if**: `if (cond) return x;` works but the `return` must include value or semicolon.

6. **Case sensitivity default**: Default is case-sensitive. Use `CsEvalOptions { IgnoreCase = true }` for case-insensitive.

7. **Service resolution timing**: `IServiceProvider` is used at evaluation time, not registration time.

## Example Usage in Abal Project

CsEval is used for dynamic query expressions in the Abal gym management system:

```typescript
// In TypeScript API layer
export const getById = (idExpr: string) => `{
    var sub = Subscriptions.GetById(${idExpr});
    if (sub == null) return null;
    var pkg = MembershipPackage.GetById(sub.PackageId);
    return sub + new {
        Package = pkg,
        Group = MembershipGroup.GetById(pkg?.GroupId)
    };
}`;
```

The expressions are sent to the backend and evaluated with registered modules for `Subscriptions`, `MembershipPackage`, etc.
