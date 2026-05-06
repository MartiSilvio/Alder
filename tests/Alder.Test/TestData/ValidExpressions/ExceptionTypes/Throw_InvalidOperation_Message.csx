// §21.3: Exception.Message carries the reason text supplied to the constructor
try
{
    throw new InvalidOperationException("x");
}
catch (InvalidOperationException e)
{
    return e.Message;
}
