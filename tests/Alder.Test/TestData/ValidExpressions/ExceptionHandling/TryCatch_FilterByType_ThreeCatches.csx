try
{
    throw new NotImplementedException();
}
catch (ArgumentException)
{
    return "arg";
}
catch (NotImplementedException)
{
    return "notimpl";
}
catch (Exception)
{
    return "generic";
}
