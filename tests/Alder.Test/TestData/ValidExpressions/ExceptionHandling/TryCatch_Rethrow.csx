string result = "";
try
{
    try
    {
        throw new InvalidOperationException("inner");
    }
    catch (InvalidOperationException)
    {
        result = "caught-inner-";
        throw;
    }
}
catch (InvalidOperationException ex)
{
    result += ex.Message;
}
return result;
