try
{
    throw new ArgumentException("bad");
}
catch (Exception ex)
{
    return ex.GetType().Name;
}
