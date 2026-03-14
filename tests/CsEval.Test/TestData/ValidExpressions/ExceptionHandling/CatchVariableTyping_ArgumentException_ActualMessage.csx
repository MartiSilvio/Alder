try
{
    throw new System.ArgumentException("bad value", "param1");
}
catch (System.ArgumentException ex)
{
    return ex.ParamName;
}
