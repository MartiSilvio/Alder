# CsEval Syntax Reference

## Grammar Overview

CsEval uses C# syntax as its core, so C# developers will feel immediately at home. In addition to standard C# expressions, CsEval adds modern enhancements inspired by other languages to make expressions more powerful and expressive while keeping full C# familiarity.

See also: [Extensions](extensions.md)

## Expression Precedence (lowest to highest)

1. Assignment: `=`, `??=`, `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`
2. Null-coalescing: `??`
3. Ternary: `? :`
4. Logical OR: `||`
5. Logical AND: `&&`
6. Bitwise OR: `|`
7. Bitwise XOR: `^`
8. Bitwise AND: `&`
9. Equality: `==`, `!=`, `===`, `!==`
10. Comparison: `<`, `<=`, `>`, `>=`, `in`
11. Shift: `<<`, `>>`
12. Additive: `+`, `-`
13. Multiplicative: `*`, `/`, `%`
14. Unary: `-`, `!`, `~`, `++` (prefix), `--` (prefix)
15. Postfix: `.`, `?.`, `[]`, `()`, `++` (postfix), `--` (postfix)
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

Expressions inside `{}` are evaluated and converted to string. Escape braces with `{{` and `}}`:

```
$"Literal braces: {{not interpolated}}"  // "Literal braces: {not interpolated}"
```

### Verbatim Strings

```
@"path\to\file"           // backslashes are literal
@"C:\Users\John"          // no need to escape
@"She said ""Hello"""     // double quotes escape quotes
```

Backslashes are treated literally (no escape sequences). Use `""` to include a quote character.

### Verbatim Interpolated Strings

```
$@"C:\Users\{name}"       // both prefixes, either order
@$"Path: {path}\file"     // same as above
$@"Braces: {{literal}}"   // {{ and }} for literal braces
```

Combines verbatim string behavior (literal backslashes) with interpolation. Both `$@"..."` and `@$"..."` orderings are supported.

### Booleans and Null

```
true
false
null
undefined    // JavaScript-friendly alias for null
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
| `===`    | Strict equality (JavaScript, same as `==`) | `a === b` |
| `!==`    | Strict inequality (JavaScript, same as `!=`) | `a !== b` |
| `<`      | Less than        | `a < b`  |
| `<=`     | Less or equal    | `a <= b` |
| `>`      | Greater than     | `a > b`  |
| `>=`     | Greater or equal | `a >= b` |
| `in`     | Containment (Python-style) | `x in [1, 2, 3]` |

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

### Assignment

| Operator | Description                | Example     |
| -------- | -------------------------- | ----------- |
| `=`      | Assignment                 | `x = y`     |
| `??=`    | Null-coalescing assignment | `x ??= y`   |

### Compound Assignment

| Operator | Description       | Example    |
| -------- | ----------------- | ---------- |
| `+=`     | Add and assign    | `x += 5`   |
| `-=`     | Subtract and assign | `x -= 3` |
| `*=`     | Multiply and assign | `x *= 2` |
| `/=`     | Divide and assign | `x /= 4`   |
| `%=`     | Modulo and assign | `x %= 3`   |
| `&=`     | Bitwise AND and assign | `x &= mask` |
| `\|=`    | Bitwise OR and assign  | `x \|= flags` |
| `^=`     | Bitwise XOR and assign | `x ^= bits` |
| `<<=`    | Left shift and assign  | `x <<= 2`  |
| `>>=`    | Right shift and assign | `x >>= 1`  |

Compound assignment works with numeric types and strings (`+=` for concatenation).

### Increment/Decrement

| Operator | Description           | Example | Returns |
| -------- | --------------------- | ------- | ------- |
| `++x`    | Prefix increment      | `++x`   | New value after increment |
| `x++`    | Postfix increment     | `x++`   | Old value before increment |
| `--x`    | Prefix decrement      | `--x`   | New value after decrement |
| `x--`    | Postfix decrement     | `x--`   | Old value before decrement |

```csharp
var x = 5;
var a = ++x;   // a = 6, x = 6 (increment then return)
var b = x++;   // b = 6, x = 7 (return then increment)
var c = --x;   // c = 6, x = 6 (decrement then return)
var d = x--;   // d = 6, x = 5 (return then decrement)
```

Works with all numeric types: `int`, `long`, `double`, `float`, `decimal`.

### Null Handling

| Operator | Description                | Example     |
| -------- | -------------------------- | ----------- |
| `??`     | Null-coalescing            | `a ?? b`    |
| `?.`     | Null-conditional           | `obj?.Prop` |

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
let name = expression;    // JavaScript-friendly (same as var)
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

### Loops

All loops support `break` (exit loop) and `continue` (skip to next iteration).

#### While Loop

```csharp
while (condition) {
    // body
}

// Single statement body
while (condition) doSomething();
```

#### For Loop

```csharp
for (var i = 0; i < 10; i += 1) {
    // body
}

// All parts are optional
for (;;) { }           // infinite loop
for (var i = 0;;) { }  // no condition
for (; i < 10;) { }    // no initializer or increment
```

#### Foreach Loop

```csharp
foreach (var item in collection) {
    // body
}

