var list = new[] { 1, 2, 3, 4, 5 };
var result = from x in list let y = x * 2 select y;
var sum = 0;
foreach (var item in result) sum += (int)item;
return sum;
