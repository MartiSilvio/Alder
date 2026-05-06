// §12.20: nested query in select projection
var outer = new[] { 1, 2, 3 };
var inner = new[] { 10, 20, 30 };
var result = from x in outer
             select (from y in inner where y > x * 10 select y).Count();
var total = 0;
foreach (var item in result) total += (int)item;
return total;
