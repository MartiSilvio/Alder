var items = new[] { 1, 2, 0, 4 };
var sum = 0;
foreach (var item in items)
{
    try
    {
        sum += 10 / item;
    }
    catch (System.DivideByZeroException)
    {
        sum += -1;
    }
}
return sum;
