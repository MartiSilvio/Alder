var log = new List<string>();
try
{
    log.Add(await Task.FromResult("try1"));
    try
    {
        log.Add(await Task.FromResult("try2"));
        throw new ArgumentException("inner");
    }
    catch (ArgumentException ex)
    {
        log.Add(await Task.FromResult($"catch2:{ex.Message}"));
        throw new InvalidOperationException("rethrown");
    }
    finally
    {
        log.Add("finally2");
    }
}
catch (InvalidOperationException ex)
{
    log.Add(await Task.FromResult($"catch1:{ex.Message}"));
}
finally
{
    log.Add("finally1");
}
return string.Join(",", log);
