var sum = 0;
var i = 0;
while (true)
{
    if (i >= 3) break;
    sum += await Task.FromResult(10);
    i++;
}
return sum;
