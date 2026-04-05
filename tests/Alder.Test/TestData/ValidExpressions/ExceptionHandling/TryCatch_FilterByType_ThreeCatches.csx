try
{
    throw new System.NotImplementedException();
}
catch (System.ArgumentException)
{
    return "arg";
}
catch (System.NotImplementedException)
{
    return "notimpl";
}
catch (System.Exception)
{
    return "generic";
}
