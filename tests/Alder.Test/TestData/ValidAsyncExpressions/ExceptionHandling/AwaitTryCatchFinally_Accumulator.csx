// §13.11, §15.15: try/catch/finally with awaits, returning accumulated result
int total = 0;
try
{
    total += await Task.FromResult(1);
    total += await Task.FromResult(2);
}
catch (Exception)
{
    total = -1;
}
finally
{
    total += 10;
}
return total;
