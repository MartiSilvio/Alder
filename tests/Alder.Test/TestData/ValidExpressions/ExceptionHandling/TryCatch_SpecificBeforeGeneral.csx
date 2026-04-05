try
{
    throw new System.ArgumentException("test");
}
catch (System.ArgumentException)
{
    return "specific";
}
catch (System.Exception)
{
    return "general";
}
