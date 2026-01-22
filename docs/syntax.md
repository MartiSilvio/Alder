# CsEval Syntax Reference

## Grammar Overview

CsEval uses a C#-like syntax with some simplifications. This document provides a formal description of the language.

## Expression Precedence (lowest to highest)

1. Assignment: `??=`
2. Null-coalescing: `??`
3. Ternary: `? :`
4. Logical OR: `||`
5. Logical AND: `&&`
6. Equality: `==`, `!=`
7. Comparison: `<`, `<=`, `>`, `>=`
8. Additive: `+`, `-`
9. Multiplicative: `*`, `/`, `%`
10. Unary: `-`, `!`
11. Postfix: `.`, `?.`, `[]`, `()`
12. Primary: literals, identifiers, grouping

## Literals

### Numbers

```
42        // long integer
-42       // negative integer
3.14      // double
-3.14     // negative double
0         // zero
```

All integers are parsed as `long` (Int64). All decimals are parsed as `double`.

### Strings

```
"hello"           // double quotes
'hello'           // single quotes (equivalent)
"line1\nline2"    // escape sequences
"path\\to\\file"  // escaped backslash
"say \"hi\""      // escaped quotes
```

Supported escape sequences: `\n`, `\r`, `\t`, `\\`, `\"`, `\'`

### Interpolated Strings

```
$"Hello, {name}!"
$"Sum: {a + b}"
$"Items: {items.Count()}"
$"Nested: {obj.Property}"
```

Expressions inside `{}` are evaluated and converted to string.

### Booleans and Null

```
true
false
null
```

## Operators

### Arithmetic

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition / String concat / Object merge | `a + b` |
| `-` | Subtraction | `a - b` |
| `*` | Multiplication | `a * b` |
| `/` | Division | `a / b` |
| `%` | Modulo | `a % b` |
| `-` (unary) | Negation | `-x` |

### Comparison

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equality | `a == b` |
| `!=` | Inequality | `a != b` |
| `<` | Less than | `a < b` |
| `<=` | Less or equal | `a <= b` |
| `>` | Greater than | `a > b` |
| `>=` | Greater or equal | `a >= b` |

### Logical

| Operator | Description | Example |
|----------|-------------|---------|
| `&&` | Logical AND | `a && b` |
| `\|\|` | Logical OR | `a \|\| b` |
| `!` | Logical NOT | `!a` |

Short-circuit evaluation is used for `&&` and `||`.

### Null Handling

| Operator | Description | Example |
|----------|-------------|---------|
| `??` | Null-coalescing | `a ?? b` |
| `?.` | Null-conditional | `obj?.Prop` |
| `??=` | Null-coalescing assignment | `x ??= y` |

### Ternary

```
condition ? valueIfTrue : valueIfFalse
```

## Member Access

### Property Access

```
object.Property
object?.Property    // returns null if object is null
```

### Method Calls

```
object.Method()
object.Method(arg1, arg2)
Module.StaticMethod(args)
```

### Index Access

```
array[0]
dictionary["key"]
```

## Collections

### Array Literals

```
[]                    // empty array
[1, 2, 3]            // array of numbers
["a", "b", "c"]      // array of strings
[obj1, obj2]         // array of objects
```

### Object Literals (Anonymous Objects)

```
new { }                           // empty object
new { Name = "John" }            // single property
new { Name = "John", Age = 30 }  // multiple properties
```

Properties are always assigned with `=` (not `:`).

### Spread Operator

The spread operator (`...`) expands arrays and objects inline:

**Array Spread:**

```
var arr1 = [1, 2, 3];
var arr2 = [4, 5, 6];
[...arr1, ...arr2]              // [1, 2, 3, 4, 5, 6]
[0, ...arr1, 4]                 // [0, 1, 2, 3, 4]
```

**Object Spread:**

```
var person = new { Name = "John", Age = 30 };
new { ...person, City = "NYC" }  // { Name = "John", Age = 30, City = "NYC" }
```

Later properties override earlier ones:

```
var defaults = new { Theme = "light", Size = 10 };
new { ...defaults, Theme = "dark" }  // { Theme = "dark", Size = 10 }
```

Multiple spreads can be combined:

```
var a = new { X = 1 };
var b = new { Y = 2 };
new { ...a, ...b, Z = 3 }       // { X = 1, Y = 2, Z = 3 }
```

The spread operator works with any iterable for arrays and any object (including typed objects) for object spread.

## Lambda Expressions

```
x => x * 2                       // single parameter, implicit
(x) => x * 2                     // single parameter, explicit
(a, b) => a + b                  // multiple parameters
() => 42                         // no parameters
x => x.Property                  // member access
x => x.Method()                  // method call
```

Lambda body is a single expression (no blocks in lambda body).

