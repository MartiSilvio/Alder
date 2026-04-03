var log = "";
try
{
    log += await Task.FromResult("try ");
    throw new Exception();
}
catch
{
    log += await Task.FromResult("catch ");
}
finally
{
    log += "finally";
}
return log;
