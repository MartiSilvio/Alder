var a = new[] { 1 };
var b = new[] { 2 };
var c = new[] { 3 };
var q = (from x in a
         from y in b
         from z in c
         let sum = x + y + z
         select $"{x},{y},{z}={sum}").First();
return q;
