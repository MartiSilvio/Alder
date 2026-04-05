var disposed = false;
try
{
    using (var ms = new System.IO.MemoryStream())
    {
        throw new System.InvalidOperationException();
    }
}
catch (System.InvalidOperationException)
{
    disposed = true;
}
return disposed;
