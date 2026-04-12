// §13.11, §15.15: finally runs after successful await path
int flag = 0;
try
{
    flag = await Task.FromResult(1);
}
finally
{
    flag += 10;
}
return flag;
