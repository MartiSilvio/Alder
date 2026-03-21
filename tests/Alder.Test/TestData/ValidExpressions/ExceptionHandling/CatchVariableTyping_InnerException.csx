try
{
    try { throw new System.InvalidOperationException("inner"); }
    catch (System.Exception inner)
    {
        throw new System.ArgumentException("outer", inner);
    }
}
catch (System.ArgumentException ex)
{
    return ex.InnerException.Message;
}
