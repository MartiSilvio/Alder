// §21.3: Exception.InnerException chains the causing exception
try
{
    var inner = new ArgumentException("inner");
    throw new InvalidOperationException("outer", inner);
}
catch (InvalidOperationException e)
{
    return e.InnerException.Message;
}
