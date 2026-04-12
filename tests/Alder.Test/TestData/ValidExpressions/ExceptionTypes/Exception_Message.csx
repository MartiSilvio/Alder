// §21.3: Exception.Message property
try
{
    throw new Exception("hello world");
}
catch (Exception e)
{
    return e.Message;
}
