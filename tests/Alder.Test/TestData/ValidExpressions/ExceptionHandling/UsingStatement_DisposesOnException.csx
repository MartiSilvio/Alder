var disposed = false;
try
{
    using (var ms = new System.IO.MemoryStream())
    {
        throw new InvalidOperationException();
    }
}
catch (InvalidOperationException)
{
    disposed = true;
}
return disposed;
