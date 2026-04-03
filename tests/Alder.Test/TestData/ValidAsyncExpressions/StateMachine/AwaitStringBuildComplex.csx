var parts = new List<string>();
for (var i = 0; i < 5; i++)
{
    var prefix = i % 2 == 0
        ? await Task.FromResult("E")
        : await Task.FromResult("O");
    var val = await Task.FromResult(i * i);
    parts.Add($"{prefix}{val}");
}
return string.Join("-", parts);
