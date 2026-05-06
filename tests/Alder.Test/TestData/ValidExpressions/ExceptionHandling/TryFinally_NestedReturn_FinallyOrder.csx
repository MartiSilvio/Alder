var log = "";
try
{
    try
    {
        log += "inner-try:";
        return log + "return";
    }
    finally
    {
        log += "inner-finally:";
    }
}
finally
{
    log += "outer-finally";
}
