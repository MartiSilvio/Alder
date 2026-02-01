# CsEval Features

## Core
- Zero external dependencies
- Hybrid execution (IL compilation with AST fallback)
- Thread-safe child contexts
- Expression caching

## Language
- Arithmetic, comparison, logical, bitwise operators
- Control flow: if/else, while, for, foreach, switch
- Variable declarations with type inference
- Interpolated and verbatim strings
- Array literals with spread operator
- Anonymous objects with spread/merge
- Null-safe navigation

## LINQ
- 40+ methods supported
- Lambda closures with variable capture

## Extensions
- Pluggable ILanguageExtension interface
- Built-in JavaScript extension (map, filter, reduce, ===, !==)
- Built-in Python extension (in operator, and/or/not keywords)

## Built-in Modules
- Math, DateTime, Guid, Convert, String, Enumerable

## Security
- Three sandbox modes: Trusted, Safe, Strict
- Granular permission controls
- Reflection blocking by default
- Loop iteration limits

## Compilation
- Interpreted mode (all features)
- Compiled mode (IL with fallback)
- StrictCompiled mode (IL required)
- Lazy compilation on first use

## Integration
- IServiceProvider support
- CancellationToken passing
- Custom argument transformers
- Assembly scanning for registration
