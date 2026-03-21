---
title: "Error Codes"
description: "Complete catalog of all CS and CSEV diagnostic codes with message templates and descriptions."
sidebar:
  order: 2
---

## Overview

Alder uses two error code namespaces:

- **CS####** (35 codes) -- Roslyn-parity codes that match C# compiler error numbers
- **CSEV####** (49 codes) -- Alder-specific codes with no Roslyn equivalent

Every code is a member of the `DiagnosticCode` enum. The formatting rule:

- Enum values < 1,000,000 format as `CS{value:D4}` (e.g., `CS0103`)
- Enum values >= 1,000,000 format as `CSEV{value-1000000:D4}` (e.g., `CSEV0010`)

Error codes appear in `AlderException.FormattedCode`, `AlderException.ErrorCode`, and `AlderDiagnostic.Code`.

## CS Codes (Roslyn Parity)

### Operator Applicability

| Code   | Name               | Message Template                                                     | Description                                    |
| ------ | ------------------ | -------------------------------------------------------------------- | ---------------------------------------------- |
| CS0019 | `BadBinaryOps`     | Operator '{0}' cannot be applied to operands of type '{1}' and '{2}' | Binary operator used with incompatible types   |
| CS0021 | `BadIndexerAccess` | Cannot apply indexing with [] to an expression of type '{0}'         | Indexer used on a type that doesn't support it |
| CS0023 | `BadUnaryOp`       | Operator '{0}' cannot be applied to operand of type '{1}'            | Unary operator used with incompatible type     |

### Type Conversion

| Code   | Name                         | Message Template                                                                                       | Description                                                    |
| ------ | ---------------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------- |
| CS0029 | `NoImplicitConversion`       | Cannot implicitly convert type '{0}' to '{1}'                                                          | No implicit conversion exists between types                    |
| CS0030 | `NoExplicitConversion`       | Cannot convert type '{0}' to '{1}'                                                                     | No conversion exists, even with explicit cast                  |
| CS0031 | `ConstantValueCannotConvert` | Constant value '{0}' cannot be converted to a '{1}'                                                    | Constant value out of range for target type                    |
| CS0037 | `NullToNonNullable`          | Cannot convert null to '{0}' because it is a non-nullable value type                                   | Null assigned to a non-nullable value type                     |
| CS0266 | `ExplicitConversionExists`   | Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?) | Implicit conversion not available but explicit cast would work |

### Name Resolution

| Code   | Name                     | Message Template                                                          | Description                                    |
| ------ | ------------------------ | ------------------------------------------------------------------------- | ---------------------------------------------- |
| CS0103 | `NameNotInContext`       | The name '{0}' does not exist in the current context                      | Undeclared variable or unknown identifier      |
| CS0104 | `AmbiguousReference`     | '{0}' is an ambiguous reference between '{1}' and '{2}'                   | Identifier matches multiple definitions        |
| CS0117 | `NoMemberOnType`         | '{0}' does not contain a definition for '{1}'                             | Static member not found on a type              |
| CS0128 | `DuplicateLocalVariable` | A local variable or function named '{0}' is already defined in this scope | Variable name already in use in the same scope |

### Control Flow

| Code   | Name                             | Message Template                                                             | Description                                   |
| ------ | -------------------------------- | ---------------------------------------------------------------------------- | --------------------------------------------- |
| CS0139 | `BreakOrContinueOutsideLoop`     | No enclosing loop out of which to break or continue                          | `break` or `continue` used outside a loop     |
| CS0155 | `ThrowExpressionMustBeException` | The type caught or thrown must be derived from System.Exception              | Throw expression is not an Exception subclass |
| CS0156 | `ThrowOutsideCatch`              | A throw statement with no arguments is not allowed outside of a catch clause | Bare `throw;` used outside a catch block      |
| CS0163 | `CaseFallThrough`                | Control cannot fall through from one case label to another                   | Non-empty switch case missing `break`         |

### Assignment

| Code   | Name                         | Message Template                                                            | Description                           |
| ------ | ---------------------------- | --------------------------------------------------------------------------- | ------------------------------------- |
| CS0131 | `AssignmentRequiresVariable` | The left-hand side of an assignment must be a variable, property or indexer | Assignment target is not assignable   |
| CS0191 | `ReadonlyAssignment`         | A readonly field cannot be assigned to                                      | Attempt to assign to a readonly field |

