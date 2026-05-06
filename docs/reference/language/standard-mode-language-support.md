---
title: Standard mode language support
description: The C# syntax Alder supports in Standard mode for runtime expressions and statement blocks.
---

# Standard mode language support

Standard mode is Alder's default language mode. It evaluates C# expressions and statement blocks with C# semantics for binding, conversions, overload resolution, member access, control flow, lambdas, query expressions, pattern matching, and CLR type interaction.

The supported syntax is scoped to code that runs inside an embeddable C# expression runtime: expressions, statement blocks, local state, calls into exposed CLR objects, and control flow over host-provided data. It is suitable for stored rules, formulas, policy checks, configurable calculations, runtime filters, and application scripting points that benefit from C# semantics.

At runtime, Alder binds Standard-mode code against the current host context: variables, registered functions, modules, type registrations, and extension-method containers. Security policy validates the bound operations, and execution constraints govern runtime work. Strongly typed inputs produce earlier diagnostics and more precise overload selection; object-shaped inputs preserve runtime flexibility. The same bound semantics feed interpreted execution, async execution, optional compiled execution, and AOT metadata-backed dispatch, while backend-specific pages document narrower execution, export, and deployment surfaces.

Standard mode accepts expression and statement-block input. Full-program declarations such as types, namespaces, members, attributes, access modifiers, and preprocessor directives are outside the accepted input.

The ECMA mapping records Standard-mode support against ECMA-334 sections. The C# specification remains the authority for the semantics Alder implements.

`Supported` means Alder can parse, bind, validate, and evaluate the construct inside its expression and statement-block input model. It covers runtime input, not complete compilation units, type/member declaration support, compiled-delegate support for every construct, or expression-tree export for every runtime construct. Provider-facing export, including Dynamic LINQ `IQueryable<T>` paths, has a narrower node surface because it must produce ordinary LINQ expression trees.

Extended mode builds on this baseline. It adds Alder-specific convenience syntax such as pipelines, inclusive and exclusive range helpers, collection literals without target types, regex predicates, SQL-style comparison helpers, date arithmetic sugar, and concise aggregate helpers. Extended-only syntax is documented separately and is excluded from the ECMA mapping here.

**Spec edition:** ECMA-334, 7th edition (December 2023).
**Last verified:** 2026-05-01.

## Standard mode at a glance

| Area | Support |
| --- | --- |
| Expressions | Arithmetic, comparison, logical operators, casts, conversions, member access, index access, calls, object creation, lambdas, query expressions, tuples, interpolation, `typeof`, `nameof`, `default`, `await`, and throw expressions inside Alder input. |
| Statement blocks | Local variables, constants, assignment, `if`, `switch`, loops, `break`, `continue`, `goto case`, `goto default`, `return`, `throw`, `try/catch/finally`, exception filters, `using`, `lock`, and iterators. |
| Type system | CLR primitive types, reference types, nullable types, tuples, constructed generic types, interfaces, delegates, enums, `dynamic` as object-shaped runtime binding, overload resolution, extension methods, user-defined conversions, and user-defined operators. |
| Host integration | Variables, registered functions, modules, type registration, extension-method containers, security policy, execution constraints, and optional compiled execution all apply to Standard-mode code. |
| Excluded constructs | Type/member declarations, namespaces, attributes, preprocessor directives, unsafe program structure, and constructs that require a C# compilation unit. |

## Scope and boundaries

Alder evaluates expressions and statement-block fragments from strings.

These language areas are intentionally out of scope for Alder input:

| Area | Status | Notes |
| --- | --- | --- |
| Type declarations | Out of scope | `class`, `struct`, `interface`, `enum`, and `namespace` declarations are not part of the input surface. |
| Attributes | Out of scope | Attribute syntax is not part of the input surface. |
| Type member declarations | Out of scope | Type-level methods, properties, fields, events, and access modifiers are not declared inside Alder expressions. Local functions are statement-level declarations and are covered separately. |
| Preprocessor directives | Out of scope | `#if`, `#define`, and related directives are not supported. |

Extended mode adds syntax outside ECMA-334; that syntax belongs to the Extended language reference, outside the Standard ECMA mapping.

## Standard features outside the ECMA mapping

