try
{
    try { throw new InvalidOperationException(); }
    catch { return await Task.FromResult("inner catch"); }
}
catch { return "outer catch"; }
