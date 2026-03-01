{
    var a = new[] { 1, 2 };
    var b = new[] { 10, 20 };
    var result = from x in a from y in b where x + y > 11 select x * 100 + y;
    var count = 0;
    var sum = 0;
    foreach (var v in result)
    {
        count++;
        sum += (int)v;
    }
    return count * 10000 + sum;
}