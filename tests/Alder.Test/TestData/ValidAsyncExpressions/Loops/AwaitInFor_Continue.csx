var sum = 0;
for (var i = 0; i < 6; i++)
{
    if (i % 2 != 0) continue;
    sum += await Task.FromResult(i);
}
return sum;
