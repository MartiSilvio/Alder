var a = new[] { 1, 2 };
var b = new[] { 10, 20 };
var c = new[] { 100, 200 };
var q = (from x in a
         from y in b
         from z in c
         let sum = x + y + z
         where sum == 111
         select sum).ToList();
return q.Count;
