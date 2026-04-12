// §13.11: try/finally executes finally and returns try value
int x = 0;
try
{
    x = 10;
}
finally
{
    x = x + 5;
}
return x;
