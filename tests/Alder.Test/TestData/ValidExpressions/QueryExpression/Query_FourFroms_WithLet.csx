var p = new[] { 1, 2 };
var q = (from a in p
         from b in p
         from c in p
         from d in p
         let sum = a + b + c + d
         where sum == 5
         select sum).Count();
return q;
