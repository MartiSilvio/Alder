try
{
    throw new Exception("hello world");
}
catch (Exception ex)
{
    return ex.Message;
}
