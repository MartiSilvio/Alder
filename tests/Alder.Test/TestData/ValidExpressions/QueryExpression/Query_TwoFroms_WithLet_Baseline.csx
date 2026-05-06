var nums = new[] { 1, 2, 3 };
var q = (from x in nums
         from y in nums
         let product = x * y
         where product > 4
         select product).ToList();
return q.Count;
