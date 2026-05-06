var sum = 0;
for (var i = 0; i < 4; i++)
{
    sum += await Task.FromResult(i * 2);
}
return sum;
