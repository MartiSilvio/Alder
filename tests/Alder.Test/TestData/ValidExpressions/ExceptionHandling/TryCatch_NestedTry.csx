string trace = "";
try
{
    try
    {
        throw new Exception("inner");
    }
    catch (Exception)
    {
        trace += "i";
        throw new Exception("outer");
    }
}
catch (Exception ex)
{
    trace += "o:" + ex.Message;
}
return trace;
