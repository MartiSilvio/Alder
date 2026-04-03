var result = 0;
for (var i = 1; i <= 10; i++)
{
    result += await Task.FromResult(i);
}
return result;
