try
{
    throw new System.ArgumentNullException("myParam");
}
catch (System.ArgumentNullException ex)
{
    return ex.ParamName;
}