## Block Expressions

```
{
    var x = 10;
    var y = 20;
    return x + y;
}
```

### Variable Declaration

```
var name = expression;
```

Variables are scoped to the containing block.

### If Statements

```
// Single statement
if (condition) return value;

// With block
if (condition) {
    var x = compute();
    return x;
}

// With else
if (condition) {
    return a;
} else {
    return b;
}

// Else-if chain
if (x == 1) return "one";
else if (x == 2) return "two";
else return "other";
```

### Return Statements

```
return;              // return null
return expression;   // return value
```

## Object Merging

The `+` operator merges objects when operands are not both numeric:

```
// Object + Anonymous object
person + new { Extra = "data" }

// Dictionary + Dictionary
dict1 + dict2

// Result is always a Dictionary<string, object?>
```

Properties from the right operand override those from the left.

## Comments

```
// Single-line comment
x + y // inline comment

/* Multi-line
   comment */

x + /* inline */ y
```

## Reserved Words

The following are reserved and cannot be used as identifiers:

- `true`
- `false`
- `null`
- `new`
- `var`
- `return`
- `if`
- `else`
- `switch` (reserved but not implemented)
- `case` (reserved but not implemented)
- `default` (reserved but not implemented)

## EBNF Grammar

```ebnf
expression     = assignment ;
assignment     = null_coalesce ( "??=" assignment )? ;
null_coalesce  = conditional ( "??" conditional )* ;
conditional    = or ( "?" expression ":" expression )? ;
or             = and ( "||" and )* ;
and            = equality ( "&&" equality )* ;
equality       = comparison ( ( "==" | "!=" ) comparison )* ;
comparison     = term ( ( "<" | "<=" | ">" | ">=" ) term )* ;
term           = factor ( ( "+" | "-" ) factor )* ;
factor         = unary ( ( "*" | "/" | "%" ) unary )* ;
unary          = ( "!" | "-" ) unary | postfix ;
postfix        = primary ( "." IDENTIFIER | "?." IDENTIFIER | "[" expression "]" | "(" arguments? ")" )* ;
primary        = NUMBER | STRING | "true" | "false" | "null"
               | INTERPOLATED_STRING
               | "new" "{" object_properties "}"
               | "(" ( expression | lambda_params "=>" expression ) ")"
               | "[" array_elements "]"
               | "{" block_body "}"
               | IDENTIFIER ( "=>" expression )? ;

lambda_params  = IDENTIFIER ( "," IDENTIFIER )* ;
arguments      = expression ( "," expression )* ;
array_elements = ( array_element ( "," array_element )* )? ;
array_element  = "..." expression | expression ;
object_properties = ( object_property ( "," object_property )* )? ;
object_property = "..." expression | IDENTIFIER "=" expression ;

block_body     = statement* ;
statement      = "return" expression? ";"
               | "if" "(" expression ")" ( "{" statement* "}" | statement ) ( "else" ( "{" statement* "}" | statement ) )?
               | "var" IDENTIFIER "=" expression ";"
               | expression ";" ;
```

## Not Supported

The following C# features are intentionally not supported:

### Type Operations

```csharp
(int)x           // Type casting - NOT supported
x is string      // Type checking - NOT supported
x as string      // Safe casting - NOT supported
typeof(int)      // Type reference - NOT supported
```

### Operators

```csharp
x & y            // Bitwise AND - NOT supported
x | y            // Bitwise OR - NOT supported
x ^ y            // Bitwise XOR - NOT supported
~x               // Bitwise NOT - NOT supported
x << 2           // Left shift - NOT supported
x >> 2           // Right shift - NOT supported
x++              // Increment - NOT supported
x--              // Decrement - NOT supported
x += 1           // Compound assignment - NOT supported (use x = x + 1)
```

### Control Flow

```csharp
for (...)        // For loops - NOT supported
while (...)      // While loops - NOT supported
foreach (...)    // Foreach loops - NOT supported
switch (x) { }   // Switch statements - NOT supported (reserved)
throw new ...    // Throw statements - NOT supported
try { } catch    // Try-catch - NOT supported
```

### Other

```csharp
nameof(x)        // Name of expression - NOT supported
default(T)       // Default value - NOT supported
x..y             // Range operator - NOT supported
new int[5]       // Array initialization - NOT supported
x = y            // Assignment (use ??= for null-coalescing assignment)
params args      // Params arrays - limited support
```

### Lambda Limitations

Lambda bodies must be single expressions:

```csharp
// Supported
x => x * 2
x => x > 0 ? "positive" : "non-positive"

// NOT supported - use block expressions at top level instead
x => { var y = x * 2; return y + 1; }
```

For complex logic, use block expressions at the top level of your expression.
