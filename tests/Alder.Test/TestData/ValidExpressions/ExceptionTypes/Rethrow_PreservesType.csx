// §13.10.6: 'throw;' rethrows current exception
int result = 0;
try
{
    try
    {
        throw new InvalidOperationException("inner");
    }
    catch (InvalidOperationException)
    {
        throw;
    }
}
catch (InvalidOperationException e)
{
    result = e.Message == "inner" ? 1 : 0;
}
return result;
