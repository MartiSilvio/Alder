// §13.11, §15.15: await completed task then throw, catch reads Message
try
{
    var x = await Task.FromResult(3);
    if (x == 3)
        throw new InvalidOperationException("boom");
    return "unreached";
}
catch (InvalidOperationException ex)
{
    return ex.Message;
}
