try
{
    throw new ArgumentNullException("myParam");
}
catch (ArgumentNullException ex)
{
    return ex.ParamName;
}
