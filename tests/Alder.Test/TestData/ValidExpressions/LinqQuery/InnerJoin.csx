// §12.20: inner join on equals
var a = new[] { 1, 2, 3 };
var b = new[] { 2, 3, 4 };
var result = from x in a join y in b on x equals y select x + y;
var total = 0;
foreach (var item in result) total += (int)item;
return total;