### Type and Namespace Resolution

| Code   | Name                    | Message Template                                                                                                  | Description                          |
| ------ | ----------------------- | ----------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| CS0246 | `TypeNotFound`          | The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?) | Type or namespace cannot be resolved |
| CS0815 | `NullToImplicitlyTyped` | Cannot assign null to an implicitly-typed variable                                                                | `var x = null` is not allowed        |

### Method and Delegate Resolution

| Code   | Name                        | Message Template                                                                     | Description                                             |
| ------ | --------------------------- | ------------------------------------------------------------------------------------ | ------------------------------------------------------- |
| CS0121 | `AmbiguousMethodInvocation` | The call is ambiguous between the following methods or properties: '{0}'             | Multiple overloads match equally well                   |
| CS0123 | `DelegateConversionFailed`  | Cannot convert '{0}' to delegate type '{1}'                                          | Method group cannot convert to the target delegate type |
| CS1501 | `NoApplicableOverload`      | No overload for method '{0}' takes the given number of arguments                     | No overload matches the argument count                  |
| CS1955 | `NonCallableType`           | Non-invocable member '{0}' cannot be used like a method                              | Trying to call a property or field as a method          |
| CS7036 | `MissingRequiredArgument`   | There is no argument given that corresponds to the required parameter '{0}' of '{1}' | Required parameter not supplied                         |

### Member Resolution

| Code   | Name                         | Message Template                                                                                                                                         | Description                                      |
| ------ | ---------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| CS1061 | `MemberNotFound`             | '{0}' does not contain a definition for '{1}'                                                                                                            | Instance member not found on an object           |
| CS1579 | `ForeachRequiresIEnumerable` | foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator' | Type doesn't implement `IEnumerable` for foreach |
| CS1729 | `NoMatchingConstructor`      | '{0}' does not contain a constructor that takes {1} arguments                                                                                            | Constructor argument count mismatch              |

### Miscellaneous

| Code   | Name                    | Message Template                                                                              | Description                                     |
| ------ | ----------------------- | --------------------------------------------------------------------------------------------- | ----------------------------------------------- |
| CS0185 | `LockRequiresNonNull`   | A lock expression must be a reference type                                                    | Lock statement used with a value type or null   |
| CS0233 | `SizeofUnsupportedType` | '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context | `sizeof` used on a type without predefined size |

### Exception Handling

| Code   | Name                       | Message Template                               | Description                                             |
| ------ | -------------------------- | ---------------------------------------------- | ------------------------------------------------------- |
| CS1017 | `GeneralCatchMustBeLast`   | Try statement already has an empty catch block | Duplicate bare `catch` clause in try statement          |
| CS1021 | `IntegralConstantTooLarge` | Integral constant is too large                 | Integer literal exceeds the maximum representable value |

### Tuple and Deconstruction

| Code   | Name                          | Message Template                                                                | Description                                             |
| ------ | ----------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------- |
| CS8124 | `TupleTooFewElements`         | Tuple must contain at least two elements                                        | Tuple literal with fewer than 2 elements                |
| CS8129 | `DeconstructionFailed`        | No suitable 'Deconstruct' instance or extension method was found for type '{0}' | Type doesn't support deconstruction                     |
| CS8132 | `DeconstructionCountMismatch` | Cannot deconstruct a tuple of '{1}' elements into '{0}' variables               | Deconstruction variable count doesn't match tuple arity |

## CSEV Codes (Alder-Specific)

### Compilation and Expression Tree (CSEV0001--CSEV0008)

