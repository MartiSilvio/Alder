var outer = new[] { 1, 2, 3 };
var inner = new[] { 2, 3, 4 };
var result = from x in outer
             join y in inner on x equals y
             select x * 10 + y;
var sum = 0;
foreach (var item in result) sum += (int)item;
return sum;
