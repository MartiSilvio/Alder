var a = new[] { 2 };
var b = new[] { 3 };
var c = new[] { 5 };
var q = (from x in a
         from y in b
         let xy = x * y
         from z in c
         let xyz = xy * z
         select xyz).First();
return q;
