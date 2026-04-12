try
{
    try { throw new InvalidOperationException("inner"); }
    catch (Exception inner)
    {
        throw new ArgumentException("outer", inner);
    }
}
catch (ArgumentException ex)
{
    return ex.InnerException.Message;
}
