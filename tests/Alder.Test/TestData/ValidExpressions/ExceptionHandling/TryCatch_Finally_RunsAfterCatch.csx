string trace = "";
try
{
    throw new Exception("boom");
}
catch (Exception)
{
    trace += "C";
}
finally
{
    trace += "F";
}
return trace;
