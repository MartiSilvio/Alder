var results = new List<string>();
for (var i = 0; i < 3; i++)
{
    try
    {
        var val = await Task.FromResult(i * 10);
        if (i == 1) throw new InvalidOperationException("boom");
        results.Add($"ok:{val}");
    }
    catch (InvalidOperationException ex)
    {
        results.Add($"err:{await Task.FromResult(ex.Message)}");
    }
}
return string.Join(",", results);
