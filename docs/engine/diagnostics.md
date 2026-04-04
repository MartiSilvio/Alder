Structured diagnostics with Roslyn-compatible CS codes where an equivalent exists, ALDR codes for Alder-specific errors. Every `AlderException` carries `IReadOnlyList<AlderDiagnostic>` with code, message, source position, and severity.

## `AlderDiagnostic`

| Property | Type | Description |
|----------|------|-------------|
| `Code` | `DiagnosticCode?` | The error code enum value |
| `FormattedCode` | `string?` | Formatted string (`"CS0103"`, `"ALDR0107"`) |
| `Message` | `string` | The formatted error message |
| `Severity` | `DiagnosticSeverity` | `Error`, `Warning`, or `Info` |
| `Span` | `TextSpan` | Source text span (start offset + length) |
| `Line` | `int?` | One-based line number |
| `Column` | `int?` | One-based column number |

## `AlderException`

| Property | Type | Description |
|----------|------|-------------|
| `Diagnostics` | `IReadOnlyList<AlderDiagnostic>` | All diagnostics for this error |
| `ErrorCode` | `DiagnosticCode?` | First diagnostic's code |
| `FormattedCode` | `string?` | First diagnostic's formatted code string |
| `Span` | `TextSpan` | First diagnostic's source span |
| `Line` | `int?` | First diagnostic's line |
| `Column` | `int?` | First diagnostic's column |

## `AlderExecutionLimitException`

Extends `AlderException` with execution limit details:

| Property | Type | Description |
|----------|------|-------------|
| `LimitType` | `ExecutionLimitType` | `Statements`, `Timeout`, or `LoopIterations` |
| `LimitValue` | `long` | The configured limit |
| `ActualValue` | `long` | The value that exceeded the limit |
| `StatementsExecuted` | `long` | Total statements executed |
| `ElapsedTime` | `TimeSpan` | Wall-clock time at failure |

## Error Code Reference

### CS Codes (Roslyn-Compatible)

These use the same codes as the C# compiler. Developers familiar with C# diagnostics will recognize them.

#### Type and Conversion Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0019` | Operator '{op}' cannot be applied to operands of type '{left}' and '{right}' | Binary operator with incompatible types |
| `CS0021` | Cannot apply indexing with [] to an expression of type '{type}' | Indexing a non-indexable type |
| `CS0023` | Operator '{op}' cannot be applied to operand of type '{type}' | Unary operator with wrong type |
| `CS0029` | Cannot implicitly convert type '{source}' to '{target}' | Implicit conversion not available |
| `CS0030` | Cannot convert type '{source}' to '{target}' | Explicit conversion not available |
| `CS0031` | Constant value '{value}' cannot be converted to a '{type}' | Constant doesn't fit target type |
| `CS0037` | Cannot convert null to '{type}' because it is a non-nullable value type | Null to non-nullable |
| `CS0266` | Cannot implicitly convert type '{source}' to '{target}'. An explicit conversion exists | Needs explicit cast |

#### Name Resolution Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0103` | The name '{name}' does not exist in the current context | Undefined variable, function, or type |
| `CS0104` | '{name}' is an ambiguous reference between '{ns1}.{name}' and '{ns2}.{name}' | Same type name in multiple imported namespaces |
| `CS0117` | '{type}' does not contain a definition for '{member}' | Static member not found |
| `CS0246` | The type or namespace name '{name}' could not be found | Type not registered |
| `CS1061` | '{type}' does not contain a definition for '{member}' | Instance member not found |

#### Method Resolution Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0121` | The call is ambiguous between methods: '{candidates}' | Multiple equally-valid overloads |
| `CS0123` | Cannot convert '{type}' to delegate type '{delegateType}' | Delegate conversion failure |
| `CS1501` | No overload for method '{name}' takes the given number of arguments | Wrong argument count |
| `CS1661` | Cannot convert lambda: parameter types do not match | Lambda param mismatch |
| `CS1955` | Non-invocable member '{name}' cannot be used like a method | Calling a property as method |
| `CS7036` | No argument given for required parameter '{param}' of '{method}' | Missing required arg |
| `CS8934` | Cannot convert lambda: return type does not match | Lambda return mismatch |

#### Variable and Assignment Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0128` | A local variable named '{name}' is already defined in this scope | Duplicate declaration |
| `CS0131` | Left-hand side of assignment must be a variable, property or indexer | Invalid assignment target |
| `CS0191` | A readonly field cannot be assigned to | Const/readonly modification |
| `CS0815` | Cannot assign null to an implicitly-typed variable | `var x = null;` |

#### Control Flow Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0139` | No enclosing loop out of which to break or continue | `break`/`continue` outside loop |
| `CS0155` | The type caught or thrown must derive from System.Exception | Throwing non-exception |
| `CS0156` | A throw with no arguments outside of a catch clause | `throw;` outside catch |
| `CS0159` | No such label '{name}' within the scope of the goto statement | Undefined goto label |
| `CS0163` | Control cannot fall through from one case label to another | Missing break in switch |
| `CS0185` | A lock expression must be a reference type | `lock(valueType)` |

