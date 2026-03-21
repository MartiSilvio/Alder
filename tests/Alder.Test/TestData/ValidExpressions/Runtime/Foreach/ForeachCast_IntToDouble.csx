var sum = 0.0;
var arr = new int[] { 1, 2, 3 };
foreach (var i in arr)
{
    double d = (double)i;
    sum += d;
}
return sum;