Alder's Standard mode also supports C# forms that are part of modern C# practice but sit outside the ECMA-334 7th edition table used below. These are Standard-mode features, not Extended-mode features.

| Area | Status | Notes |
| --- | --- | --- |
| Switch expressions | Supported | Includes pattern arms, discard arms, `when` guards, non-exhaustive diagnostics, and arm-local pattern-variable scope. |
| Relational, logical, and property patterns | Supported | Available in `is` expressions and switch-expression arms. ECMA-334 7th edition only defines declaration, constant, and var patterns. |
| C# range expressions (`..`) | Supported | Produces `System.Range` when used as a C# range expression. Alder-specific inclusive and exclusive range helpers belong to Extended mode. |

## Status meanings

| Status | Meaning |
| --- | --- |
| Supported | Implemented for Standard-mode runtime input, subject to any backend, security policy, AOT, or export boundary documented on the relevant operations page. |
| Partial | Implemented with documented constraints in Alder's input model. |
| Out of scope | Not part of Alder's input surface by design. |

The `Notes` column clarifies constraints, edge cases, or integration expectations for the listed language area.

## Lexical and parsing (Chapter 6)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §6.4.5 | Literals | Supported | Includes numeric, character, string, boolean, and null literals that are part of Alder input. |
| §6.4.3 | Verbatim identifiers (`@name`) | Supported |  |
| §6.4.4 | Contextual keywords | Supported | Contextual keywords are interpreted according to parse context. |
| §6.4.5.3 | Integer literal type and promotion rules | Supported |  |
| §6.4.5.4 | Real literals that start with `.` | Supported |  |
| §6.4.5.5 | Character literals | Supported | Includes supported escape sequences. |

## Variables (Chapter 9)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §9.2.9.1 | Discards | Supported | `_` is treated as a discard when it is not already defined as a variable in the current context. |

## Conversions (Chapter 10)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §10.2 | Implicit conversions | Supported | Alder applies the standard implicit conversion rules during binding. |
| §10.2.3 | Implicit numeric conversions | Supported |  |
| §10.2.8 | Implicit reference conversions | Supported | Includes reference and boxing conversions that apply to Alder input. |
| §10.2.11 | Implicit constant-expression conversions | Supported | Applies to constant expressions in Alder input. |
| §10.2.13 | Implicit tuple conversions | Supported | Tuple element conversions are applied element-wise when a tuple literal has a target tuple type. |
| §10.3 | Explicit conversions | Supported | Explicit conversions are validated during binding and executed at runtime. |
| §10.3.1 | General | Supported | Every implicit conversion is also an explicit conversion. |
| §10.3.2 | Explicit numeric conversions | Supported |  |
| §10.3.3 | Explicit enumeration conversions | Supported | Enum conversions are supported between enums and numeric types, and between enums. |
| §10.3.5 | Explicit reference conversions | Supported | Validity is checked during binding; success may still depend on the runtime value. |
| §10.3.6 | Explicit tuple conversions | Supported | Tuple element conversions are applied element-wise. |
| §10.3.7 | Unboxing conversions | Supported | Object or interface values can be explicitly unboxed to value types. |
| §10.3.8 | Explicit dynamic conversions | Supported | `dynamic` participates as `object` in Alder's type system. |
| §10.4.2 | Standard implicit conversions | Supported |  |
| §10.5.x | User-defined conversions | Supported | User-defined operators are discovered on the participating types. |
| §10.5.3 | Evaluation of user-defined conversions | Supported | Selected user-defined conversion operators are executed at runtime. |
| §10.5.4 | User-defined implicit conversions | Supported |  |
| §10.5.5 | User-defined explicit conversions | Supported |  |
| §10.7.1 | General | Supported | Nullable lifting is applied where relevant. |
| §10.8 | Method group conversions | Supported | Method groups can convert to compatible delegate types during binding. |

## Patterns and pattern matching (Chapter 11)

ECMA-334 (7th edition) defines declaration, constant, and var patterns. Additional pattern forms supported by Alder are still Standard-mode behavior, but they are not counted as ECMA-334 conformance rows.

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §11.2 | Pattern forms | Supported | Alder supports declaration, constant, and var patterns. |
| §11.2.1 | General | Supported |  |
| §11.2.2 | Declaration pattern | Supported |  |
| §11.2.3 | Constant pattern | Supported |  |
| §11.2.4 | Var pattern | Supported |  |
| §11.3 | Pattern subsumption | Supported | Applied when validating pattern usage in `is` and switch constructs. |