#### Syntax Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS1003` | Syntax error, '{token}' expected | Missing expected token |
| `CS1013` | Invalid number | Malformed numeric literal |
| `CS1017` | Try statement already has an empty catch block | Duplicate catch-all |
| `CS1021` | Integral constant is too large | Number overflow |
| `CS1525` | Invalid expression term '{term}' | Reserved keyword in expression position |
| `CS1733` | Expression expected | Empty expression |
| `CS8997` | Unterminated raw string literal | Missing closing `"""` |

#### Other CS Errors

| Code | Message | Trigger |
|------|---------|---------|
| `CS0233` | '{type}' does not have a predefined size | `sizeof` on complex type |
| `CS0742` | Query body must end with select or group clause | Incomplete LINQ query |
| `CS0744` | Expected contextual keyword '{keyword}' | Missing LINQ keyword |
| `CS1579` | foreach requires GetEnumerator | Non-enumerable in foreach |
| `CS1729` | '{type}' does not contain a constructor that takes {n} arguments | Wrong ctor args |
| `CS7053` | An expression tree may not contain '{feature}' | Unsupported node in ParseAsExpression |
| `CS8078` | An expression is too long or complex to compile | Nesting depth exceeded |
| `CS8124` | Tuple must contain at least two elements | Single-element tuple |
| `CS8129` | No suitable Deconstruct method found | Missing Deconstruct |
| `CS8132` | Cannot deconstruct {n} elements into {m} variables | Deconstruction count mismatch |
| `CS8510` | Switch expression does not handle all possible values | Non-exhaustive switch |

### ALDR Codes (Alder-Specific)

#### Compilation (ALDR00xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0001` | Strict compilation mode could not compile to IL | Expression not compilable |
| `ALDR0002` | Expression binding failed | Binder failure |
| `ALDR0003` | Call requires runtime overload resolution | Binder can't statically resolve |
| `ALDR0004` | Variable types changed since compilation | `SetVariable<T>` changed a variable's type after `Compile<T>` |
| `ALDR0010` | ParseAsExpression requires a generic Func-style delegate type | Wrong delegate type |
| `ALDR0011` | ParseAsExpression requires lambda input | Non-lambda passed |
| `ALDR0020` | Feature requires LanguageMode.Extended | Extended syntax in Standard mode |

#### Security (ALDR01xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0100` | Method calls blocked by sandbox | `AllowMethodCalls = false` |
| `ALDR0101` | Variable assignment blocked by sandbox | `AllowAssignment = false` |
| `ALDR0102` | Index assignment blocked by sandbox | `AllowIndexSet = false` |
| `ALDR0103` | Property access blocked by sandbox | `AllowPropertyRead = false` |
| `ALDR0104` | Static member access blocked by sandbox | `AllowStaticPropertyRead = false` |
| `ALDR0105` | Property assignment blocked by sandbox | `AllowPropertySet = false` |
| `ALDR0106` | Object construction blocked by sandbox | `AllowConstruction = false` |
| `ALDR0107` | Type blocked by sandbox | Type in deny list |
| `ALDR0108` | Reflection type access blocked | `typeof(T).GetMethods()` etc. |

#### Execution Limits (ALDR02xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0200` | Statement limit exceeded | `MaxStatements` hit |
| `ALDR0201` | Timeout exceeded | `MaxTimeout` hit |
| `ALDR0202` | Collection size exceeded | Array or collection exceeds `MaxCollectionSize` |
| `ALDR0203` | Loop iteration limit exceeded | `MaxLoopIterations` hit |

#### Runtime (ALDR03xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0300` | Cannot access member on null | `null.Property` (non-?.) |
| `ALDR0301` | Cannot call method on null | `null.Method()` (non-?.) |
| `ALDR0302` | Cannot call null as a function | Invoking null |
| `ALDR0303` | Cannot assign to property on null | `null.Prop = x` |
| `ALDR0304` | Method invocation failed | Reflection invoke failure |
| `ALDR0305` | Multi-parameter indexer not supported | `obj[a, b]` on unsupported type |
| `ALDR0306` | Unsupported member type | Member kind not handled |
| `ALDR0307` | Indexer access failed | Runtime indexer error |
| `ALDR0308` | Semantic validation failed | General validation error |
| `ALDR0309` | Pattern type not yet implemented | Unhandled pattern kind |
| `ALDR0310` | Unknown relational pattern operator | Bad pattern operator |
| `ALDR0311` | Invalid out argument index | Out arg index out of range |
| `ALDR0312` | Unsupported tuple arity | Tuple too large |
| `ALDR0313` | Unsupported delegate arity | Too many delegate params |
| `ALDR0314` | Could not resolve delegate type definition | Generic delegate resolution failure |
| `ALDR0315` | Cannot resolve module instance | Module has no instance/constructor/DI |

#### Extended Mode (ALDR04xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0400` | Cannot slice null | `null[1:3]` |
| `ALDR0401` | Slice step cannot be zero | `arr[::0]` |
| `ALDR0402` | Cannot slice type '{type}' | Slicing unsupported type |
| `ALDR0403` | Unsupported compound assignment operator | Unknown `op=` |
| `ALDR0404` | Unsupported chained comparison operator | Unknown chained op |
| `ALDR0405` | Spread outside literal | `..x` outside `[]` or `new {}` |

#### AOT (ALDR05xx)

| Code | Message | Trigger |
|------|---------|---------|
| `ALDR0500` | Type not available in AOT environment | Missing `[AlderRegistered]` for type |
