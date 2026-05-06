var arr = new int[] { 1, 2, 3, 4 };
var sum = 0.0;
foreach (var x in arr)
{
    sum += (double)x / 10;
}
return sum;
