# CsEval Syntax Reference

## Grammar Overview

CsEval uses C# syntax as its core, so C# developers will feel immediately at home. In addition to standard C# expressions, CsEval adds modern enhancements inspired by other languages to make expressions more powerful and expressive while keeping full C# familiarity.

See also: [Extensions](extensions.md)

## Expression Precedence (lowest to highest)

1. Assignment: `??=`
2. Null-coalescing: `??`
3. Ternary: `? :`
4. Logical OR: `||`
5. Logical AND: `&&`
6. Bitwise OR: `|`
7. Bitwise XOR: `^`
8. Bitwise AND: `&`
9. Equality: `==`, `!=`
10. Comparison: `<`, `<=`, `>`, `>=`
11. Shift: `<<`, `>>`
12. Additive: `+`, `-`
13. Multiplicative: `*`, `/`, `%`
14. Unary: `-`, `!`, `~`
15. Postfix: `.`, `?.`, `[]`, `()`
16. Primary: literals, identifiers, grouping

## Literals

### Numbers

```
42        // int (default for integers)
-42       // negative int
42L       // long (explicit suffix)
3.14      // double (default for floating-point)
3.14f     // float (explicit suffix)
3.14m     // decimal (explicit suffix)
0         // zero (int)
```

Numeric literal types match C# behavior:
- Integers default to `int` (Int32), auto-promote to `long` if too large
- Floating-point defaults to `double`
- Suffixes: `L` (long), `U` (uint), `UL` (ulong), `F` (float), `D` (double), `M` (decimal)

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

| Operator    | Description                                                              | Example |
| ----------- | ------------------------------------------------------------------------ | ------- |
| `+`         | Addition / String concat / [Object merge](extensions.md#object-merging-) | `a + b` |
| `-`         | Subtraction                                                              | `a - b` |
| `*`         | Multiplication                                                           | `a * b` |
| `/`         | Division                                                                 | `a / b` |
| `%`         | Modulo                                                                   | `a % b` |
| `-` (unary) | Negation                                                                 | `-x`    |

### Comparison

| Operator | Description      | Example  |
| -------- | ---------------- | -------- |
| `==`     | Equality         | `a == b` |
| `!=`     | Inequality       | `a != b` |
| `<`      | Less than        | `a < b`  |
| `<=`     | Less or equal    | `a <= b` |
| `>`      | Greater than     | `a > b`  |
| `>=`     | Greater or equal | `a >= b` |

### Logical

| Operator | Description | Example    |
| -------- | ----------- | ---------- |
| `&&`     | Logical AND | `a && b`   |
| `\|\|`   | Logical OR  | `a \|\| b` |
| `!`      | Logical NOT | `!a`       |

Short-circuit evaluation is used for `&&` and `||`.

### Bitwise

| Operator | Description | Example  |
| -------- | ----------- | -------- |
| `&`      | Bitwise AND | `a & b`  |
| `\|`     | Bitwise OR  | `a \| b` |
| `^`      | Bitwise XOR | `a ^ b`  |
| `~`      | Bitwise NOT | `~a`     |
| `<<`     | Left shift  | `a << 2` |
| `>>`     | Right shift | `a >> 2` |

Bitwise operations convert operands to integers (truncating decimals) and return `long`.

### Null Handling

| Operator | Description                | Example     |
| -------- | -------------------------- | ----------- |
| `??`     | Null-coalescing            | `a ?? b`    |
| `?.`     | Null-conditional           | `obj?.Prop` |
| `??=`    | Null-coalescing assignment | `x ??= y`   |

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

```csharp
var name = expression;    // Type inferred
int x = 42;               // Explicit type
long y = 100;
double z = 3.14;
string s = "hello";
bool flag = true;
```

Supported type keywords: `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `object`

**Type validation is strict**: Assigning an incompatible type throws an error.

```csharp
int x = "hello";    // Error: Cannot assign String to int
int y = null;       // Error: Cannot assign null to int
string s = 42;      // Error: Cannot assign Int32 to string
```

**Implicit coercion is allowed** for compatible numeric types:

```csharp
long x = 42;        // OK: int coerced to long
double y = 10;      // OK: int coerced to double
```

**Type keywords are reserved** (matching C# behavior) and cannot be used as variable names.

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

Type keywords (matching C#):
- `int`
- `long`
- `double`
- `float`
- `decimal`
- `string`
- `bool`
- `object`

## EBNF Grammar

```ebnf
expression     = assignment ;
assignment     = null_coalesce ( "??=" assignment )? ;
null_coalesce  = conditional ( "??" conditional )* ;
conditional    = or ( "?" expression ":" expression )? ;
or             = and ( "||" and )* ;
and            = bitwise_or ( "&&" bitwise_or )* ;
bitwise_or     = bitwise_xor ( "|" bitwise_xor )* ;
bitwise_xor    = bitwise_and ( "^" bitwise_and )* ;
bitwise_and    = equality ( "&" equality )* ;
equality       = comparison ( ( "==" | "!=" ) comparison )* ;
comparison     = shift ( ( "<" | "<=" | ">" | ">=" ) shift )* ;
shift          = term ( ( "<<" | ">>" ) term )* ;
term           = factor ( ( "+" | "-" ) factor )* ;
factor         = unary ( ( "*" | "/" | "%" ) unary )* ;
unary          = ( "!" | "-" | "~" ) unary | postfix ;
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
               | TYPE_KEYWORD IDENTIFIER "=" expression ";"
               | expression ";" ;

TYPE_KEYWORD   = "int" | "long" | "double" | "float" | "decimal" | "string" | "bool" | "object" ;
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
