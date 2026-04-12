// §15.15, §13.11: try/catch around await, successful await path
int result = 0;
try
{
    result = (await Task.FromResult(5)) + 1;
}
catch (Exception)
{
    result = -1;
}
return result;
