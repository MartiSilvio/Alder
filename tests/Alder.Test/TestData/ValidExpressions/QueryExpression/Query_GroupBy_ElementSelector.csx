var list = new[] { 1, 2, 3, 4, 5, 6 };
var result = from x in list
             group x * 10 by x % 2 == 0;
var sum = 0;
foreach (var g in result)
{
    foreach (var item in g) sum += (int)item;
}
return sum;
