var sum = 0;
var i = 0;
while (i < 5)
{
    sum += await Task.FromResult(i);
    i++;
}
return sum;
