var sum = 0;
for (var i = 0; i < 3; i++)
{
    for (var j = 0; j < 3; j++)
    {
        sum += await Task.FromResult(1);
    }
}
return sum;
