// §13.11, §15.15: throw after await, catch extracts substring
try
{
    var x = await Task.FromResult(1);
    throw new InvalidOperationException("error-" + x);
}
catch (InvalidOperationException ex)
{
    return ex.Message.Substring(0, 5);
}
