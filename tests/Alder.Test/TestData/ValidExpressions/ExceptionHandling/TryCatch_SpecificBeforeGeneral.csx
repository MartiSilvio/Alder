try
{
    throw new ArgumentException("test");
}
catch (ArgumentException)
{
    return "specific";
}
catch (Exception)
{
    return "general";
}
