var list = new[] { 1, 2, 3, 4, 5, 6 };
var result = from x in list
             group x by x % 2 into g
             select g.Key;
var sum = 0;
foreach (var item in result) sum += (int)item;
return sum;
