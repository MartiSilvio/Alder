try
{
    var inner = new InvalidOperationException("inner-msg");
    throw new Exception("outer-msg", inner);
}
catch (Exception ex)
{
    return ex.InnerException.Message;
}