## Expressions (Chapter 12)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §12.4.5 | Binary operator overload resolution | Supported | User-defined operators are considered where applicable. |
| §12.4.7.2 | Unary numeric promotions | Supported |  |
| §12.4.7.3 | Binary numeric promotions | Supported |  |
| §12.4.8 | Lifted operators | Supported | Nullable lifting follows the predefined lifted-operator rules. |
| §12.6.2 | Argument lists and output parameters | Partial | `out` arguments are supported. `ref` arguments are not part of the current input surface. |
| §12.6.3 | Type inference | Supported |  |
| §12.6.3.9 | Exact inferences | Supported |  |
| §12.6.3.10 | Lower-bound inferences | Supported |  |
| §12.6.3.12 | Fixing | Supported |  |
| §12.6.3.15 | Finding the best common type of a set of expressions | Supported |  |
| §12.6.4 | Overload resolution | Supported |  |
| §12.6.4.4 | Better parameter-passing mode | Supported | Used when choosing among candidates that differ by parameter passing mode. |
| §12.6.4.7 | Better conversion target | Supported | Used when comparing candidate parameter types during overload resolution. |
| §12.7 | Deconstruction | Supported |  |
| §12.8.3 | Interpolated string expressions | Supported |  |
| §12.8.6 | Tuple expressions | Supported |  |
| §12.8.7.2 | Identical simple names and type names (static access via type name) | Supported |  |
| §12.8.8 | Null conditional member access | Supported |  |
| §12.8.9.3 | Extension method invocations | Supported |  |
| §12.8.11 | Element access | Supported |  |
| §12.8.11.3 | Indexer access | Supported |  |
| §12.8.15 | Postfix increment and decrement operators | Supported | `++` and `--` require a writable operand. |
| §12.8.16.2 | Object creation expressions | Supported |  |
| §12.8.16.3 | Object initializers | Supported |  |
| §12.8.16.4 | Collection initializers | Supported |  |
| §12.8.16.5 | Array creation expressions | Supported |  |
| §12.8.16.6 | Delegate creation expressions | Supported | `new D(...)` is supported when `D` is a delegate type. |
| §12.8.17 | `typeof` | Supported |  |
| §12.8.20 | Default value expressions | Supported |  |
| §12.8.21 | Stack allocation | Out of scope | `stackalloc` is not part of Alder's executable input surface. |
| §12.8.22 | `nameof` | Supported |  |
| §12.9.3 | Unary `+` | Supported |  |
| §12.9.4 | Unary `-` | Supported |  |
| §12.9.5 | Logical negation `!` | Supported |  |
| §12.9.6 | Bitwise complement `~` | Supported |  |
| §12.9.8 | Await expressions | Supported | Requires an async evaluation API. |
| §12.9.8.1 | `await` in `lock` is not allowed | Supported |  |
| §12.10 | Arithmetic operators | Supported | Includes predefined arithmetic operators and relevant lifting behavior. |
| §12.10.5 | String concatenation and delegate addition | Supported |  |
| §12.10.6 | Delegate subtraction | Supported |  |
| §12.11 | Operator overload resolution | Supported |  |
| §12.12 | Relational and type-testing operators | Supported |  |
| §12.12.11 | Tuple equality | Supported |  |
| §12.13 | Boolean logical operators | Supported | Includes `&`, `|`, `^`, `&&`, `||`. |
| §12.13.3 | Enum bitwise operators | Supported |  |
| §12.13.5 | Three-valued `bool?` logic | Supported |  |
| §12.14.2 | Boolean conditional logical operators | Supported | Includes three-valued `bool?` semantics for `&&` and `||`. |
| §12.16 | The throw expression operator | Supported | Includes `throw` as an expression, including `?? throw ...`. |
| §12.15 | Null coalescing operator (`??`) | Supported |  |
| §12.18 | Conditional operator (`?:`) type unification | Supported |  |
| §12.19 | Anonymous functions (lambdas, anonymous `delegate`) | Supported |  |
| §12.20 | Query expressions | Supported |  |
| §12.20.2 | Ambiguities in query expressions | Supported | Query parsing follows the standard disambiguation rules. |
| §12.20.3.2 | Query expressions with continuations | Supported | `into` continuations are supported. |
| §12.20.3.5 | From, let, where, join, orderby clause translation | Supported |  |
| §12.20.3.6 | Select clause translation | Supported |  |
| §12.20.3.7 | Group clause translation | Supported |  |
| §12.20.3.8 | Transparent identifiers | Supported |  |
| §12.21.2 | Simple assignment | Supported | Assignment targets must be assignable. |
| §12.21.4 | Compound assignment | Supported | Includes forms such as `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`. |
| §12.21.5 | Event assignment | Out of scope | Events are not first-class assignment targets in Alder input. |
| §12.22 | Expression | Supported | In boolean-condition contexts, the expression must be implicitly convertible to `bool`. |
| §12.23 | Constant expressions | Supported |  |

