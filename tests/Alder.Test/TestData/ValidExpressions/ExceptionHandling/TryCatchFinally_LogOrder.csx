var log = "";
try
{
    log += "try:";
    throw new System.InvalidOperationException();
}
catch (System.InvalidOperationException)
{
    log += "catch:";
}
finally
{
    log += "finally";
}
return log;
