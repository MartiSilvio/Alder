var sum = 0;
var i = 0;
while (i < 20)
{
    i++;
    if (i % 3 == 0)
    {
        sum += await Task.FromResult(i);
        continue;
    }
    if (i > 15) break;
    sum += await Task.FromResult(0);
}
return sum;