| Code     | Name                                       | Message Template                                                          | Description                                        |
| -------- | ------------------------------------------ | ------------------------------------------------------------------------- | -------------------------------------------------- |
| CSEV0001 | `StrictCompilationFailed`                  | Strict compilation mode could not compile the expression to IL: {0}       | IL compilation failed in strict mode               |
| CSEV0002 | `ExpressionTreeUnsupportedNode`            | Expression tree output does not support '{0}'.                            | Expression tree cannot represent this node type    |
| CSEV0003 | `ExpressionTreeUnsupportedCallShape`       | Expression tree output does not support call shape '{0}'.                 | Expression tree cannot represent this call pattern |
| CSEV0004 | `ParseAsExpressionRequiresGenericDelegate` | ParseAsExpression requires a generic Func-style delegate type; got '{0}'. | Non-Func delegate passed to ParseAsExpression      |
| CSEV0005 | `ParseAsExpressionRequiresLambda`          | Expression must be a lambda (e.g., '{0}').                                | ParseAsExpression input is not a lambda            |
| CSEV0006 | `ParseAsExpressionParameterCountMismatch`  | Expression has {0} parameter(s) but {1} expects {2}.                      | Lambda parameter count doesn't match delegate      |
| CSEV0007 | `ParseAsExpressionReturnTypeMismatch`      | Cannot convert expression body type '{0}' to return type '{1}'.           | Lambda return type incompatible with delegate      |
| CSEV0008 | `BindingFailed`                            | {0}                                                                       | Generic binding failure with detailed message      |

### Language Mode (CSEV0009)

| Code     | Name                   | Message Template                                                                                                     | Description                                 |
| -------- | ---------------------- | -------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| CSEV0009 | `ExtendedModeRequired` | Feature '{0}' is not available in Standard mode. Use LanguageMode.Extended to enable non-standard syntax extensions. | Extended-mode feature used in Standard mode |

### Sandbox and Security (CSEV0010--CSEV0019)

| Code     | Name                                 | Message Template                                   | Description                                                    |
| -------- | ------------------------------------ | -------------------------------------------------- | -------------------------------------------------------------- |
| CSEV0010 | `SandboxAccessBlocked`               | {0} access blocked by sandbox: {1}.{2}             | General member access blocked                                  |
| CSEV0011 | `SandboxMethodCallBlocked`           | Method calls blocked by sandbox: {0}               | Method invocation blocked (AllowMethodCalls = false)           |
| CSEV0012 | `SandboxAssignmentBlocked`           | Assignment blocked by sandbox: {0}                 | Variable assignment blocked                                    |
| CSEV0013 | `SandboxIndexAssignmentBlocked`      | Index assignment blocked by sandbox: [{0}] = ...   | Index setter blocked                                           |
| CSEV0014 | `SandboxPropertyAccessBlocked`       | Property access blocked by sandbox: {0}            | Property read blocked (AllowPropertyRead = false)              |
| CSEV0015 | `SandboxStaticFieldAccessBlocked`    | Static field access blocked by sandbox: {0}.{1}    | Static field read blocked (AllowStaticFieldRead = false)       |
| CSEV0016 | `SandboxStaticPropertyAccessBlocked` | Static property access blocked by sandbox: {0}.{1} | Static property read blocked (AllowStaticPropertyRead = false) |
| CSEV0017 | `SandboxPropertyAssignmentBlocked`   | Property assignment blocked by sandbox: {0}        | Property setter blocked (AllowPropertySet = false)             |
| CSEV0018 | `SandboxConstructionBlocked`         | Object construction blocked by sandbox: new {0}()  | Constructor call blocked (AllowConstruction = false)           |
| CSEV0019 | `SandboxTypeBlocked`                 | Type '{0}' is not in the sandbox allowlist         | Type not present in AllowedTypes                               |

### Null Access (CSEV0020--CSEV0023)

| Code     | Name                     | Message Template                        | Description                             |
| -------- | ------------------------ | --------------------------------------- | --------------------------------------- |
| CSEV0020 | `NullMemberAccess`       | Cannot access {0} '{1}' on null         | Member access on a null reference       |
| CSEV0021 | `NullMethodCall`         | Cannot call method '{0}' on null        | Method call on a null reference         |
| CSEV0022 | `NullInvocation`         | Cannot call null as a function          | Attempting to invoke null as a callable |
| CSEV0023 | `NullPropertyAssignment` | Cannot assign to property '{0}' on null | Property assignment on a null reference |

### Method Resolution (CSEV0024--CSEV0025)

| Code     | Name                                | Message Template                                | Description                             |
| -------- | ----------------------------------- | ----------------------------------------------- | --------------------------------------- |
| CSEV0024 | `MethodInvocationFailed`            | Method '{0}' invocation failed                  | Method call threw or could not complete |
| CSEV0025 | `RuntimeOverloadResolutionRequired` | Call '{0}' requires runtime overload resolution | Overload resolution deferred to runtime |

### Member Access and Property Resolution (CSEV0026--CSEV0029)

