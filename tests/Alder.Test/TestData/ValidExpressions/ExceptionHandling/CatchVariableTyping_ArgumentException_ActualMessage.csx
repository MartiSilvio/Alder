try
{
    throw new ArgumentException("bad value", "param1");
}
catch (ArgumentException ex)
{
    return ex.ParamName;
}
