var log = "";
try
{
    try
    {
        log += "A";
        throw new System.InvalidOperationException();
    }
    finally
    {
        log += "B";
    }
}
catch (System.InvalidOperationException)
{
    log += "C";
}
return log;
