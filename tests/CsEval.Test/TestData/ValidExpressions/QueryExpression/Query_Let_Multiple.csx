var list = new[] { 1, 2, 3 };
var result = from x in list let y = x * 10 let z = y + x select z;
var sum = 0;
foreach (var item in result) sum += (int)item;
return sum;
