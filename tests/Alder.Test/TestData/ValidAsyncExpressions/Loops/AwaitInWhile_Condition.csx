var i = await Task.FromResult(0);
var sum = 0;
while (i < 4)
{
    sum += i;
    i++;
}
return sum;
