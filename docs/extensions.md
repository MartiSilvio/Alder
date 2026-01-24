# CsEval Extensions

Features that extend beyond standard C# syntax.

---

## Spread Operator (`...`)

Expands arrays and objects inline, borrowed from JavaScript/TypeScript.

### Array Spread

```
var arr1 = [1, 2, 3];
var arr2 = [4, 5, 6];
[...arr1, ...arr2]              // [1, 2, 3, 4, 5, 6]
[0, ...arr1, 4]                 // [0, 1, 2, 3, 4]
```

### Object Spread

```
var person = new { Name = "John", Age = 30 };
new { ...person, City = "NYC" }  // { Name = "John", Age = 30, City = "NYC" }
```

Later properties override earlier ones:

```
var defaults = new { Theme = "light", Size = 10 };
new { ...defaults, Theme = "dark" }  // { Theme = "dark", Size = 10 }
```

---

## Object Merging (`+`)

The `+` operator merges objects when operands are not both numeric.

```
// Object + Anonymous object
person + new { Extra = "data" }

// Dictionary + Dictionary
dict1 + dict2

// Typed object + Anonymous object
entity + new { Computed = entity.A + entity.B }
```

Properties from the right operand override those from the left. Result is always `Dictionary<string, object?>`.

---

## Containment Operator (`in`)

Python-style membership testing. Checks if a value exists in a collection or substring exists in a string.

### Collection Containment

```csharp
2 in [1, 2, 3]              // true
5 in [1, 2, 3]              // false
"b" in ["a", "b", "c"]      // true
null in [1, null, 3]        // true
```

### String Containment

```csharp
"bc" in "abcd"              // true (substring check)
"xy" in "abcd"              // false
```

### With Variables

```csharp
var x = 3;
x in [1, 2, 3, 4, 5]        // true

var arr = [1, 2, 3];
2 in arr                    // true
```

### Combined with Logical Operators

```csharp
(2 in [1, 2, 3]) && (5 in [4, 5, 6])  // true
!(5 in [1, 2, 3])                      // true
x in [1, 2, 3] ? "found" : "missing"  // ternary works
```

---

## Logical Operator Keywords (`and`, `or`, `not`)

Python/SQL-style logical operators as alternatives to C# symbols.

### Equivalents

| Keyword | C# Symbol | Description |
|---------|-----------|-------------|
| `and` | `&&` | Logical AND |
| `or` | `\|\|` | Logical OR |
| `not` | `!` | Logical NOT |

### Examples

```csharp
true and false              // false (same as true && false)
true or false               // true (same as true || false)
not true                    // false (same as !true)
```

### With Expressions

```csharp
(x > 0) and (x < 100)       // Range check
(name == null) or (name == "")  // Empty check
not (x in [1, 2, 3])        // Not in list
```

### Mixed with Symbols

Can be freely mixed with C# symbols:

```csharp
true && true and true       // All true
false || true or false      // true
!false and not false        // true
```

---