## Statements (Chapter 13)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §13.3 | Blocks and statement lists | Supported |  |
| §13.6.2 | Local variable declarations | Supported | Includes multi-declarator forms such as `int x = 1, y = 2;`. |
| §13.6.3 | Local constant declarations | Supported | `const` locals must be initialized with a compile-time constant expression. |
| §13.6.4 | Local function declarations | Partial | Local functions are lowered to lambda-backed locals and support closures, recursion, mutual recursion, and iterator local functions. Generic local functions, `static`, `ref`, `out`, `params`, default parameter values, named-argument local-function calls, and forward references are outside the supported input. |
| §13.8.2 | `if` statement | Supported |  |
| §13.8.3 | `switch` statement | Supported | Includes `case` labels and `default`. |
| §13.9.2-§13.9.5 | Iteration statements (`while`, `do`, `for`, `foreach`) | Supported |  |
| §13.9.4 | The for statement | Supported | Includes empty statement bodies such as `for (...);`. |
| §13.10.2 | The break statement | Supported |  |
| §13.10.3 | The continue statement | Supported |  |
| §13.10.4 | `goto` statement | Supported | Includes `goto case` and `goto default`. |
| §13.10.6 | `throw` statement | Supported |  |
| §13.10.5 | The return statement | Supported | Includes `return;` and `return <expr>;`. |
| §13.11 | `try/catch/finally` | Supported | Includes exception filters (`when`). |
| §13.13 | `lock` statement | Supported |  |
| §13.14 | `using` statement | Supported |  |
| §13.15 | `yield return` and `yield break` | Supported |  |

## Declarations and scopes (Chapter 7)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §7.3 | Declarations | Supported | Tuple element names behave as compile-time metadata and do not affect runtime type identity. |
| §7.7 | Scopes | Supported | Local scoping permits shadowing of variables in parent scopes. |

## Types (Chapter 8)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §8.1 | General | Supported | Standard mode uses CLR types and runtime member discovery. |
| §8.2.4 | The dynamic type | Supported | `dynamic` participates as `object` in Alder's type system, with runtime binding where necessary. |
| §8.2.6 | Interface types | Supported | Interfaces participate in type resolution and conversion classification. |
| §8.3.5 | Simple types | Supported | Simple types map to the corresponding CLR primitive types. |
| §8.3.6 | Integral types | Supported | Integral types participate in numeric promotion and overload resolution as in C#. |
| §8.4 | Constructed types | Supported | Constructed generic types are supported, including arity validation during type-name resolution. |

## Delegates (Chapter 20)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §20.1 | General | Supported | Delegate invocation follows the delegate's `Invoke` signature. |

## Unsafe code (Chapter 23)

| ECMA section | Area | Status | Notes |
| --- | --- | --- | --- |
| §23.6.9 | The sizeof operator | Partial | `sizeof(T)` is supported for the predefined primitive value-type forms Alder maps directly: `bool`, integer types, `char`, floating-point types, and `decimal`. Other unsafe `sizeof` forms are outside the input surface. |
| Chapter 23 | Unsafe blocks, pointer types, and unsafe declarations | Out of scope | Unsafe program structure is outside Alder's expression and statement-block input surface. |
