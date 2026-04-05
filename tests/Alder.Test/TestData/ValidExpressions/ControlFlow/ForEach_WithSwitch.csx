var items = new[] { 1, 2, 3, 4, 5 };
var sum = 0;
foreach (var item in items)
{
    switch (item % 3)
    {
        case 0: sum += 100; break;
        case 1: sum += 10; break;
        default: sum += 1; break;
    }
}
return sum;
