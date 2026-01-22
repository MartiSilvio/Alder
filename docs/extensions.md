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