| Code     | Name                                | Message Template                                                                | Description                              |
| -------- | ----------------------------------- | ------------------------------------------------------------------------------- | ---------------------------------------- |
| CSEV0026 | `MultiParameterIndexerNotSupported` | Indexer overloads with multiple parameters are not supported yet on type '{0}'. | Multi-parameter indexer not implemented  |
| CSEV0027 | `UnsupportedMemberType`             | Unsupported member type '{0}'                                                   | Member type not handled by the evaluator |
| CSEV0028 | `IndexerAccessFailed`               | Indexer access failed: {0}                                                      | Runtime error during indexer evaluation  |
| CSEV0029 | `NoIndexerOnType`                   | No indexer found on type '{0}'                                                  | Type has no indexer property             |

### Type System (CSEV0030--CSEV0031)

| Code     | Name                          | Message Template                                                    | Description                                                   |
| -------- | ----------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------- |
| CSEV0030 | `ReflectionTypeAccessBlocked` | Access to reflection types is not allowed: {0} ({1})                | Expression returns a reflection type (Type, MemberInfo, etc.) |
| CSEV0031 | `CollectionInitializerNoAdd`  | Type '{0}' does not have an 'Add' method for collection initializer | Collection initializer used on a type without `Add`           |

### Control Flow and Semantics (CSEV0032--CSEV0049)

| Code     | Name                                   | Message Template                                                                                                       | Description                                        |
| -------- | -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------- |
| CSEV0032 | `SemanticValidationFailed`             | Semantic validation failed: {0}                                                                                        | General semantic validation error                  |
| CSEV0033 | `ExpressionNestingDepthExceeded`       | Expression nesting depth exceeded available stack space.                                                               | Stack guard triggered by deep nesting              |
| CSEV0034 | `SliceNull`                            | Cannot slice null                                                                                                      | Slice operator applied to null                     |
| CSEV0035 | `SliceStepZero`                        | Slice step cannot be zero                                                                                              | Slice with step value of zero                      |
| CSEV0036 | `SliceUnsupportedType`                 | Cannot slice type '{0}'                                                                                                | Slice operator on a non-sliceable type             |
| CSEV0037 | `UnknownCompoundAssignmentOperator`    | Unknown compound assignment operator '{0}'                                                                             | Unrecognized compound assignment                   |
| CSEV0038 | `UnsupportedCompoundBaseOperator`      | Unsupported compound assignment base operator '{0}'                                                                    | Compound assignment with unsupported base operator |
| CSEV0039 | `UnsupportedChainedComparisonOperator` | Unsupported chained comparison operator: {0}                                                                           | Chained comparison not supported                   |
| CSEV0040 | `SpreadOutsideLiteral`                 | Spread operator can only be used in array or object literals                                                           | Spread (`..`) used in wrong context                |
| CSEV0041 | `GotoCaseTargetNotFound`               | goto case/default target not found                                                                                     | Switch goto target does not exist                  |
| CSEV0042 | `PatternNotImplemented`                | Pattern type '{0}' not yet implemented                                                                                 | Unimplemented pattern matching form                |
| CSEV0043 | `InvalidOutArgumentIndex`              | Invalid out argument index '{0}'.                                                                                      | Out parameter index out of range                   |
| CSEV0044 | `UnknownRelationalPatternOperator`     | Unknown relational pattern operator '{0}'                                                                              | Unrecognized operator in relational pattern        |
| CSEV0045 | `UnsupportedTupleArity`                | Unsupported tuple arity: {0}                                                                                           | Tuple has too many elements                        |
| CSEV0046 | `SequenceContainsNoElements`           | Sequence contains no elements                                                                                          | Empty sequence where element required              |
| CSEV0047 | `UnsupportedDelegateArity`             | Unsupported delegate arity: {0}                                                                                        | Delegate has too many parameters                   |
| CSEV0048 | `DelegateTypeDefinitionNotFound`       | Could not resolve delegate type definition for '{0}'                                                                   | Delegate type cannot be resolved                   |
| CSEV0049 | `CannotResolveModuleInstance`          | Cannot resolve instance of '{0}'. Either register it in IServiceProvider or ensure it has a parameterless constructor. | Module instance resolution failed                  |

## See Also

- [Exceptions and Diagnostics](../diagnostics/exceptions/) -- exception hierarchy, TryValidate pattern, catch order
