// §21.4: Catch base Exception
try
{
    throw new ArgumentException("arg");
}
catch (Exception e)
{
    return e.GetType().Name == "ArgumentException";
}
