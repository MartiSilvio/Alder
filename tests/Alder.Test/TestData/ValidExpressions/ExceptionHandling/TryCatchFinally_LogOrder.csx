var log = "";
try
{
    log += "try:";
    throw new InvalidOperationException();
}
catch (InvalidOperationException)
{
    log += "catch:";
}
finally
{
    log += "finally";
}
return log;
