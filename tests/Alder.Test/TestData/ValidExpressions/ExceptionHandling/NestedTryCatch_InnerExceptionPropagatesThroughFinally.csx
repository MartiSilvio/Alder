var log = "";
try
{
    try
    {
        log += "A";
        throw new InvalidOperationException();
    }
    finally
    {
        log += "B";
    }
}
catch (InvalidOperationException)
{
    log += "C";
}
return log;
