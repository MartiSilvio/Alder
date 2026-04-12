try
{
    throw new ArgumentNullException("param");
}
catch (ArgumentNullException)
{
    return "argnull";
}
catch (Exception)
{
    return "general";
}