// With typed variable
foreach (int num in numbers) {
    // body
}
```

#### Do-While Loop

```csharp
do {
    // body executes at least once
} while (condition);
```

#### Break and Continue

```csharp
// Break exits the innermost loop or switch
while (true) {
    if (done) break;
}

// Continue skips to next iteration
for (var i = 0; i < 10; i += 1) {
    if (i % 2 == 0) continue;  // skip even numbers
    process(i);
}
```

#### Switch Statement

```csharp
switch (expression) {
    case value1:
        // statements
        break;
    case value2:
    case value3:
        // fall-through: multiple cases can share code
        break;
    default:
        // executed when no case matches
        break;
}
```

Switch statements support:
- Numeric, string, boolean, and null case values
- Fall-through behavior (execution continues to next case without break)
- `default` case for unmatched values
- `break` to exit the switch
- `return` to exit the containing block
- Expressions in switch value and case patterns

```csharp
// Example with return
{
    switch (status) {
        case "active":
            return "User is active";
        case "pending":
            return "User is pending";
        default:
            return "Unknown status";
    }
}

// Example with fall-through
{
    var result = "";
    switch (grade) {
        case 10:
        case 9:
            result = "A";
            break;
        case 8:
            result = "B";
            break;
        default:
            result = "Other";
            break;
    }
    return result;
}
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
- `undefined` (JavaScript-friendly, maps to null)
- `new`
- `var`
- `let` (JavaScript-friendly, treated as var)
- `const` (reserved keyword)
- `return`
- `if`
- `else`
- `while`
- `for`
- `foreach`
- `do`
- `in`
- `break`
- `continue`
- `switch`
- `case`
- `default`

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
assignment     = null_coalesce ( ( "??=" | "=" | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | ">>=" ) assignment )? ;
null_coalesce  = conditional ( "??" conditional )* ;
conditional    = or ( "?" expression ":" expression )? ;
or             = and ( "||" and )* ;
and            = bitwise_or ( "&&" bitwise_or )* ;
bitwise_or     = bitwise_xor ( "|" bitwise_xor )* ;
bitwise_xor    = bitwise_and ( "^" bitwise_and )* ;
bitwise_and    = equality ( "&" equality )* ;
equality       = comparison ( ( "==" | "!=" | "===" | "!==" ) comparison )* ;
comparison     = shift ( ( "<" | "<=" | ">" | ">=" | "in" ) shift )* ;
shift          = term ( ( "<<" | ">>" ) term )* ;
term           = factor ( ( "+" | "-" ) factor )* ;
factor         = unary ( ( "*" | "/" | "%" ) unary )* ;
unary          = ( "!" | "-" | "~" ) unary | ( "++" | "--" ) IDENTIFIER | postfix ;
postfix        = primary ( "." IDENTIFIER | "?." IDENTIFIER | "[" expression "]" | "(" arguments? ")" | "++" | "--" )* ;
primary        = NUMBER | STRING | "true" | "false" | "null" | "undefined"
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
               | "break" ";"
               | "continue" ";"
               | "if" "(" expression ")" ( "{" statement* "}" | statement ) ( "else" ( "{" statement* "}" | statement ) )?
               | "while" "(" expression ")" ( "{" statement* "}" | statement )
               | "for" "(" for_init? ";" expression? ";" expression? ")" ( "{" statement* "}" | statement )
               | "foreach" "(" ( "var" | TYPE_KEYWORD ) IDENTIFIER "in" expression ")" ( "{" statement* "}" | statement )
               | "do" ( "{" statement* "}" | statement ) "while" "(" expression ")" ";"?
               | "switch" "(" expression ")" "{" switch_case* "}"
               | ( "var" | "let" ) IDENTIFIER "=" expression ";"
               | TYPE_KEYWORD IDENTIFIER "=" expression ";"
               | expression ";" ;

switch_case    = "case" expression ":" statement*
               | "default" ":" statement* ;

for_init       = ( "var" | "let" ) IDENTIFIER "=" expression
               | TYPE_KEYWORD IDENTIFIER "=" expression
               | expression ;

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

### Control Flow

```csharp
throw new ...    // Throw statements - NOT supported
try { } catch    // Try-catch - NOT supported
```

Note: All loops (`while`, `for`, `foreach`, `do-while`), `switch` statements, and loop control (`break`, `continue`) ARE supported.

### Constructors

```csharp
// Supported
new { Name = "John", Age = 30 }    // Anonymous objects - SUPPORTED
new { ...person, Extra = "value" } // Spread operator - SUPPORTED

// Not yet supported (planned)
new DateTime(2024, 1, 1)           // Typed constructors - NOT YET (planned)
new Point { X = 10, Y = 20 }       // Object initializers - NOT YET (planned)
new List<int> { 1, 2, 3 }          // Collection initializers - NOT YET (low priority)
```

See [ROADMAP.md](../ROADMAP.md) for planned constructor features.

### Other

```csharp
nameof(x)        // Name of expression - NOT supported
default(T)       // Default value - NOT supported
x..y             // Range operator - NOT supported
new int[5]       // Array creation with size - NOT supported (use [1,2,3] literal)
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
