// §12.20: let introduces a range variable
var list = new[] { 1, 2, 3, 4 };
var result = from x in list let y = x * 2 where y > 4 select y;
var total = 0;
foreach (var item in result) total += (int)item;
return total;
